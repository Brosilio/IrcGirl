using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Client
{
	public class IrcServerInfo
	{
		/// <summary>
		/// The content of the welcome message sent by the server.
		/// </summary>
		public string Welcome { get; internal set; }

		/// <summary>
		/// The content of the YourHost message sent by the server.
		/// </summary>
		public string YourHost { get; internal set; }

		/// <summary>
		/// The content of the Created message sent by the server.
		/// </summary>
		public string Created { get; internal set; }

		/// <summary>
		/// The content of the MyInfo message sent by the server.
		/// </summary>
		public string MyInfo { get; internal set; }
	}
}
