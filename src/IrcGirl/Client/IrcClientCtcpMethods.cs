using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using IrcGirl.Protocol.Ctcp;
using IrcGirl.Protocol.IrcV3;
using static IrcGirl.Protocol.Ctcp.CtcpCommand;

namespace IrcGirl.Client
{
	public static class IrcClientCtcpMethods
	{
		/// <summary>
		/// Send a CTCP pong to the specified target.
		/// </summary>
		/// <param name="client"></param>
		/// <param name="target"></param>
		/// <param name="tag"></param>
		public static void CtcpPong(this IrcClient client, string target, string tag)
        {
			client.Send(new RawCtcpMessage(PONG, tag).ToQuery(target));
        }
	}
}
