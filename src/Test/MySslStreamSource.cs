using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

using IrcGirl;
using IrcGirl.Net;

namespace Test
{
    internal class MySslStreamSource : ISslStreamSource
    {
        public Task AuthenticateAsClientAsync(SslStream stream, string targetHost)
        {
            return stream.AuthenticateAsClientAsync(targetHost, null, System.Security.Authentication.SslProtocols.Tls12, false);
        }

        public SslStream CreateStream(Stream innerStream)
        {
            return new SslStream(innerStream, false, ValidateRemote);
        }

        private bool ValidateRemote(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
    }
}
