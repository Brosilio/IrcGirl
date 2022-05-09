using System;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Events
{
	public class MessageLoopExceptionEventArgs : EventArgs
	{
		public Exception Exception { get; private set; }
		public IrcMessage IrcMessage { get; private set; }

		public MessageLoopExceptionEventArgs()
		{
		}

		public MessageLoopExceptionEventArgs(Exception exception, IrcMessage ircMessage)
		{
			this.Exception = exception;
			this.IrcMessage = ircMessage;
		}
	}
}