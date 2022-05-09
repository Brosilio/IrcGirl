using System;
using System.Collections.Generic;
using System.Text;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Events
{
	public class IrcMessageEventArgs : EventArgs
	{
		public IrcMessage IrcMessage { get; private set; }

		public IrcMessageEventArgs(IrcMessage ircMessage)
		{
			IrcMessage = ircMessage;
		}
	}
}
