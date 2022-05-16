using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Commands
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#quit-message"/>
    /// </summary>
    [IrcMessage(IrcCommands.QUIT)]
    public class QuitIrcMessage : IrcMessage
    {
        /// <summary>
        /// The reason for quitting.
        /// </summary>
        public string Reason
        {
            get
            {
                if (RawIrcMessage.Parameters[0] == null)
                    return null;

                return RawIrcMessage.Parameters[0];
            }

            set
            {
                RawIrcMessage.Parameters[0] = value;
            }
        }

        public QuitIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount != 1)
                throw InvalidIrcMessageException.WrongParamCount(1, RawIrcMessage);
        }

        /// <summary>
        /// Initialize a new QuitIrcMessage with a custom quit reason.
        /// </summary>
        /// 
        /// <param name="reason">The reason for quitting.</param>
        public QuitIrcMessage(string reason)
        {
            RawIrcMessage = new RawIrcMessage("QUIT", reason);
        }

        /// <summary>
        /// Initialize a new QuitIrcMessage using the default quit reason of "Leaving".
        /// </summary>
        public QuitIrcMessage() : this("Leaving") { }
    }
}
