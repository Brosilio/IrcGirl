using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl.Client
{
    public class IrcClient : IDisposable
    {
        #region properties

        public IrcUser User { get; private set; }

        public bool IsConnected { get; private set; }

        public bool IsRegistered { get; private set; }

        public ISslStreamSource SslStreamSource { get; set; } = new DefaultSslStreamSource();

        #endregion properties

        #region fields

        private TcpClient _tcpClient;
        private Stream _stream;
        private ESslMode _sslMode;
        private IrcStream _irc;

        #endregion fields

        #region ctors

        public IrcClient()
        {
            _tcpClient = new TcpClient();
        }

        #endregion ctors

        #region methods.connect

        public async Task ConnectAsync(string host, int port, ESslMode sslMode)
        {
            _sslMode = sslMode;

            await _tcpClient.ConnectAsync(host, port);
            
            if (sslMode == ESslMode.None)
            {
                _stream = _tcpClient.GetStream();
            }
            else if (sslMode == ESslMode.UseSsl)
            {
                SslStream ssl = SslStreamSource.CreateStream(_tcpClient.GetStream());
                await SslStreamSource.AuthenticateAsClientAsync(ssl, host);

                Console.WriteLine(ssl.SslProtocol);

                _stream = ssl;
            }
            else
            {
                throw new Exception("Internal exception: unsupported SSL mode");
            }

            _irc = new IrcStream(_stream);
        }

        #endregion methods.connect

        public async Task RegisterAsync()
        {
            byte[] buffer = Encoding.UTF8.GetBytes("NICK basilio");
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            await _stream.FlushAsync();

            buffer = new byte[1024];
            int read = await _stream.ReadAsync(buffer, 0, buffer.Length);
            Console.WriteLine(Encoding.UTF8.GetString(buffer, 0, read));
        }

        public async Task DisconnectAsync()
        {
            IsConnected = false;
            IsRegistered = false;
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
