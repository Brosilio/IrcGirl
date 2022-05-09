using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl.Net
{
    public interface ISslStreamSource
    {
        SslStream CreateStream(Stream innerStream);

        Task AuthenticateAsClientAsync(SslStream stream, string targetHost);
    }
}
