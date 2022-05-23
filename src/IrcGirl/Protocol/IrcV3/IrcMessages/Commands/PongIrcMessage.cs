using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Commands
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#pong-message"/>
    /// </summary>
    [IrcMessage(IrcCommand.PONG)]
    public class PongIrcMessage : IrcMessage
    {
        /// <summary>
        /// The pong token. Should be identical to the token received in a PING message.
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

        public PongIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 1)
                throw InvalidIrcMessageException.WrongParamCount(1, RawIrcMessage);
        }

        public PongIrcMessage(string token)
        {
            this.RawIrcMessage = new RawIrcMessage(IrcCommand.PONG, token);
        }
    }
}
