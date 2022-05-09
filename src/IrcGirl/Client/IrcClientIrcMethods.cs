using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Client
{
	public static class IrcClientIrcMethods
	{
		public static void IrcQuit(this IrcClient client, string reason)
		{
			client.Send(new IrcMessage("QUIT", reason));
		}

		public static void IrcNick(this IrcClient client, string nickName)
		{
			client.Send(new IrcMessage("NICK", nickName));
		}

		public static void IrcPass(this IrcClient client, string password)
		{
			client.Send(new IrcMessage("PASS", password));
		}

		public static void IrcUser(this IrcClient client, string userName, string realName)
		{
			client.Send(new IrcMessage("USER", userName, "0", "*", realName));
		}

		public static void IrcJoin(this IrcClient client, string channel)
		{
			client.Send(new IrcMessage("JOIN", channel));
		}

		/// <summary>
		/// Send a QUIT message to the server.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="reason"></param>
		/// <returns></returns>
		public static async Task IrcQuitAsync(this IrcClient client, string reason)
		{

		}
	}
}
