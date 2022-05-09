using System;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Events
{
	public class IrcErrorReplyEventArgs : EventArgs
	{
		public IrcReplyCode IrcReplyCode { get; private set; }
		public IrcMessage IrcMessage { get; private set; }

		public IrcErrorReplyEventArgs(IrcReplyCode replyCode, IrcMessage message)
		{
			this.IrcReplyCode = replyCode;
			this.IrcMessage = message;
		}
	}
}