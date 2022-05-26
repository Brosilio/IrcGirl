using IrcGirl.Protocol.IrcV3;
using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
    public class IrcUser
    {
        /// <summary>
        /// The hostmask for this user.
        /// </summary>
        public Hostmask Hostmask { get; private set; }
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
            Hostmask = Hostmask.Parse(hostmask);
        }

        /// <summary>
        /// Send a PrivMsg to this user.
        /// </summary>
        /// 
        /// <param name="message">The message to send to the user.</param>
        public void PrivMsg(string message)
        {
            _client.IrcPrivMsg(message, Hostmask.Nickname);
        }
    }
}
