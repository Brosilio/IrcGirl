using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.Ctcp.CtcpMessages
{
    [CtcpMessage(CtcpCommand.PING)]
    public class PingCtcpMessage : CtcpMessage
    {
        public string PingTag
        {
            get
            {
                return RawCtcpMessage.ParameterString;
            }
        }

        public PingCtcpMessage(RawCtcpMessage raw) : base(raw)
        {

        }
    }
}
