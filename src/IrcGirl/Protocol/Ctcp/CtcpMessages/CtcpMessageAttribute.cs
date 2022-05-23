using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.Ctcp.CtcpMessages
{
    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    sealed class CtcpMessageAttribute : Attribute
    {
        public string Command { get; }

        public CtcpMessageAttribute(string command)
        {
            this.Command = command;
        }
    }
}
