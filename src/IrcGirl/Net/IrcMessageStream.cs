using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Net
{
	public class IrcMessageStream : IDisposable
	{
		private Stream _stream;
		private StreamReader _reader;
		private StreamWriter _writer;
		private IIrcMessageParser _parser;
		private IIrcMessageValidator _validator;
		private ConcurrentQueue<IrcMessage> _sendq;
		private SemaphoreSlim _sendqSem;
		private bool _probablyDisconnected;

		public IrcMessageStream(Stream innerStream)
		{
			_parser = new IrcMessageParser();
			_validator = new IrcMessageValidator();
			_sendq = new ConcurrentQueue<IrcMessage>();
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

		private async Task SendLoop()
		{
			//if (_sendqSem.CurrentCount == 0)
			//	return;

			while (!_probablyDisconnected)
			{
				await _sendqSem.WaitAsync();

				while (_sendq.TryDequeue(out IrcMessage message))
				{
					await _writer.WriteLineAsync(message.ToString());
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
		public async Task<IrcMessage> ReadAsync()
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
		/// Queue a message to be sent. Returns immediately. Thread-safe.
		/// </summary>
		/// <param name="message"></param>
		public void QueueForSend(IrcMessage message)
		{
			_sendq.Enqueue(message);
			_sendqSem.Release();
		}

		public async Task WriteAsync(IrcMessage message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			await _writer.WriteAsync(message.ToString());
			await _writer.WriteLineAsync();
			await _writer.FlushAsync();
		}

		public Task WriteAsync(string message)
		{
			IrcMessage ircMessage = _parser.Parse(message);

			if (ircMessage == null)
				throw new Exception("Invalid IRC message");

			return WriteAsync(ircMessage);
		}

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
