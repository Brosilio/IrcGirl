using System;

using IrcGirl;
using IrcGirl.Net;
using IrcGirl.Client;
using IrcGirl.Events;
using IrcGirl.Protocol.IrcV3;

using System.Threading.Tasks;
using System.Net.Security;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using IrcGirl.Protocol.IrcV3.IrcMessages.Rpl;

namespace LibTest
{
    public class MyIrcClient : IrcClientBase
    {
        static async Task Main(string[] args)
        {
            MyIrcClient c = new MyIrcClient();
            c.SslStreamSource = new MySslStreamSource();

            await Task.Delay(-1);
        }

        protected override void OnRawIrcMessageReceived(RawIrcMessageEventArgs e)
        {
            //Console.WriteLine($"'{e.RawIrcMessage}'");
        }

        protected override void OnIrcWelcome(WelcomeIrcMessage e)
        {

        }
    }

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