using IrcGirl.Protocol.IrcV3.IrcMessages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl.Protocol.IrcV3
{
    /// <summary>
    /// Represents a "raw" IRC message - that is, a message which has been parsed but not validated.
    /// 
    /// The direct usage of this class is probably not safe. The fields are left accessible and unverified
    /// for speed - use at your own risk.
    /// </summary>
    public class RawIrcMessage
    {
        /// <summary>
        /// The string determined to be the tag portion of the message
        /// 
        /// Does not include the leading @ symbol (and don't prepend one if you are constructing a message).
        /// </summary>
        public string Tag;

        /// <summary>
        /// The prefix portion of the message.
        /// </summary>
        public string Prefix;

        /// <summary>
        /// The command or reply code of the message.
        /// </summary>
        public string Command;

        /// <summary>
        /// The parameters of the message. Do not rely on <see cref="Parameters.Length"/>, instead use
        /// <see cref="ParameterCount"/>.
        /// 
        /// The last parameter will be the trailing parameter. On <see cref="Serialize"/>, a colon will be prefixed to the
        /// last parameter if it contains spaces.
        /// </summary>
        public string[] Parameters;

        /// <summary>
        /// The actual number of parameters in <see cref="Parameters"/>.
        /// </summary>
        public int ParameterCount;

        public RawIrcMessage()
        {
            Parameters = new string[IrcMessageParser.MAX_PARAMS];
        }

        public RawIrcMessage(string command, params string[] parameters)
        {
            this.Command = command;
            this.Parameters = parameters;
            this.ParameterCount = parameters.Length;

            for (int i = 0; i < Parameters.Length - 1; i++)
                if (Parameters[i].Contains(" "))
                    throw new ArgumentException("Only the last parameter of a message can contain spaces");
        }

        /// <summary>
        /// Return <see cref="Command"/> as <see cref="IrcReplyCode" /> if possible,
        /// or <see cref="IrcReplyCode.None"/> if not possible.
        /// </summary>
        /// 
        /// <returns>
        /// An <see cref="IrcReplyCode"/> or <see cref="IrcReplyCode.None"/>.
        /// </returns>
        public IrcReplyCode AsReplyCode()
        {
            if (TryGetReplyCode(out IrcReplyCode code))
                return code;

            return IrcReplyCode.None;
        }

        /// <summary>
        /// Try to turn <see cref="Command"/> into a valid <see cref="IrcReplyCode"/>.
        /// </summary>
        /// 
        /// <param name="code">The code, if any.</param>
        /// 
        /// <returns>
        /// <see langword="true"/> if the code was valid, <see langword="false"/> if not.
        /// </returns>
        public bool TryGetReplyCode(out IrcReplyCode code)
        {
            code = IrcReplyCode.None;

            if (Command == null || Command.Length != 3)
                return false;

            if (int.TryParse(Command, out int i) && Enum.IsDefined(typeof(IrcReplyCode), i))
            {
                code = (IrcReplyCode)i;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if this message's command is both a reply code and represents an error reply code.
        /// </summary>
        /// 
        /// <returns>
        /// <see langword="true"/> if this message represents an error reply code, <see langword="false"/> otherwise.
        /// </returns>
        public bool IsErrorReply()
        {
            return AsReplyCode().ToString().StartsWith("ERR_", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns true if this message represents a CTCP message.
        /// </summary>
        /// 
        /// <returns>
        /// <see langword="true"/> if this is a CTCP message, <see langword="false"/> if not.
        /// </returns>
        public bool IsCtcpMessage()
        {
            if (string.IsNullOrWhiteSpace(Command))
                return false;

            if (!IrcCommands.PRIVMSG.Equals(Command, StringComparison.OrdinalIgnoreCase)
                && !IrcCommands.NOTICE.Equals(Command, StringComparison.OrdinalIgnoreCase))
                return false;

            return Parameters[ParameterCount - 1][0] == '\x01';
        }

        /// <summary>
        /// Serialize this to an IRC message line that conforms to the protocol. Does not add the line terminator (\r\n).
        /// Messages are checked for syntactic and semantic protocol conformity.
        /// </summary>
        /// 
        /// <returns>A string representing the IRC message.</returns>
        /// 
        /// <exception cref="InvalidIrcMessageException"/>
        public unsafe string Serialize()
        {
            if (string.IsNullOrWhiteSpace(Command))
                throw new InvalidIrcMessageException("RawIrcMessage.Command was null, empty, or whitespace");

            fixed (char* p = Command)
                for (int i = 0; i < Command.Length; i++)
                    if (!char.IsLetterOrDigit(*(p + i)))
                        throw new InvalidIrcMessageException("RawIrcMessage.Command can only contain letters and/or digits");

            // check that every parameter does not contain a space except for the last one
            for (int pIdx = 0; pIdx < ParameterCount - 1; pIdx++)
            {
                int pLen = Parameters[pIdx].Length;

                fixed (char* p = Parameters[pIdx])
                {
                    for (int i = 0; i < pLen; i++)
                        if (*(p + i) == ' ')
                            throw new InvalidIrcMessageException("Only the last parameter in RawIrcMessage.Parameters can contain a space");
                }
            }

            // create an instance of this irc message just to validate it.
            // if it's null then we don't know what kind of message it is.
            // this throws the invalid irc message exception if the message
            // isn't valid.
            IrcMessage message = IrcMessage.CreateInstance(this);

            StringBuilder sb = new StringBuilder();

            // append the tag, if any
            if (!string.IsNullOrWhiteSpace(Tag))
            {
                sb.Append('@')
                    .Append(Tag)
                    .Append(' ');
            }

            // append the prefix, if any
            if (!string.IsNullOrWhiteSpace(Prefix))
            {
                sb.Append(':')
                    .Append(Prefix)
                    .Append(' ');
            }

            // append command
            sb.Append(Command);

            // append and validate parameters
            for (int pIdx = 0; pIdx < ParameterCount; pIdx++)
            {
                // separate command and parameters
                sb.Append(' ');

                int pLen = Parameters[pIdx].Length;

                fixed (char* p = Parameters[pIdx])
                {
                    for (int i = 0; i < pLen; i++)
                    {
                        // if the current parameter contains a space and it isn't the last
                        // parameter, throw
                        if (*(p + i) == ' ')
                        {
                            if (pIdx == ParameterCount - 1)
                            {
                                // otherwise add a colon because it's the trailing parameter
                                sb.Append(':');
                                break;
                            }
                            else
                            {
                                throw new InvalidIrcMessageException("Only the last parameter in RawIrcMessage.Parameters can contain a space");
                            }
                        }
                    }

                    // append the parameter
                    sb.Append(p, pLen);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Return a string representation of this message. Will produce the same result as <see cref="Serialize"/>,
        /// except ToString() does NOT perform any syntactic or semantic validation.
        /// </summary>
        /// 
        /// <returns>A string representing this message.</returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            //sb.AppendFormat("{0} {1} ", Prefix, Command);
            sb.AppendFormat(string.Join(" ", Prefix, Command));

            for (int i = 0; i < ParameterCount; i++)
            {
                sb.AppendFormat(" {0}", Parameters[i]);
            }

            return sb.ToString();
        }
    }
}
