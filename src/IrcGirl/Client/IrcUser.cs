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

        private IrcClientBase _client;

        internal IrcUser(IrcClientBase from)
        {
            _client = from;
        }
    }
}
