using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl
{

    [Serializable]
    public class IrcException : Exception
    {
        public int ReplyCode { get; set; }

        /// <summary>
        /// Get the reply code as <see cref="IrcReplyCode"/>.
        /// </summary>
        public IrcReplyCode IrcReplyCode => (IrcReplyCode)this.ReplyCode;

        public IrcException() { }

        public IrcException(IrcReplyCode replyCode) : base(replyCode.ToString())
        {
            this.ReplyCode = (int)replyCode;
        }

        public IrcException(int replyCode) : base(((IrcReplyCode)replyCode).ToString())
        {
            this.ReplyCode = replyCode;
        }

        public IrcException(string message) : base(message) { }

        public IrcException(int replyCode, string message) : base(message)
        {
            this.ReplyCode = replyCode;
        }

        public IrcException(IrcReplyCode replyCode, string message) : base(message)
        {
            this.ReplyCode = (int)replyCode;
        }

        public IrcException(int replyCode, string message, Exception inner) : base(message, inner)
        {
            this.ReplyCode = replyCode;
        }

        public IrcException(string message, Exception inner) : base(message, inner) { }

        protected IrcException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
