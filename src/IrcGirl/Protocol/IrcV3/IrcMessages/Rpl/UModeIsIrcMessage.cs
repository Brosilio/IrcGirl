using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Rpl
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#rplumodeis-221"/>
    /// </summary>
    [IrcMessage(IrcReplyCode.RPL_UMODEIS)]
    public class UModeIsIrcMessage : IrcMessage
    {
        public string Client
        {
            get
            {
                return RawIrcMessage.Parameters[0];
            }
        }

        public string UserModes
        {
            get
            {
                return RawIrcMessage.Parameters[1];
            }
        }

        public UModeIsIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 2)
                throw InvalidIrcMessageException.WrongParamCount(2, raw);
        }
    }
}
