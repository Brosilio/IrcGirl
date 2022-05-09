using System;
using System.Collections.Generic;
using System.Text;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Events
{
	public class RawIrcMessageEventArgs : EventArgs
	{
		/// <summary>
		/// The relevant IRC message.
		/// </summary>
		public RawIrcMessage RawIrcMessage { get; private set; }

		public RawIrcMessageEventArgs(RawIrcMessage ircMessage)
		{
			RawIrcMessage = ircMessage;
		}
	}
}
