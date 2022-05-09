using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3
{
	public interface IIrcMessageValidator
	{
		bool ValidateOutbound(IrcMessage message);
		bool ValidateInbound(IrcMessage message);
	}
}
