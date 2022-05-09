using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Client
{
	public static class IrcClientIrcMethods
	{
		/// <summary>
		/// Send a QUIT message to the server.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="reason"></param>
		public static void IrcQuit(this IrcClientBase client, string reason)
		{
			client.Send(new RawIrcMessage("QUIT", reason));
		}

		public static void IrcNick(this IrcClientBase client, string nickName)
		{
			client.Send(new RawIrcMessage("NICK", nickName));
		}

		public static void IrcPass(this IrcClientBase client, string password)
		{
			client.Send(new RawIrcMessage(IrcCommands.PASS, password));
		}

		public static void IrcUser(this IrcClientBase client, string userName, string realName)
		{
			client.Send(new RawIrcMessage("USER", userName, "0", "*", realName));
		}

		public static void IrcJoin(this IrcClientBase client, string channel)
		{
			client.Send(new RawIrcMessage("JOIN", channel));
		}

		public static void IrcPrivMsg(this IrcClientBase client, string message, params string[] targets)
        {
			client.Send(new RawIrcMessage(IrcCommands.PRIVMSG, string.Join(",", targets), $":{message}"));
        }
	}
}
