using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	internal sealed class IrcMessageSinkAttribute : Attribute
	{
		/// <summary>
		/// The command.
		/// </summary>
		public string Command { get; private set; }

		/// <summary>
		/// The reply code.
		/// </summary>
		public IrcReplyCode IrcReplyCode { get; private set; }

		/// <summary>
		/// If true, an exception will be raised.
		/// </summary>
		public bool Throw { get; set; }

		public IrcMessageSinkAttribute(string command)
		{
			Command = command;
		}

		public IrcMessageSinkAttribute(IrcReplyCode replyCode)
        {
			IrcReplyCode = replyCode;
        }
	}
}
