using System;
using System.Collections.Generic;
using System.Text;

using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Client
{
	[AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = true)]
	internal sealed class CtcpMessageSinkAttribute : Attribute
	{
		/// <summary>
		/// The command.
		/// </summary>
		public string Command { get; private set; }

		public CtcpMessageSinkAttribute(string command)
		{
			Command = command;
		}
	}
}
