using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Rpl
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#rplmyinfo-004"/>
    /// </summary>
    [IrcMessage(IrcReplyCode.RPL_MYINFO)]
    public class MyInfoIrcMessage : IrcMessage
    {
        public string Client
        {
            get
            {
                return RawIrcMessage.Parameters[0];
            }
        }

        public string Message
        {
            get
            {
                return RawIrcMessage.Parameters[1];
            }
        }

        public MyInfoIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount < 5 || raw.ParameterCount > 6)
                throw new InvalidIrcMessageException($"Expected 5 or 6 parameters, got {raw.ParameterCount}", raw);
        }
    }
}
