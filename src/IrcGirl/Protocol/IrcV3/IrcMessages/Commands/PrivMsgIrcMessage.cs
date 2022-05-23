using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Commands
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#quit-message"/>
    /// </summary>
    [IrcMessage(IrcCommand.PRIVMSG)]
    public class PrivMsgIrcMessage : IrcMessage
    {
        /// <summary>
        /// The content of the PrivMessage.
        /// </summary>
        public string Content
        {
            get
            {
                return RawIrcMessage.Parameters[1];
            }
        }

        /// <summary>
        /// The source of the messsage (probably some user's nickname!hostname).
        /// </summary>
        public string Source
        {
            get
            {
                return RawIrcMessage.Prefix;
            }
        }

        /// <summary>
        /// The target of the message (either a nickname or the name of a #channel).
        /// </summary>
        public string Target
        {
            get
            {
                return RawIrcMessage.Parameters[0];
            }
        }


        public PrivMsgIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 2)
                throw InvalidIrcMessageException.WrongParamCount(2, RawIrcMessage);
        }
    }
}
