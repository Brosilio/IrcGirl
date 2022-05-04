using System;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using IrcGirl;
using IrcGirl.Client;

namespace Test
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            IrcClient client = new IrcClient();
            client.SslStreamSource = new MySslStreamSource();

            await client.ConnectAsync("itkb.cmcc.edu", 6667, ESslMode.UseSsl);
            Console.WriteLine("Connected");

            await client.RegisterAsync();
            Console.WriteLine("Registered");

            await Task.Delay(Timeout.Infinite);
        }
    }
}