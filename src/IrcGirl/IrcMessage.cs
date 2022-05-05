using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl
{
    public class IrcMessage
    {
        public string tag;
        public string prefix;
        public string command;
        public string[] parameters;

        public IrcMessage()
		{

		}

        public IrcMessage(string command, params string[] parameters)
		{
            this.command = command;
            this.parameters = parameters;
		}

        public bool IsReplyCode()
		{
            return command != null && command.Length == 3 && int.TryParse(command, out _);
		}

        public string GetTrailer()
		{
            return parameters[parameters.Length - 1];
		}

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0} {1} ", prefix, command);
            
            for (int i = 0; i < parameters.Length; i++)
            {
                sb.AppendFormat("{0} ", parameters[i]);
            }

            return sb.ToString();
        }
    }
}
