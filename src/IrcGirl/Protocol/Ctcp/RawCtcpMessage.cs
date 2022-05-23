using IrcGirl.Protocol.IrcV3;
using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.Ctcp
{
    public class RawCtcpMessage
    {
        public RawIrcMessage RawIrcMessage;
        public string Command;
        public string ParameterString;

        public RawCtcpMessage()
        {

        }

        public RawCtcpMessage(string command, string parameterString)
        {
            this.Command = command;
            this.ParameterString = parameterString;
        }

        /// <summary>
        /// Serialize this instance to a <see cref="RawIrcMessage"/> with the command set to PRIVMSG.
        /// </summary>
        /// 
        /// <returns></returns>
        public RawIrcMessage ToQuery(params string[] targets)
        {
            RawIrcMessage raw = ToRawIrcMessage(targets);
            raw.Command = "PRIVMSG";

            return raw;
        }

        /// <summary>
        /// Serialize this instance to a <see cref="RawIrcMessage"/> with the command set to NOTICE.
        /// </summary>
        /// 
        /// <returns></returns>
        public RawIrcMessage ToReply(params string[] targets)
        {
            RawIrcMessage raw = ToRawIrcMessage(targets);
            raw.Command = "NOTICE";

            return raw;
        }

        private RawIrcMessage ToRawIrcMessage(params string[] targets)
        {
            if (string.IsNullOrWhiteSpace(Command))
                throw new InvalidCtcpMessageException("RawCtcpMessage.Command must not be null, empty, or whitespace");

            RawIrcMessage raw = new RawIrcMessage(2);
            raw.Parameters[0] = string.Join(",", targets);
            
            StringBuilder sb = new StringBuilder();
            sb.Append('\x01')
                .Append(Command)
                .Append(' ')
                .Append(ParameterString)
                .Append('\x01');

            raw.Parameters[1] = sb.ToString();

            return raw;
        }
    }
}
