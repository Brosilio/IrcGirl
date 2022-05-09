using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Rpl
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#rplwelcome-001"/>
    /// </summary>
    [IrcMessage(IrcReplyCode.RPL_WELCOME)]
    public class WelcomeIrcMessage : IrcMessage
    {
        public string Client
        {
            get
            {
                return RawIrcMessage.Parameters[0];
            }
        }

        public string WelcomeMessage
        {
            get
            {
                return RawIrcMessage.Parameters[1];
            }
        }

        public WelcomeIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 2)
                throw InvalidIrcMessageException.WrongParamCount(2, raw);
        }
    }
}
