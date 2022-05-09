using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using IrcGirl;
using IrcGirl.Client;
using IrcGirl.Protocol.IrcV3;

namespace Test
{
	public class Program
	{
		static async Task Main(string[] args)
		{
			string[] messages = new string[]
			{
				"foo bar baz asdf",
				":coolguy foo bar baz asdf",
				"foo bar baz :asdf quux",
				"foo bar baz :",
				"foo bar baz ::asdf",
				":coolguy foo bar baz :asdf quux",
				":coolguy foo bar baz :  asdf quux ",
				":coolguy PRIVMSG bar :lol :) ",
				":coolguy foo bar baz :",
				":coolguy foo bar baz :  ",
				"@a=b;c=32;k;rt=ql7 foo",
				"@a=b\\\\and\\nk;c=72\\s45;d=gh\\:764 foo",
				"@c;h=;a=b :quux ab cd",
				":src JOIN #chan",
				":src JOIN :#chan",
				":src AWAY",
				":src AWAY ",
				":cool\tguy foo bar baz",
				":coolguy!ag@net\x035w\x03ork.admin PRIVMSG foo :bar baz",
				":coolguy!~ag@n\x02et\x0305w\x0fork.admin PRIVMSG foo :bar baz",
				"@tag1=value1;tag2;vendor1/tag3=value2;vendor2/tag4= :irc.example.com COMMAND param1 param2 :param3 param3",
				":irc.example.com COMMAND param1 param2 :param3 param3",
				"@tag1=value1;tag2;vendor1/tag3=value2;vendor2/tag4 COMMAND param1 param2 :param3 param3",
				"COMMAND",
				"@foo=\\\\\\\\\\:\\\\s\\s\\r\\n COMMAND",
				":gravel.mozilla.org 432  #momo :Erroneous Nickname: Illegal characters",
				":gravel.mozilla.org MODE #tckk +n ",
				":services.esper.net MODE #foo-bar +o foobar  ",
				"@tag1=value\\\\ntest COMMAND",
				"@tag1=value\\1 COMMAND",
				"@tag1=value1\\ COMMAND",
				"@tag1=1;tag2=3;tag3=4;tag1=5 COMMAND",
				"@tag1=1;tag2=3;tag3=4;tag1=5;vendor/tag2=8 COMMAND",
				":SomeOp MODE #channel :+i",
				":SomeOp MODE #channel +oo SomeUser :AnotherUser"
			};


			Stopwatch stopwatch = new Stopwatch();
			stopwatch.Start();

			const int SIZE = 10_000_000;
			string[] toParse = new string[SIZE];
			IrcMessage[] parsed = new IrcMessage[SIZE];

			for (int i = 0; i < SIZE; i++)
			{
				toParse[i] = messages[i % messages.Length];
			}

			Console.WriteLine($"Took {stopwatch.Elapsed.TotalSeconds} to build array of {toParse.Length:N0}");

			//IrcMessageParser parser = new IrcMessageParser();

			stopwatch.Restart();

			ParallelOptions opt = new ParallelOptions();
			opt.MaxDegreeOfParallelism = 6;

			Parallel.For(0, SIZE, opt, (i) =>
			{
				//IrcMessageParser parser = new IrcMessageParser();
				var msg = IrcMessageParser.Default.Parse(toParse[i]);
			});

			stopwatch.Stop();
			Console.WriteLine($"Parallel.For took {stopwatch.Elapsed.TotalSeconds} to parse {toParse.Length:N0} ({(float)toParse.Length / stopwatch.Elapsed.TotalSeconds:N2}/sec)");

			stopwatch.Restart();

			for (int i = 0; i < SIZE; i++)
			{
				var msg = IrcMessageParser.Default.Parse(toParse[i]);

			}


			stopwatch.Stop();
			Console.WriteLine($"For took {stopwatch.Elapsed.TotalSeconds} to parse {toParse.Length:N0} ({(float)toParse.Length / stopwatch.Elapsed.TotalSeconds:N2}/sec)");

			Console.ReadLine();

			foreach (string message in messages)
			{
				Console.Clear();

				var m = IrcMessageParser.Default.Parse(message);

				Console.WriteLine($"TAG:{m.Tag}");
				Console.WriteLine($"PRE:{m.Prefix}");
				Console.WriteLine($"CMD:{m.Command}");

				foreach (string arg in m.Parameters)
					if (arg != null)
						Console.WriteLine($"\t'{arg}'");

				//for(int i = 0; i < m.ParamLen; i++)
				//Console.WriteLine($"\t'{m.Parameters[i]}'");

				Console.WriteLine();

				Console.ReadLine();
			}

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