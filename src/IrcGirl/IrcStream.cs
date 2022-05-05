﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl
{
	public class IrcStream : IDisposable
	{
		private Stream _stream;
		private StreamReader _reader;
		private StreamWriter _writer;
		private IrcMessageLexer _lexer;

		public IrcStream(Stream innerStream)
		{
			_stream = innerStream;
			_reader = new StreamReader(innerStream);
			_writer = new StreamWriter(innerStream);
			_writer.NewLine = "\r\n";
			_lexer = new IrcMessageLexer();
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
			string line;

			do
			{
				line = await _reader.ReadLineAsync();

				if (line == null)
					return null;
			}
			while (line.Length == 0);

			return _lexer.Lex(line);
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
			IrcMessageLexer lex = new IrcMessageLexer();
			IrcMessage ircMessage = lex.Lex(message);

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
