using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3
{
	public class IrcMessageValidator : IIrcMessageValidator
	{
		public IrcMessageValidator()
		{
		}

		public bool ValidateInbound(IrcMessage message)
		{
			throw new NotImplementedException();
		}

		public bool ValidateOutbound(IrcMessage message)
		{
			return true;
		}

		protected void ValidateNick()
		{

		}
	}
}
