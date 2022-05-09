using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl.Protocol.IrcV3
{
    public class IrcMessage
    {
        public string Tag;
        public string Prefix;
        public string Command;
        public string[] Parameters;
        public int ParameterCount;

        public IrcMessage()
        {
            Parameters = new string[IrcMessageParser.MAX_PARAMS];
        }

        public IrcMessage(string command, params string[] parameters)
        {
            this.Command = command;
            this.Parameters = parameters;
            this.ParameterCount = parameters.Length;
        }

        public bool IsReplyCode()
        {
            return Command != null && Command.Length == 3 && int.TryParse(Command, out _);
        }

        public IrcReplyCode AsReplyCode()
        {
            if (TryGetReplyCode(out IrcReplyCode code))
                return code;

            return IrcReplyCode.None;
        }

        public bool TryGetReplyCode(out IrcReplyCode code)
        {
            code = IrcReplyCode.None;

            if (Command == null || Command.Length != 3)
                return false;

            if (int.TryParse(Command, out int i) && Enum.IsDefined(typeof(IrcReplyCode), i))
            {
                code = (IrcReplyCode)i;
                return true;
            }

            return false;
        }

        public string GetTrailer()
        {
            return Parameters[Parameters.Length - 1];
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            //sb.AppendFormat("{0} {1} ", Prefix, Command);
            sb.AppendFormat(string.Join(" ", Prefix, Command));

            for (int i = 0; i < ParameterCount; i++)
            {
                sb.AppendFormat(" {0}", Parameters[i]);
            }

            return sb.ToString();
        }
    }
}
