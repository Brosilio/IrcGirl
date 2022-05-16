using System;
using System.Collections.Generic;
using System.Text;

using IrcGirl.Protocol.IrcV3.IrcMessages;

namespace IrcGirl.Events
{
    public class IrcMessageEventArgs<TMessage> : EventArgs where TMessage : IrcMessage
    {
        public TMessage Message { get; set; }

        public IrcMessageEventArgs(TMessage ircMessage)
        {
            this.Message = ircMessage;
        }
    }
}
