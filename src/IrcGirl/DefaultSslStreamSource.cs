using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl
{
    /// <summary>
    /// Represents the default SSL stream source to use.
    /// 
    /// Probably implements the .NET Standard 2.0 SslStream which supports up
    /// to TLS 1.2.
    /// </summary>
    internal class DefaultSslStreamSource : ISslStreamSource
    {
        public Task AuthenticateAsClientAsync(SslStream stream, string targetHost)
        {
            return stream.AuthenticateAsClientAsync(targetHost, null, SslProtocols.None, true);
        }

        public SslStream CreateStream(Stream innerStream)
        {
            return new SslStream(innerStream);
        }
    }
}
