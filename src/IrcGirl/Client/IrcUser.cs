using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
    public class IrcUser
    {
        public string NickName { get; internal set; }
        public string UserName { get; internal set; }
        public string HostName { get; internal set; }
        public string RealName { get; internal set; }

        private IrcClient _client;

        /// <summary>
        /// Create an instance of IrcUser.
        /// </summary>
        /// 
        /// <param name="connection">The connection this IrcUser is available on.</param>
        internal IrcUser(IrcClient connection)
        {
            _client = connection;
        }

        /// <summary>
        /// Create an instance of IrcUser with the specified hostmask.
        /// </summary>
        /// 
        /// <param name="connection">The connection this IrcUser is available on.</param>
        /// <param name="hostmask">The hostmask string.</param>
        internal IrcUser(IrcClient connection, string hostmask)
        {
            _client = connection;

            string nickName = null;
            string userName = null;
            string hostName = null;

            ParseHostmask(hostmask, out nickName, out userName, out hostName);

            this.NickName = nickName;
            this.UserName = userName;
            this.HostName = hostName;
        }

        public unsafe static void ParseHostmask(string hostmask, out string nickName, out string userName, out string hostName)
        {
            nickName = null;
            userName = null;
            hostName = null;

            int hLen = hostmask.Length;
            int partLen = 0;
            fixed (char* p = hostmask)
            {
                for (int i = 0; i < hLen; i++)
                {
                    char pc = *(p + i);

                    if (pc == '!' || partLen == hLen)
                    {
                        nickName = new string(p, 0, partLen);
                        partLen = 0;
                        continue;
                    }

                    if (pc == '@')
                    {
                        userName = new string(p, i - partLen, partLen);
                        hostName = new string(p, i + 1, hLen - i - 1);
                        break;
                    }

                    partLen++;
                }
            }
        }

        /// <summary>
        /// Send a PrivMsg to this user.
        /// </summary>
        /// 
        /// <param name="message">The message to send to the user.</param>
        public void PrivMsg(string message)
        {
            _client.IrcPrivMsg(message, NickName);
        }
    }
}
