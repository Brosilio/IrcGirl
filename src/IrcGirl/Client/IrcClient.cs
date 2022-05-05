using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IrcGirl.Client
{
	public class IrcClient : IDisposable
	{
		#region properties

		public IrcUser User { get; private set; }
		public IrcServerInfo Server { get; private set; }
		public bool IsConnected { get; private set; }
		public bool IsRegistered { get; private set; }
		public ISslStreamSource SslStreamSource { get; set; } = new DefaultSslStreamSource();

		public Action<IrcMessage> IrcMessageReceived { get; set; }
		public Action Disconnected { get; set; }
		public Action<Exception> ExceptionRaised { get; set; }

		#endregion properties

		#region fields

		private TcpClient _tcpClient;
		private Stream _stream;
		private IrcStream _irc;
		private EventAwaiter _awaiter = new EventAwaiter();
		private Dictionary<string, Action<IrcClient, IrcMessage>> _customHandlers = new Dictionary<string, Action<IrcClient, IrcMessage>>();
		private Dictionary<string, MessageSinkDelegate> _sinkHandlers = new Dictionary<string, MessageSinkDelegate>();

		private delegate void MessageSinkDelegate(IrcMessage message);

		#endregion fields

		#region ctors

		public IrcClient()
		{
			HookInternalSinks();
		}

		private void HookInternalSinks()
		{
			var methods = GetType().GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

			foreach (var method in methods)
			{
				var customAttribs = method.GetCustomAttributes(typeof(IrcMessageSinkAttribute), false);

				if (customAttribs == null || customAttribs.Length == 0)
					continue;

				foreach (var attrib in customAttribs)
				{
					if (attrib is IrcMessageSinkAttribute sinkAttrib)
					{
						foreach (string cmd in sinkAttrib.Commands)
						{
							Console.WriteLine($"Sink({cmd}) -> {method.Name}");

							_sinkHandlers.Add(cmd.ToUpper(), (MessageSinkDelegate)method.CreateDelegate(typeof(MessageSinkDelegate), this));
						}
					}
				}
			}
		}

		#endregion ctors

		#region methods.connect

		public async Task ConnectAsync(string host, int port, ESslMode sslMode)
		{
			CheckNotConnected();

			if (_tcpClient != null)
				_tcpClient.Dispose();

			_tcpClient = new TcpClient();
			await _tcpClient.ConnectAsync(host, port);

			if (sslMode == ESslMode.None)
			{
				_stream = _tcpClient.GetStream();
			}
			else if (sslMode == ESslMode.UseSsl)
			{
				SslStream ssl = SslStreamSource.CreateStream(_tcpClient.GetStream());

				await SslStreamSource.AuthenticateAsClientAsync(ssl, host);

				_stream = ssl;
			}
			else
			{
				throw new Exception("Internal exception: unsupported SSL mode");
			}

			OnConnectInternal();
		}

		public async Task ConnectAsync(string host, int port, ESslMode sslMode, string password)
		{
			await ConnectAsync(host, port, sslMode);
			await SendRaw($"PASS {password}");
		}

		#endregion methods.connect

		#region methods.disconnect

		/// <summary>
		/// Disconnect from the server.
		/// </summary>
		/// 
		/// <param name="reason">
		/// A string for the QUIT command.
		/// If null, the connection will be forcefully closed.
		/// </param>
		public async Task DisconnectAsync(string reason)
		{
			if (_tcpClient == null)
				return;

			if (reason == null)
			{
				_tcpClient.Dispose();
				_tcpClient = null;

				IsConnected = false;
				IsRegistered = false;
				return;
			}

			await SendRaw($"QUIT :{reason}");
		}

		#endregion methods.disconnect

		#region methods.messageloop

		/// <summary>
		/// The internal receive and event dispatch loop.
		/// </summary>
		private async Task MessageLoop()
		{
			while (true)
			{
				var msg = await _irc.ReadAsync();

				if (msg == null)
				{
					OnDisconnectInternal();

					break;
				}

				try
				{
					OnIrcMessageInternal(msg);
				}
				catch (Exception ex)
				{
					OnDispatchExceptionInternal(ex, msg);
				}
			}
		}

		/// <summary>
		/// Add a custom handler for an IRC command (eg. PRIVMSG).
		/// Your handler will be run after all other internal handlers.
		/// </summary>
		/// 
		/// <param name="command">The IRC command to watch for.</param>
		/// <param name="handler">An action to execute when the target command is received.</param>
		public void On(string command, Action<IrcClient, IrcMessage> handler)
		{
			_customHandlers.Add(command.ToLower(), handler);
		}

		/// <summary>
		/// Add a custom handler for an IRC reply code (eg. 001).
		/// Your handler will be run after all other internal handlers.
		/// </summary>
		/// 
		/// <param name="replyCode">The reply code to watch for.</param>
		/// <param name="handler">An action to execute when the target reply code is received.</param>
		/// 
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public void On(int replyCode, Action<IrcClient, IrcMessage> handler)
		{
			if (replyCode < 0 || replyCode > 999)
				throw new ArgumentOutOfRangeException(nameof(replyCode), "Must be between 0 and 999");

			On(replyCode.ToString("D3"), handler);
		}

		#endregion methods.messageloop

		#region methods.events

		/// <summary>
		/// Called when a connection is made
		/// </summary>
		private void OnConnectInternal()
		{
			_irc = new IrcStream(_stream);
			Server = new IrcServerInfo();
			User = new IrcUser(this);

			IsConnected = true;
			var t = Task.Factory.StartNew(MessageLoop, TaskCreationOptions.LongRunning);
		}

		/// <summary>
		/// Called when a connection is destroyed
		/// </summary>
		private void OnDisconnectInternal()
		{
			IsConnected = false;
			IsRegistered = false;

			Disconnected?.Invoke();

			_awaiter.Finish(nameof(RegisterAsync));
		}

		/// <summary>
		/// Called when a new IRC message is received.
		/// 
		/// This is the method that fires sinks and events.
		/// </summary>
		private void OnIrcMessageInternal(IrcMessage message)
		{
			if (_sinkHandlers.ContainsKey(message.command.ToUpper()))
				_sinkHandlers[message.command.ToUpper()](message);

			if (IrcMessageReceived != null)
				IrcMessageReceived(message);

			if (_customHandlers.ContainsKey(message.command.ToLower()))
				_customHandlers[message.command.ToLower()]?.Invoke(this, message);
		}

		/// <summary>
		/// Called when <see cref="OnIrcMessageInternal(IrcMessage)"/> throws an exception.
		/// </summary>
		/// 
		/// <param name="ex">The exception.</param>
		/// <param name="msg">The message that caused the exception.</param>
		private void OnDispatchExceptionInternal(Exception ex, IrcMessage msg)
		{
			var cmd = msg.command.ToUpper();
			if (_sinkHandlers.ContainsKey(cmd))
				_awaiter.Error(_sinkHandlers[cmd].Method.Name, ex);

			ExceptionRaised?.Invoke(ex);
		}

		#endregion methods.events

		#region methods.sinks

		/// <summary>
		/// Sink for RPL_WELCOME.
		/// </summary>
		[IrcMessageSink(001)]
		private void SinkWelcomeInternal(IrcMessage message)
		{
			IsRegistered = true;
			Server.Welcome = message.GetTrailer();

			_awaiter.Finish("NICK");
		}

		/// <summary>
		/// Sink for RPL_YOURHOST.
		/// </summary>
		[IrcMessageSink(002)]
		private void SinkYourHostInternal(IrcMessage message) => Server.YourHost = message.GetTrailer();

		/// <summary>
		/// Sink for RPL_CREATED.
		/// </summary>
		[IrcMessageSink(003)]
		private void SinkCreatedInternal(IrcMessage message) => Server.Created = message.GetTrailer();

		/// <summary>
		/// Sink for RPL_MYINFO.
		/// </summary>
		[IrcMessageSink(004)]
		private void SinkMyInfoInternal(IrcMessage message) => Server.MyInfo = message.GetTrailer();

		[IrcMessageSink(005)]
		private void SinkISupportInternal(IrcMessage message) => throw new NotImplementedException();

		/// <summary>
		/// Called when a NICK message is received or error responses to a NICK
		/// message are received.
		/// </summary>
		[IrcMessageSink("NICK")]
		[IrcMessageSink(431, 432, 433, 436)]
		private void SinkNickInternal(IrcMessage message)
		{
			new IrcException()
			if (message.command == "433") throw new Exception("Nick in use");
		}

		#endregion methods.sinks

		#region methods.sending

		/// <summary>
		/// Send an IRC message.
		/// </summary>
		/// 
		/// <param name="message">The message to send.</param>
		public Task Send(IrcMessage message)
		{
			return _irc.WriteAsync(message);
		}

		/// <summary>
		/// Send an IRC message. The message is parsed and validated first.
		/// </summary>
		/// 
		/// <param name="message">The message to send.</param>
		public Task Send(string message)
		{
			return _irc.WriteAsync(message);
		}

		/// <summary>
		/// Send a raw string. No validation is done.
		/// </summary>
		/// 
		/// <param name="message">The message to send.</param>
		public Task SendRaw(string message)
		{
			return _irc.WriteRawAsync(message);
		}

		#endregion methods.sending

		#region methods.irc

		/// <summary>
		/// Register.
		/// </summary>
		/// 
		/// <param name="userName">Your username.</param>
		/// <param name="nickName">Your nickname.</param>
		/// <param name="realName">Your real name.</param>
		/// 
		/// <returns></returns>
		/// 
		/// <exception cref="Exception"></exception>
		public async Task RegisterAsync(string userName, string nickName, string realName)
		{
			if (IsRegistered)
				throw new Exception("Already registered");

			if (_awaiter.IsInProgress(nameof(SinkNickInternal)))
				throw new Exception("A registration is already in progress");

			await SendRaw($"NICK {nickName}");
			await _awaiter.Wait(nameof(SinkNickInternal));

			await SendRaw($"USER {userName} 0 *: {realName}");
			await _awaiter.Wait("USER");

			User = new IrcUser(this)
			{
				UserName = userName,
				NickName = nickName,
				RealName = realName
			};
		}

		/// <summary>
		/// Change your nickname on the server. Must be registered first.
		/// </summary>
		/// 
		/// <param name="nickName">Your new nickname.</param>
		public async Task ChangeNickAsync(string nickName)
		{
			CheckRegistered();

			if (_awaiter.IsInProgress(nameof(ChangeNickAsync)))
				throw new Exception("Nick change already in progress!");
		}

		#endregion methods.irc

		#region methods.checks

		/// <summary>
		/// Throw if the client is not registered.
		/// </summary>
		/// 
		/// <exception cref="Exception"></exception>
		private void CheckRegistered()
		{
			if (!IsRegistered)
				throw new Exception("Call RegisterAsync() first");
		}

		/// <summary>
		/// Throw if the client is registered.
		/// </summary>
		/// 
		/// <exception cref="Exception"></exception>
		private void CheckNotRegistered()
		{
			if (IsRegistered)
				throw new Exception("Operation not valid after registration");
		}

		/// <summary>
		/// Throw if the client is not connected.
		/// </summary>
		/// 
		/// <exception cref="Exception"></exception>
		private void CheckConnected()
		{
			if (!IsConnected)
				throw new Exception("Call ConnectAsync() first");
		}

		/// <summary>
		/// Throw if the client is connected.
		/// </summary>
		/// 
		/// <exception cref="Exception"></exception>
		private void CheckNotConnected()
		{
			if (IsConnected)
				throw new Exception("Operation not valid while connected");
		}

		#endregion methods.checks

		#region IDisposable

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~IrcClientBase()
		// {
		//     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
		//     Dispose(disposing: false);
		// }

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}

		private void CheckDisposed()
		{
			if (disposedValue)
				throw new ObjectDisposedException(GetType().FullName);
		}

		#endregion IDisposable
	}
}
