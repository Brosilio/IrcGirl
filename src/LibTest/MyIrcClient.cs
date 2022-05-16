using System;
using System.Threading.Tasks;
using System.Net.Security;
using System.IO;
using System.Security.Cryptography.X509Certificates;

using IrcGirl;
using IrcGirl.Net;
using IrcGirl.Client;
using IrcGirl.Protocol.IrcV3;
using IrcGirl.Protocol.IrcV3.IrcMessages.Rpl;
using IrcGirl.Protocol.IrcV3.IrcMessages.Commands;
using IrcGirl.Events;

namespace LibTest
{
    public class MyIrcClient : IrcClient
    {
        static async Task Main(string[] args)
        {
            //IrcUser.ParseHostmask("test!test@test.net, out string nick, out string user, out string host);
            //Console.WriteLine(nick);
            //Console.WriteLine(user);
            //Console.WriteLine(host);

            //Console.ReadLine();

            //return;

            MyIrcClient c = new MyIrcClient();
            c.SslStreamSource = new MySslStreamSource();

            await c.ConnectAsync(args[0], int.Parse(args[1]), SslMode.UseSsl);
            c.IrcNick("test");
            c.IrcUser("test", "test");

            await Task.Delay(-1);
        }

        protected override void OnDisconnected(IrcDisconnectedEventArgs e)
        {
            Console.WriteLine("!!Disconnected!!");
        }

        protected override void OnRawIrcMessageReceived(RawIrcMessage message)
        {
            if (message.IsCtcpMessage())
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }

            //Console.WriteLine($"CTCP: {message.IsCtcpMessage()}");
            Console.WriteLine(message.Serialize());

            Console.ResetColor();
        }

        protected override void OnIrcWelcome(WelcomeIrcMessage e)
        {
            this.IrcJoin("#test");
        }

        protected override void OnIrcPrivMsg(IrcUser from, PrivMsgIrcMessage msg)
        {
            //Console.WriteLine($"PrivMsg from {from.NickName} ({from.UserName}@{from.HostName})");

            if (msg.Target == Me.NickName)
                this.IrcPrivMsg(msg.Content, from.NickName);
            else
                this.IrcPrivMsg(msg.Content, msg.Target);
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