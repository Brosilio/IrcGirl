using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
    internal class IrcMessageSinkContainer
    {
        internal string Command { get; set; }
        internal IrcReplyCode ReplyCode { get; set; }
        internal bool Error { get; set; }
        internal IrcClientBase.MessageSinkDelegate Delegate { get; set; }
        internal string MethodName => Delegate.Method.Name;

        internal string GetName()
        {
            if (Command == null)
                return ((int)ReplyCode).ToString("D3");

            return Command.ToUpper();
        }
    }
}
