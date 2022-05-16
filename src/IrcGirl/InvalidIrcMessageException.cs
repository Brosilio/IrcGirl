using IrcGirl.Protocol.IrcV3;
using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl
{

    [Serializable]
    public class InvalidIrcMessageException : Exception
    {
        /// <summary>
        /// The associated <see cref="RawIrcMessage"/> (if any).
        /// </summary>
        public RawIrcMessage RawIrcMessage { get; private set; }

        public InvalidIrcMessageException() { }
        public InvalidIrcMessageException(RawIrcMessage rawIrcMessage)
        {
            this.RawIrcMessage = rawIrcMessage;
        }

        public InvalidIrcMessageException(string message) : base(message) { }
        public InvalidIrcMessageException(string message, RawIrcMessage rawIrcMessage) : base(message)
        {
            this.RawIrcMessage = rawIrcMessage;
        }

        public InvalidIrcMessageException(string message, Exception inner) : base(message, inner) { }
        public InvalidIrcMessageException(string message, Exception inner, RawIrcMessage rawIrcMessage) : base(message, inner)
        {
            this.RawIrcMessage = rawIrcMessage;
        }

        protected InvalidIrcMessageException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        /// <summary>
        /// Shorthand for indicating the message has an invalid number of parameters.
        /// </summary>
        /// 
        /// <param name="expected">How many parameters were expected.</param>
        /// <param name="message">The original message.</param>
        /// 
        /// <returns>
        /// A new <see cref="InvalidIrcMessageException"/> with a human-readable message.
        /// </returns>
        public static InvalidIrcMessageException WrongParamCount(int expected, RawIrcMessage message)
        {
            return new InvalidIrcMessageException(
                string.Format("IrcMessage '{0}' expects {1} parameters, got {2}", message.Command, expected, message.ParameterCount),
                message
            );
        }
    }
}
