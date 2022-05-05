using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
	public class IrcServerInfo
	{
		public string Welcome { get; internal set; }
		public string YourHost { get; internal set; }
		public string Created { get; internal set; }
		public string MyInfo { get; internal set; }
	}
}
