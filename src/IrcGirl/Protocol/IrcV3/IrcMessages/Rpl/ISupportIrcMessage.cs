using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Rpl
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#rplisupport-005"/>
    /// </summary>
    [IrcMessage(IrcReplyCode.RPL_ISUPPORT)]
    public class ISupportIrcMessage : IrcMessage
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

        public ISupportIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 2)
                throw new InvalidIrcMessageException($"Expected 2 parameters, got {raw.ParameterCount}", raw);
        }
    }
}
