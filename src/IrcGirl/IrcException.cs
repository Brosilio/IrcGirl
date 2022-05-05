using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl
{

	[Serializable]
	public class IrcException : Exception
	{
		public int ReplyCode { get; set; }

		public IrcException() { }

		public IrcException(string message) : base(message) { }

		public IrcException(string message, int replyCode) : base(message)
		{
			this.ReplyCode = replyCode;
		}

		public IrcException(string message, int replyCode, Exception inner) : base(message, inner)
		{
			this.ReplyCode = replyCode;
		}

		public IrcException(string message, Exception inner) : base(message, inner) { }

		protected IrcException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

		private static Dictionary<int, string> _codeToMessage = new Dictionary<int, string>()
		{
			{ 000, "" }
		};


		public static IrcException FromReplyCode(int replyCode)
		{

		}
	}
}
