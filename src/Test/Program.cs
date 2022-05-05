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

			client.Disconnected += async () =>
			{
				Wlc("Disconnected", ConsoleColor.Red);

				await Task.Delay(2000);
			};

			client.IrcMessageReceived += (msg) =>
			{
				Console.WriteLine(msg.ToString());
			};



			Wlc("REGISTERD FOR REAL", ConsoleColor.Yellow);

			await Task.Delay(Timeout.Infinite);
		}

		static void Wlc(object o, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(o.ToString());
			Console.ResetColor();
		}
	}
}