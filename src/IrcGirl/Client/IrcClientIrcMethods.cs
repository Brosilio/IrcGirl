using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using IrcGirl.Protocol.IrcV3;
using static IrcGirl.Protocol.IrcV3.IrcCommand;

namespace IrcGirl.Client
{
	public static class IrcClientIrcMethods
	{
		/// <summary>
		/// Send a QUIT message to the server.
		/// </summary>
		/// 
		/// <param name="client">The client to send the command with.</param>
		/// <param name="reason">The reason for quitting.</param>
		public static void IrcQuit(this IrcClient client, string reason)
		{
			client.Send(new RawIrcMessage(QUIT, reason));
		}

		/// <summary>
		/// Send a NICK message to the server.
		/// </summary>
		/// 
		/// <param name="client">The client to send the command with.</param>
		/// <param name="nickName">The nickname to use.</param>
		public static void IrcNick(this IrcClient client, string nickName)
		{
			client.Send(new RawIrcMessage(NICK, nickName));
			client.Me.NickName = nickName;
		}

		/// <summary>
		/// Send a PASS message to the server.
		/// </summary>
		/// 
		/// <param name="client">The client to send the command with.</param>
		/// <param name="password">The password to send.</param>
		public static void IrcPass(this IrcClient client, string password)
		{
			client.Send(new RawIrcMessage(PASS, password));
		}

		public static void IrcUser(this IrcClient client, string userName, string realName)
		{
			client.Send(new RawIrcMessage(USER, userName, "0", "*", realName));
		}

		public static void IrcJoin(this IrcClient client, string channel)
		{
			client.Send(new RawIrcMessage(JOIN, channel));
		}

		public static void IrcPing(this IrcClient client, string token)
        {
			client.Send(new RawIrcMessage(PING, token));
        }

		public static void IrcPong(this IrcClient client, string token)
        {
			client.Send(new RawIrcMessage(PONG, token));
        }

		public static void IrcPrivMsg(this IrcClient client, string message, params string[] targets)
        {
			client.Send(new RawIrcMessage(PRIVMSG, string.Join(",", targets), message));
        }
	}
}
