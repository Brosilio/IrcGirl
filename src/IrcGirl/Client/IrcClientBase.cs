﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using IrcGirl.Protocol.IrcV3;
using IrcGirl.Net;

//using SinkDictionary = System.Collections.Generic.Dictionary<string, IrcGirl.Client.IrcMessageSinkContainer>;
using SinkDictionary = System.Collections.Generic.Dictionary<string, System.Action<object>>;
using IrcGirl.Protocol.IrcV3.IrcMessages.Rpl;
using System.Linq.Expressions;

namespace IrcGirl.Client
{
    /// <summary>
    /// Represents the base class for all IrcClients.
    /// 
    /// Either inherit this class and override the event methods or use <see cref="EventedIrcClient"/>.
    /// </summary>
    public abstract class IrcClientBase : IDisposable
    {
        public IrcUser User { get; private set; }
        public IrcServerInfo Server { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsRegistered { get; private set; }
        public ISslStreamSource SslStreamSource { get; set; }

        private TcpClient _tcpClient;
        private IrcMessageStream _ircStream;
        private SinkDictionary _sinks;

        public IrcClientBase()
        {
            _sinks = new SinkDictionary();

            SslStreamSource = new DefaultSslStreamSource();

            RegisterSinksFromType(GetType());
        }

        public void RegisterSinksFromType(Type source)
        {
            var methods = source.BaseType.GetMethods(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            foreach (var method in methods)
            {
                var methodParams = method.GetParameters();
                if (methodParams == null || methodParams.Length != 1) continue;
                if (methodParams[0].ParameterType.BaseType != typeof(IrcMessage)) continue;

                var customAttribs = method.GetCustomAttributes(typeof(IrcMessageSinkAttribute), false);
                if (customAttribs == null || customAttribs.Length == 0) continue;

                foreach (var genericAttrib in customAttribs)
                {
                    if (genericAttrib is IrcMessageSinkAttribute sinkAttrib)
                    {
                        ParameterExpression paramExpr = Expression.Parameter(typeof(object));
                        UnaryExpression castExpr = Expression.Convert(paramExpr, methodParams[0].ParameterType);
                        ConstantExpression thisExpr = Expression.Constant(this);
                        MethodCallExpression callExpr = Expression.Call(thisExpr, method, castExpr);

                        Action<object> invoker = Expression.Lambda<Action<object>>(callExpr, paramExpr).Compile();

                        // overwrite...
                        _sinks[sinkAttrib.Command] = invoker;
                    }
                }
            }
        }

        /// <summary>
        /// The internal receive and event dispatch loop.
        /// </summary>
        private async Task MessageLoop()
        {
            while (true)
            {
                // wait for and read the next RawIrcMessage on the network.
                RawIrcMessage rawMessage = await _ircStream.ReadAsync();

                if (rawMessage == null)
                {
                    OnDisconnected(new Events.IrcDisconnectedEventArgs());
                    break;
                }

                try
                {
                    // raise the IrcMessageReceived event first, with the raw message
                    OnRawIrcMessageReceived(new Events.RawIrcMessageEventArgs(rawMessage));

                    // if we have an internal sink for the message, use it
                    if (_sinks.TryGetValue(rawMessage.Command, out var sink))
                    {
                        // convert the raw message to a fancy message of whatever type
                        IrcMessage message = IrcMessage.CreateInstance(rawMessage);

                        if (message == null)
                            throw new Exception("Message sink registered for type but the message wasn't creaeted");

                        sink(message);
                    }
                }
                catch (Exception ex)
                {
                    OnMessageLoopExceptionRaised(new Events.MessageLoopExceptionEventArgs(ex, rawMessage));
                }
            }
        }

        /// <summary>
        /// Queues a message to be sent by the underlying stream.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void Send(RawIrcMessage message)
        {
            _ircStream.QueueForSend(message);
        }

        /// <summary>
        /// Connect to an IRC server.
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        /// <param name="sslMode"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public async Task ConnectAsync(string host, int port, SslMode sslMode)
        {
            // dispose old stuff if any
            try
            {
                _tcpClient?.Dispose();
                _ircStream?.Dispose();
            }
            catch { }

            // new client and connect
            _tcpClient = new TcpClient();
            await _tcpClient.ConnectAsync(host, port).ConfigureAwait(false);

            // get new stream and auth as client if necessary
            Stream stream;
            switch (sslMode)
            {
                case SslMode.None:
                    stream = _tcpClient.GetStream();
                    break;

                case SslMode.UseSsl:
                    SslStream ssl = SslStreamSource.CreateStream(_tcpClient.GetStream());
                    await SslStreamSource.AuthenticateAsClientAsync(ssl, host).ConfigureAwait(false);
                    stream = ssl;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(sslMode), "Unsupported SslMode provided");
            }

            // start new irc stream and set flags
            _ircStream = new IrcMessageStream(stream);
            IsConnected = true;

            // raise OnConnected
            OnConnected(new Events.IrcConnectedEventArgs());

            // start message loop
            _ = Task.Factory.StartNew(
                MessageLoop,
                TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Close the connection to the server.
        /// 
        /// If you want to gracefully disconnect, use <see cref=""/>
        /// </summary>
        /// <returns></returns>
        public async Task DisconnectAsync()
        {

        }

        #region sinks

        /// <summary>
        /// Called when this client connects to a server.
        /// </summary>
        protected virtual void OnConnected(Events.IrcConnectedEventArgs e) { }

        /// <summary>
        /// Called when this client disconnects (network connection lost).
        /// </summary>
        protected virtual void OnDisconnected(Events.IrcDisconnectedEventArgs e) { }

        /// <summary>
        /// Called when any <see cref="RawIrcMessage"/> is received.
        /// </summary>
        protected virtual void OnRawIrcMessageReceived(Events.RawIrcMessageEventArgs e) { }

        /// <summary>
        /// Called when an exception occours inside the internal message loop.
        /// </summary>
        protected virtual void OnMessageLoopExceptionRaised(Events.MessageLoopExceptionEventArgs e) { }

        /// <summary>
        /// Called when an outbound <see cref="RawIrcMessage"/> is malformed.
        /// </summary>
        protected virtual void OnOutboundIrcProtocolViolation(Events.IrcProtocolViolationEventArgs e) { }

        /// <summary>
        /// Called when an inbound <see cref="RawIrcMessage"/> is malformed.
        /// </summary>
        protected virtual void OnInboundIrcProtocolViolation(Events.IrcProtocolViolationEventArgs e) { }

        /// <summary>
        /// Called when a received <see cref="RawIrcMessage"/> indicates an error.
        /// </summary>
        protected virtual void OnIrcErrorReplyReceived(Events.IrcErrorReplyEventArgs e) { }

        /// <summary>
        /// Called when an IRC message with reply code 001 is received.
        /// </summary>
        protected virtual void OnIrcWelcomeReceived(WelcomeIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.RPL_WELCOME)]
        private void OnIrcWelcomeReceivedInternal(WelcomeIrcMessage msg)
        {
            IsRegistered = true;
            OnIrcWelcomeReceived(msg);
        }

        [IrcMessageSink(IrcReplyCode.RPL_YOURHOST)] protected virtual void OnIrcYourHost(YourHostIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_CREATED)] protected virtual void OnIrcCreated(CreatedIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_MYINFO)] protected virtual void OnIrcMyInfo(MyInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ISUPPORT)] protected virtual void OnIrcISupport(ISupportIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_BOUNCE)] protected virtual void OnIrcBounce(BounceIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_UMODEIS)] protected virtual void OnIrcUModeIs(UModeIsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSERCLIENT)] protected virtual void OnIrcLUserClient(LUserClientIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSEROP)] protected virtual void OnIrcLUserOp(LUserOpIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSERUNKNOWN)] protected virtual void OnIrcLUserUnknown(LUserUnknownIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSERCHANNELS)] protected virtual void OnIrcLUserChannels(LUserChannelsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSERME)] protected virtual void OnIrcLUserMe(LUserMeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINME)] protected virtual void OnIrcAdminMe(AdminMeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINLOC1)] protected virtual void OnAdminLoc1(AdminInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINLOC2)] protected virtual void OnAdminLoc2(AdminInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINEMAIL)] protected virtual void OnAdminEmail(AdminInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_TRYAGAIN)] protected virtual void OnTryAgain(TryAgainIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LOCALUSERS)] protected virtual void OnLocalUsers(LocalUsersIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_GLOBALUSERS)] protected virtual void OnGlobalUsers(GlobalUsersIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISCERTFP)] protected virtual void OnWhoIsCertFp(WhoIsCertFpIrcMessage msg) { }
        //[IrcMessageSink(IrcReplyCode.RPL_NONE)] protected virtual void OnNone(RawIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_AWAY)] protected virtual void OnAway(AwayIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_USERHOST)] protected virtual void OnUserHost(UserHostIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_UNAWAY)] protected virtual void OnUnaway(UnawayIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_NOWAWAY)] protected virtual void OnNowAway(NowAwayIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOREPLY)] protected virtual void OnWhoReply(WhoReplyIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFWHO)] protected virtual void OnEndOfWho(EndOfWhoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISREGNICK)] protected virtual void OnWhoIsRegNick(WhoIsRegNickIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISUSER)] protected virtual void OnWhoIsUser(WhoIsUserIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISSERVER)] protected virtual void OnWhoIsServer(WhoIsServerIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISOPERATOR)] protected virtual void OnWhoIsOperator(WhoIsOperatorIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOWASUSER)] protected virtual void OnWhoWasUser(WhoWasUserIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISIDLE)] protected virtual void OnWhoIsIdle(WhoIsIdleIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFWHOIS)] protected virtual void OnEndOfWhoIs(EndOfWhoIsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISCHANNELS)] protected virtual void OnWhoIsChannels(WhoIsChannelsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISSPECIAL)] protected virtual void OnWhoIsSpecial(WhoIsSpecialIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LISTSTART)] protected virtual void OnListStart(ListStartIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LIST)] protected virtual void OnList(ListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LISTEND)] protected virtual void OnListEnd(ListEndIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_CHANNELMODEIS)] protected virtual void OnChannelModeIs(ChannelModeIsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_CREATIONTIME)] protected virtual void OnCreationTime(CreationTimeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISACCOUNT)] protected virtual void OnWhoIsAccount(WhoIsAccountIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_NOTOPIC)] protected virtual void OnNoTopic(TopicIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_TOPIC)] protected virtual void OnTopic(TopicIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_TOPICWHOTIME)] protected virtual void OnTopicWhoTime(TopicWhoTimeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_INVITELIST)] protected virtual void OnInviteList(InviteListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFINVITELIST)] protected virtual void OnEndOfInviteList(EndOfInviteListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISACTUALLY)] protected virtual void OnWhoIsActually(WhoIsActuallyIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_INVITING)] protected virtual void OnInviting(InvitingIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_INVEXLIST)] protected virtual void OnInvExList(InvExListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFINVEXLIST)] protected virtual void OnEndOfInvExList(EndOfInvExListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_EXCEPTLIST)] protected virtual void OnExceptList(ExceptListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFEXCEPTLIST)] protected virtual void OnEndOfExceptList(EndOfExceptListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_VERSION)] protected virtual void OnVersion(VersionIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_NAMREPLY)] protected virtual void OnNamReply(NamReplyIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFNAMES)] protected virtual void OnEndOfNames(EndOfNamesIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LINKS)] protected virtual void OnLinks(LinksIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFLINKS)] protected virtual void OnEndOfLinks(EndOfLinksIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_BANLIST)] protected virtual void OnBanList(BanListIrcMessage msg) { }

        #endregion sinks

        #region IDisposable

        private bool disposedValue;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~IrcClientBase()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        private void CheckDisposed()
        {
            if (disposedValue)
                throw new ObjectDisposedException(GetType().FullName);
        }

        #endregion IDisposable
    }
}
