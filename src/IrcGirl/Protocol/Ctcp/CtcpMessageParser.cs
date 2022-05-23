using System;
using System.Collections.Generic;
using System.Text;

using IrcGirl.Protocol.Ctcp.CtcpMessages;
using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Protocol.Ctcp
{
    public unsafe class CtcpMessageParser
    {
        public bool TryUnbox(RawIrcMessage rim, out RawCtcpMessage rcm)
        {
            //if (string.IsNullOrWhiteSpace(rim.Command))
            //    return null;

            rcm = null;

            if (rim.ParameterCount == 0)
                return false;

            if (!IrcCommand.PRIVMSG.Equals(rim.Command, StringComparison.OrdinalIgnoreCase)
                && !IrcCommand.NOTICE.Equals(rim.Command, StringComparison.OrdinalIgnoreCase))
                return false;

            string trailer = rim.Parameters[rim.ParameterCount - 1];

            if (string.IsNullOrWhiteSpace(trailer))
                return false;

            fixed (char* p = trailer)
            {
                if (*p != '\x01')
                    return false;

                int tLen = trailer.Length;
                int endDelimiter = 0;

                if (*(p + tLen - 1) == '\x01')
                    endDelimiter = 1;

                rcm = new RawCtcpMessage();

                for (int i = 1; i < tLen; i++)
                {
                    for (; i < tLen && *(p + i) == ' '; i++) ;

                    rcm.Command = ReadWord(p, ref i, ref tLen);

                    for (; i < tLen && *(p + i) == ' '; i++) ;

                    rcm.ParameterString = new string(p, i, tLen - i - endDelimiter);
                    break;
                }

                rcm.RawIrcMessage = rim;
                return true;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private static string ReadWord(char* c, ref int pos, ref int len)
        {
            int resultLen = 0;
            for (; resultLen + pos < len; resultLen++)
            {
                if (*(c + pos + resultLen) == ' ') break;
            }

            string result = new string(c, pos, resultLen);

            pos += resultLen;

            return result;

        }
    }
}
