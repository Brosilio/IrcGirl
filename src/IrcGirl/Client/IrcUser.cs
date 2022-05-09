using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
    public class IrcUser
    {
        public string UserName { get; internal set; }
        public string RealName { get; internal set; }
        public string NickName { get; internal set; }

        private IrcClient _client;

        internal IrcUser(IrcClient from)
        {
            _client = from;
        }
    }
}
