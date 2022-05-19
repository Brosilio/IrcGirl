using System;
using System.Collections.Generic;
using System.Text;

using IrcGirl.Protocol.Ctcp.CtcpMessages;
using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Protocol.Ctcp
{
    public unsafe class CtcpMessageParser
    {
        public RawCtcpMessage Unbox(RawIrcMessage rim)
        {
            //if (string.IsNullOrWhiteSpace(rim.Command))
            //    return null;

            if (rim.ParameterCount == 0)
                return null;

            if (!IrcCommands.PRIVMSG.Equals(rim.Command, StringComparison.OrdinalIgnoreCase)
                && !IrcCommands.NOTICE.Equals(rim.Command, StringComparison.OrdinalIgnoreCase))
                return null;

            string trailer = rim.Parameters[rim.ParameterCount - 1];

            if (string.IsNullOrWhiteSpace(trailer))
                return null;

            fixed (char* p = trailer)
            {
                if (*p != '\x01')
                    return null;

                int tLen = trailer.Length;
                RawCtcpMessage rcm = new RawCtcpMessage();

                for (int i = 1; i < tLen; i++)
                {
                    for (; i < tLen && *(p + i) == ' '; i++) ;

                    rcm.Command = ReadWord(p, ref i, ref tLen);

                    for (; i < tLen && *(p + i) == ' '; i++) ;

                    rcm.ParameterString = new string(p, i, tLen - i);
                }

                return rcm;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static string ReadWord(char* c, ref int pos, ref int len)
        {
            int resultLen = 0;
            for (; resultLen + pos < resultLen; resultLen++)
            {
                if (*(c + pos + resultLen) == ' ') break;
            }

            string result = new string(c, pos, resultLen);

            pos += resultLen;

            return result;

        }
    }
}
