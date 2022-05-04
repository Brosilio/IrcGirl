using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace IrcGirl
{
    public class IrcMessage
    {
        public string prefix;
        public string command;
        public string[] parameters;

        public static IrcMessage Parse(string input)
        {
            if (input == null || input.Length == 0)
                return null;

            IrcMessage message = new IrcMessage();

            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == ':')

                    continue;


            }
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
