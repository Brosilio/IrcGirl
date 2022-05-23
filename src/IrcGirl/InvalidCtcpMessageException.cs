using System;
using System.Collections.Generic;
using System.Text;

using IrcGirl.Protocol.Ctcp;
using IrcGirl.Protocol.IrcV3;

namespace IrcGirl
{

    [Serializable]
    public class InvalidCtcpMessageException : Exception
    {
        /// <summary>
        /// The associated <see cref="RawCtcpMessage"/> (if any).
        /// </summary>
        public RawCtcpMessage RawCtcpMessage { get; private set; }

        public InvalidCtcpMessageException() { }
        public InvalidCtcpMessageException(RawCtcpMessage rawCtcpMessage)
        {
            this.RawCtcpMessage = rawCtcpMessage;
        }

        public InvalidCtcpMessageException(string message) : base(message) { }
        public InvalidCtcpMessageException(string message, RawCtcpMessage rawCtcpMessage) : base(message)
        {
            this.RawCtcpMessage = rawCtcpMessage;
        }

        public InvalidCtcpMessageException(string message, Exception inner) : base(message, inner) { }
        public InvalidCtcpMessageException(string message, Exception inner, RawCtcpMessage rawCtcpMessage) : base(message, inner)
        {
            this.RawCtcpMessage = rawCtcpMessage;
        }

        protected InvalidCtcpMessageException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
