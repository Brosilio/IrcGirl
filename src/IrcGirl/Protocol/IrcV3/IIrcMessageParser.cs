using IrcGirl.Protocol.IrcV3;
using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3
{
	public interface IIrcMessageParser
	{
		RawIrcMessage Parse(string raw);
	}
}
