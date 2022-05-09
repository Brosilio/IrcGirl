using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Rpl
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#rplcreated-003"/>
    /// </summary>
    [IrcMessage(IrcReplyCode.RPL_CREATED)]
    public class CreatedIrcMessage : IrcMessage
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

        public CreatedIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 2)
                throw InvalidIrcMessageException.WrongParamCount(2, raw);
        }
    }
}
