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

        public int Port { get; private set; }

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

            if (int.TryParse(raw.Parameters[2], out int port))
			{
                Port = port;
                if (port < 0 || port > 65535)
                    throw new InvalidIrcMessageException("Port was out of range (0-65535)");
			}
            else
			{
                throw new InvalidIrcMessageException("Expected Parameters[2] to be int, got invalid value", raw);
			}
        }
    }
}
