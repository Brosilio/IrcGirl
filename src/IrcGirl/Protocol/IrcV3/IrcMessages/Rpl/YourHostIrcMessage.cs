using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Rpl
{
    /// <summary>
    /// See <see href="https://modern.ircdocs.horse/#rplyourhost-002"/>
    /// </summary>
    [IrcMessage(IrcReplyCode.RPL_YOURHOST)]
    public class YourHostIrcMessage : IrcMessage
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

        public YourHostIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 2)
                throw InvalidIrcMessageException.WrongParamCount(2, raw);
        }
    }
}
