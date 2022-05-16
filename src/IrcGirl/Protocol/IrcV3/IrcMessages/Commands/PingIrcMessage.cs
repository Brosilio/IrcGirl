using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Commands
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#ping-message"/>
    /// </summary>
    [IrcMessage(IrcCommands.PING)]
    public class PingIrcMessage : IrcMessage
    {
        /// <summary>
        /// The ping token.
        /// </summary>
        public string Token
        {
            get
            {
                return RawIrcMessage.Parameters[0];
            }

            set
            {
                RawIrcMessage.Parameters[0] = value;
            }
        }

        public PingIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 1)
                throw InvalidIrcMessageException.WrongParamCount(1, RawIrcMessage);
        }

        public PingIrcMessage(string token)
        {
            this.RawIrcMessage = new RawIrcMessage(IrcCommands.PING, token);
        }
    }
}
