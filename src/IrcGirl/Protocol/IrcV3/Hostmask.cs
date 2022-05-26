using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3
{
    public class Hostmask
    {
        /// <summary>
        /// The nickname portion of this hostmask.
        /// </summary>
        public string Nickname;

        /// <summary>
        /// The username portion of this hostmask. If it starts with a tilde (~), the represented user is not running ident.
        /// </summary>
        public string Username;

        /// <summary>
        /// The hostname portion of this hostmask. May be an IP address or garbage.
        /// </summary>
        public string Hostname;

        /// <summary>
        /// Returns a string representing the Username@Hostname format of this hostmask.
        /// </summary>
        public string UserHost
        {
            get { return $"{Username}@{Hostname}"; }
        }

        /// <summary>
        /// Parse a <see cref="Hostmask"/> from a string.
        /// </summary>
        /// 
        /// <param name="hostmask">The string representing the hostmask in Nickname!Username@Hostname format.</param>
        /// 
        /// <returns>
        /// A new instance of <see cref="Hostmask"/>.
        /// </returns>
        public static unsafe Hostmask Parse(string hostmask)
        {
            string nick = null;
            string user = null;
            string host = null;

            int hLen = hostmask.Length;
            int partLen = 0;
            fixed (char* p = hostmask)
            {
                for (int i = 0; i < hLen; i++)
                {
                    char pc = *(p + i);

                    if (pc == '!' || partLen == hLen)
                    {
                        nick = new string(p, 0, partLen);
                        partLen = 0;
                        continue;
                    }

                    if (pc == '@')
                    {
                        user = new string(p, i - partLen, partLen);
                        host = new string(p, i + 1, hLen - i - 1);
                        break;
                    }

                    partLen++;
                }
            }

            return new Hostmask(nick, user, host);
        }

        /// <summary>
        /// Determine if a Nickname!Username@Hostname matches the provided pattern (*!*@*).
        /// Only simple patterns are supported, this is not regex.
        /// </summary>
        /// 
        /// <param name="pattern">The pattern.</param>
        /// <param name="hostmask">The input hostmask.</param>
        /// 
        /// <returns>
        /// <see langword="true"/> if the hostmask matches the pattern, <see langword="false"/> if it does not.
        /// </returns>
        public static bool Match(string pattern, string hostmask)
        {
            return Utility.PatternMatch.IsMatch(pattern, hostmask);
        }

        /// <summary>
        /// Create a new instance of the <see cref="Hostmask"/> class. The inputs are validated.
        /// </summary>
        /// 
        /// <param name="nickname">The nickname.</param>
        /// <param name="username">The username.</param>
        /// <param name="hostname">The hostname.</param>
        public Hostmask(string nickname, string username, string hostname)
        {
            this.Nickname = nickname;
            this.Username = username;
            this.Hostname = hostname;
        }

        /// <summary>
        /// Check if the <see cref="Username"/> portion of this hostmask indicates ident or not.
        /// <para>The check is performed by testing if the first character of <see cref="Username"/> is a tilde (~).</para>
        /// </summary>
        /// 
        /// <returns>
        /// <see langword="true"/> if ident, <see langword="false"/> if not.
        /// </returns>
        public bool IsIdent()
        {
            if (string.IsNullOrWhiteSpace(Nickname))
                return false;

            return Username[0] != '~';
        }

        /// <summary>
        /// Check if this hostmask instance matches the specified pattern. Simple patterns only (not regex).
        /// </summary>
        /// 
        /// <param name="pattern">The pattern to match.</param>
        /// 
        /// <returns>
        /// <see langword="true"/> if this hostmask instance matches the pattern, <see langword="false"/> if it does not.
        /// </returns>
        public bool Matches(string pattern)
        {
            return Match(pattern, ToString());
        }

        /// <summary>
        /// Returns this hostmask in Nickname!Username@Hostname format.
        /// </summary>
        /// 
        /// <returns>
        /// This hostmask, in Nickname!Username@Hostname format.
        /// </returns>
        public override string ToString()
        {
            return $"{Nickname}!{Username}@{Hostname}";
        }
    }
}
