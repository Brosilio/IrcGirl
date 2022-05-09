using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using IrcGirl.Protocol.IrcV3;
using IrcGirl.Net;

using SinkDictionary = System.Collections.Generic.Dictionary<string, IrcGirl.Client.IrcMessageSinkContainer>;

namespace IrcGirl.Client
{
	/// <summary>
	/// Represents the base class for all IrcClients.
	/// 
	/// Either inherit this class and override the event methods or use <see cref="EventedIrcClient"/>.
	/// </summary>
	public abstract class IrcClient : IDisposable
	{
		public IrcUser User { get; private set; }
		public IrcServerInfo Server { get; private set; }
		public bool IsConnected { get; private set; }
		public bool IsRegistered { get; private set; }
		public ISslStreamSource SslStreamSource { get; set; }

		private TcpClient _tcpClient;
		private IrcMessageStream _ircStream;
		private SinkDictionary _sinks;

		internal delegate void MessageSinkDelegate(IrcMessage message);

		public IrcClient()
		{
			_sinks = new SinkDictionary();

			SslStreamSource = new DefaultSslStreamSource();

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
						IrcMessageSinkContainer sink = new IrcMessageSinkContainer()
						{
							Command = sinkAttrib.Command,
							ReplyCode = sinkAttrib.IrcReplyCode,
							Delegate = (MessageSinkDelegate)method.CreateDelegate(typeof(MessageSinkDelegate), this),
							Error = sinkAttrib.Throw,
						};

						_sinks.Add(sink.GetName(), sink);
					}
				}
			}
		}

		/// <summary>
		/// The internal receive and event dispatch loop.
		/// </summary>
		private async Task MessageLoop()
		{
			while (true)
			{
				// wait for and read the next IrcMessage on the network.
				IrcMessage message = await _ircStream.ReadAsync();

				if (message == null)
				{
					OnDisconnected(new Events.IrcDisconnectedEventArgs());
					break;
				}

				try
				{
					// raise the IrcMessageReceived event first
					OnIrcMessageReceived(new Events.IrcMessageEventArgs(message));

					// if we have an internal sink for the message, use it
					if (_sinks.TryGetValue(message.Command, out var sink))
					{
						sink.Delegate(message);
					}
				}
				catch (Exception ex)
				{
					OnMessageLoopExceptionRaised(new Events.MessageLoopExceptionEventArgs(ex, message));
				}
			}
		}

		#region events

		/// <summary>
		/// Called when this client connects to a server.
		/// </summary>
		protected virtual void OnConnected(Events.IrcConnectedEventArgs e) { }

		/// <summary>
		/// Called when this client disconnects (network connection lost).
		/// </summary>
		protected virtual void OnDisconnected(Events.IrcDisconnectedEventArgs e) { }

		/// <summary>
		/// Called when any <see cref="IrcMessage"/> is received.
		/// </summary>
		/// 
		/// <param name="message">The message that was received.</param>
		protected virtual void OnIrcMessageReceived(Events.IrcMessageEventArgs e) { }

		/// <summary>
		/// Called when an exception occours inside the internal message loop.
		/// </summary>
		protected virtual void OnMessageLoopExceptionRaised(Events.MessageLoopExceptionEventArgs e) { }

		/// <summary>
		/// Called when an outbound <see cref="IrcMessage"/> is malformed.
		/// </summary>
		protected virtual void OnOutboundIrcProtocolViolation(Events.IrcProtocolViolationEventArgs e) { }

		/// <summary>
		/// Called when an inbound <see cref="IrcMessage"/> is malformed.
		/// </summary>
		/// <param name="e"></param>
		protected virtual void OnInboundIrcProtocolViolation(Events.IrcProtocolViolationEventArgs e) { }

		/// <summary>
		/// Called when a received <see cref="IrcMessage"/> indicates an error.
		/// </summary>
		/// <param name="replyCode">The <see cref="IrcReplyCode"/> that was received.</param>
		/// <param name="message">The message.</param>
		protected virtual void OnIrcErrorReplyReceived(Events.IrcErrorReplyEventArgs e) { }

		#endregion events

		//internal async Task SendIrcCommand(string command)
		//{
		//	await _ircStream.WriteAsync(command);
		//}

		/// <summary>
		/// Queues a message to be sent by the underlying stream.
		/// </summary>
		/// <param name="message">The message to send.</param>
		public void Send(IrcMessage message)
		{
			_ircStream.QueueForSend(message);
		}

		/// <summary>
		/// Connect to an IRC server.
		/// </summary>
		/// <param name="host"></param>
		/// <param name="port"></param>
		/// <param name="sslMode"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public async Task ConnectAsync(string host, int port, SslMode sslMode)
		{
			// dispose old stuff if any
			try
			{
				_tcpClient?.Dispose();
				_ircStream?.Dispose();
			}
			catch { }

			// new client and connect
			_tcpClient = new TcpClient();
			await _tcpClient.ConnectAsync(host, port).ConfigureAwait(false);

			// get new stream and auth as client if necessary
			Stream stream;
			switch (sslMode)
			{
				case SslMode.None:
					stream = _tcpClient.GetStream();
					break;

				case SslMode.UseSsl:
					SslStream ssl = SslStreamSource.CreateStream(_tcpClient.GetStream());
					await SslStreamSource.AuthenticateAsClientAsync(ssl, host).ConfigureAwait(false);
					stream = ssl;
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(sslMode), "Unsupported SslMode provided");
			}

			// start new irc stream and set flags
			_ircStream = new IrcMessageStream(stream);
			IsConnected = true;

			// raise OnConnected
			OnConnected(new Events.IrcConnectedEventArgs());

			// start message loop
			_ = Task.Factory.StartNew(
				MessageLoop,
				TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach
			).ConfigureAwait(false);
		}

		/// <summary>
		/// Close the connection to the server.
		/// 
		/// If you want to gracefully disconnect, use <see cref=""/>
		/// </summary>
		/// <returns></returns>
		public async Task DisconnectAsync()
		{

		}

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
