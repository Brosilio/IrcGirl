using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using IrcGirl.Protocol.IrcV3;
using IrcGirl.Protocol.IrcV3.IrcMessages;

namespace IrcGirl.Net
{
	public class IrcMessageStream : IDisposable
	{
		private Stream _stream;
		private StreamReader _reader;
		private StreamWriter _writer;
		private IIrcMessageParser _parser;
		private ConcurrentQueue<string> _sendq;
		private SemaphoreSlim _sendqSem;
		private bool _probablyDisconnected;

		public IrcMessageStream(Stream innerStream)
		{
			_parser = new IrcMessageParser();
			_sendq = new ConcurrentQueue<string>();
			_sendqSem = new SemaphoreSlim(0);

			_stream = innerStream;
			_reader = new StreamReader(innerStream);
			_writer = new StreamWriter(innerStream);
			_writer.NewLine = "\r\n";
			//_parser = new IrcMessageParser();

			// start send loop
			_ = Task.Factory.StartNew(
				SendLoop,
				TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach
			).ConfigureAwait(false);
		}

		/// <summary>
		/// The internal send loop.
		/// </summary>
		private async Task SendLoop()
		{
			//if (_sendqSem.CurrentCount == 0)
			//	return;

			while (!_probablyDisconnected)
			{
				await _sendqSem.WaitAsync();

				while (_sendq.TryDequeue(out string message))
				{
					await _writer.WriteLineAsync(message);
				}

				// flush if we haven't already
				await _writer.FlushAsync();
			}
		}

		/// <summary>
		/// Read the oldest (as in, earliest received) IrcMessage from the network.
		/// </summary>
		/// 
		/// <returns>
		/// An IrcMessage if available or null if the input stream is dead.
		/// </returns>
		public async Task<RawIrcMessage> ReadAsync()
		{
			if (_probablyDisconnected)
				return null;

			string line;

			do
			{
				line = await _reader.ReadLineAsync();

				if (line == null)
				{
					_probablyDisconnected = true;
					return null;
				}
			}
			while (line.Length == 0);

			return _parser.Parse(line);
		}

		/// <summary>
		/// Queue a <see cref="RawIrcMessage"/> to be sent to the server.
		/// 
		/// The message is validated immediately, sent eventually. Thread-safe.
		/// </summary>
		/// 
		/// <param name="message">The message to send.</param>
		/// 
		/// <exception cref="InvalidIrcMessageException"/>
		public void QueueForSend(RawIrcMessage message)
		{
			// if the message is of a known type, ensure it is valid
			_ = IrcMessage.CreateInstance(message);

			// serialize the message and queue it
			_sendq.Enqueue(message.Serialize());
			_sendqSem.Release();
		}

		/// <summary>
		/// Immediately an IRC message to the network.
		/// Returns when the message sent. Not thread-safe, may conflict with the
		/// internal send queue (see <see cref="QueueForSend(RawIrcMessage)"/>).
		/// </summary>
		/// 
		/// <param name="message">The message to send.</param>
		///
		/// <exception cref="ArgumentNullException"></exception>
		public async Task WriteAsync(RawIrcMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			await _writer.WriteAsync(message.Serialize());
			await _writer.WriteLineAsync();
			await _writer.FlushAsync();
		}

		/// <summary>
		/// Immediately write an IRC message to the network.
		/// The message is parsed and validated before sending. Not thread-safe,
		/// may conflict with the internal send queue. Use <see cref="QueueForSend(RawIrcMessage)"/>
		/// instead.
		/// </summary>
		/// 
		/// <param name="message">The message to send.</param>
		/// 
		/// <exception cref="Exception"></exception>
		public Task WriteAsync(string message)
		{
			RawIrcMessage ircMessage = _parser.Parse(message);

			if (ircMessage == null)
				throw new Exception("Invalid IRC message");

			return WriteAsync(ircMessage);
		}

		/// <summary>
		/// Write a raw string to the network. The string is not parsed or validated. Use with caution.
		/// </summary>
		/// 
		/// <param name="message">The message to send.</param>
		public async Task WriteRawAsync(string message)
		{
			await _writer.WriteLineAsync(message);
			await _writer.FlushAsync();
		}

		#region IDisposable

		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					_stream?.Dispose();
					_reader?.Dispose();
					_writer?.Dispose();

					_stream = null;
					_reader = null;
					_writer = null;

					// TODO: dispose managed state (managed objects)
				}

				// TODO: free unmanaged resources (unmanaged objects) and override finalizer
				// TODO: set large fields to null
				disposedValue = true;
			}
		}

		// // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
		// ~IrcStream()
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

		#endregion IDisposable
	}
}
