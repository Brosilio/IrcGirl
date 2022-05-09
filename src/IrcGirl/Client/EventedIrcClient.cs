using System;
using System.Collections.Generic;
using System.Text;
using IrcGirl.Events;
using IrcGirl.Protocol.IrcV3;

namespace IrcGirl.Client
{
	public class EventedIrcClient : IrcClientBase
	{
		/// <summary>
		/// Raised when the client connects to a host.
		/// </summary>
		public event EventHandler<IrcConnectedEventArgs> Connected;
		protected override void OnConnected(IrcConnectedEventArgs e)
		{
			base.OnConnected(e);
			Connected?.Invoke(this, e);
		}

		/// <summary>
		/// Raised when the connection to the host is lost.
		/// </summary>
		public event EventHandler<IrcDisconnectedEventArgs> Disconnected;
		protected override void OnDisconnected(IrcDisconnectedEventArgs e)
		{
			base.OnDisconnected(e);
			Disconnected?.Invoke(this, e);
		}

		/// <summary>
		/// Raised when an exception occours in the internal message loop.
		/// </summary>
		public event EventHandler<MessageLoopExceptionEventArgs> MessageLoopExceptionRaised;
		protected override void OnMessageLoopExceptionRaised(MessageLoopExceptionEventArgs e)
		{
			base.OnMessageLoopExceptionRaised(e);
			MessageLoopExceptionRaised?.Invoke(this, e);
		}

		/// <summary>
		/// Raised on every received <see cref="RawIrcMessage"/>.
		/// </summary>
		public event EventHandler<RawIrcMessageEventArgs> RawMessageReceived;
		protected override void OnRawIrcMessageReceived(RawIrcMessageEventArgs e)
		{
			base.OnRawIrcMessageReceived(e);
			RawMessageReceived?.Invoke(this, e);
		}

		/// <summary>
		/// Raised when an <see cref="RawIrcMessage"/> is received and contains an <see cref="IrcReplyCode"/> that indicates
		/// an error.
		/// </summary>
		public event EventHandler<IrcErrorReplyEventArgs> IrcErrorReceived;
		protected override void OnIrcErrorReplyReceived(IrcErrorReplyEventArgs e)
		{
			base.OnIrcErrorReplyReceived(e);
			IrcErrorReceived?.Invoke(this, e);
		}

		/// <summary>
		/// Raised when an inbound <see cref="RawIrcMessage"/> is malformed.
		/// </summary>
		public event EventHandler<IrcProtocolViolationEventArgs> InboundIrcProtocolViolation;
		protected override void OnInboundIrcProtocolViolation(IrcProtocolViolationEventArgs e)
		{
			base.OnInboundIrcProtocolViolation(e);
			InboundIrcProtocolViolation?.Invoke(this, e);
		}

		/// <summary>
		/// Raised when an outbound <see cref="RawIrcMessage"/> is malformed.
		/// </summary>
		public event EventHandler<IrcProtocolViolationEventArgs> OutboundIrcProtocolViolation;
		protected override void OnOutboundIrcProtocolViolation(IrcProtocolViolationEventArgs e)
		{
			base.OnOutboundIrcProtocolViolation(e);
			OutboundIrcProtocolViolation?.Invoke(this, e);
		}
	}
}
