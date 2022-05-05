using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	internal sealed class IrcMessageSinkAttribute : Attribute
	{
		public string[] Commands { get; private set; }

		public IrcMessageSinkAttribute(params string[] command)
		{
			this.Commands = command;
		}

		public IrcMessageSinkAttribute(params int[] replyCode)
		{
			Commands = new string[replyCode.Length];

			for(int i = 0; i < replyCode.Length; i++)
				Commands[i] = replyCode[i].ToString("D3");
		}
	}
}
