﻿using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class IrcMessageAttribute : Attribute
    {
        public string Command { get; }


        public IrcMessageAttribute(string command)
        {
            this.Command = command;
        }

        public IrcMessageAttribute(IrcReplyCode replyCode) : this(((int)replyCode).ToString("D3"))
        {
        }
    }
}
