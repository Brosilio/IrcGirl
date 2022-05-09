using System;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Events
{
	public class IrcErrorReplyEventArgs : EventArgs
	{
		public IrcReplyCode IrcReplyCode { get; private set; }
		public RawIrcMessage IrcMessage { get; private set; }

		public IrcErrorReplyEventArgs(IrcReplyCode replyCode, RawIrcMessage message)
		{
			this.IrcReplyCode = replyCode;
			this.IrcMessage = message;
		}
	}
}