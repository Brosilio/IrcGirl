using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3.IrcMessages.Rpl
{
    /// <summary>
    /// <see href="https://modern.ircdocs.horse/#rplbounce-010"/>
    /// </summary>
    [IrcMessage(IrcReplyCode.RPL_BOUNCE)]
    public class BounceIrcMessage : IrcMessage
    {
        public string Client
        {
            get
            {
                return RawIrcMessage.Parameters[0];
            }
        }

        public string Hostname
        {
            get
            {
                return RawIrcMessage.Parameters[1];
            }
        }

        public int Port
        {
            get
            {
                return int.Parse(RawIrcMessage.Parameters[2]);
            }
        }


        public string Info
        {
            get
            {
                if (RawIrcMessage.ParameterCount < 4)
                    return null;

                return RawIrcMessage.Parameters[3];
            }
        }

        public BounceIrcMessage(RawIrcMessage raw) : base(raw)
        {
            if (raw.ParameterCount < 3 || raw.ParameterCount > 4)
                throw new InvalidIrcMessageException($"Expected 3 or 4 parameters, got {raw.ParameterCount}", raw);
        }
    }
}
