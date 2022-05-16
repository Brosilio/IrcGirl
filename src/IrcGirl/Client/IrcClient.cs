using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using IrcGirl.Net;
using IrcGirl.Protocol.IrcV3;
using IrcGirl.Protocol.IrcV3.IrcMessages;
using IrcGirl.Protocol.IrcV3.IrcMessages.Commands;
using IrcGirl.Protocol.Ctcp;
using IrcGirl.Protocol.Ctcp.CtcpMessages;

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
    public abstract class IrcClient : IDisposable
    {
        public IrcUser Me { get; private set; }
        public IrcServerInfo Server { get; private set; }
        public bool IsConnected { get; private set; }
        public bool IsRegistered { get; private set; }
        public ISslStreamSource SslStreamSource { get; set; }

        private TcpClient _tcpClient;
        private IrcMessageStream _ircStream;
        private SinkDictionary _sinks;

        public IrcClient()
        {
            _sinks = new SinkDictionary();

            SslStreamSource = new DefaultSslStreamSource();
            Me = new IrcUser(this);
            Server = new IrcServerInfo();

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
                    OnRawIrcMessageReceived(rawMessage);

                    // if it's an error reply code, call the error reply code function
                    if (rawMessage.TryGetReplyCode(out IrcReplyCode code) && rawMessage.IsErrorReply())
                        OnIrcErrorReplyReceived(code, rawMessage);

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
        protected virtual void OnRawIrcMessageReceived(RawIrcMessage message) { }

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
        /// Called when a received <see cref="RawIrcMessage"/> indicates some error.
        /// </summary>
        protected virtual void OnIrcErrorReplyReceived(IrcReplyCode code, RawIrcMessage message) { }

        /// <summary>
        /// Called when an IRC message with reply code 001 is received. Indicates successful registration.
        /// </summary>
        protected virtual void OnIrcWelcome(WelcomeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WELCOME)]
        private void OnIrcWelcomeInternal(WelcomeIrcMessage msg)
        {
            // mark as registered and set server.welcomemessage
            IsRegistered = true;
            Server.Welcome = msg.WelcomeMessage;

            OnIrcWelcome(msg);
        }

        /// <summary>
        /// Called when an IRC message with reply code <see cref="IrcReplyCode.RPL_YOURHOST"/> is received.
        /// </summary>
        /// 
        /// <param name="msg">The message received from the server.</param>
        protected virtual void OnIrcYourHost(YourHostIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_YOURHOST)]
        private void OnIrcYourHostInternal(YourHostIrcMessage msg)
        {
            // internally set the server.yourhost property
            Server.YourHost = msg.Message;

            OnIrcYourHost(msg);
        }

        /// <summary>
        /// Called when an IRC message with reply code <see cref="IrcReplyCode.RPL_CREATED"/> is received.
        /// </summary>
        /// 
        /// <param name="msg">The messaged received from the server.</param>
        protected virtual void OnIrcCreated(CreatedIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_CREATED)]
        private void OnIrcCreatedInternal(CreatedIrcMessage msg)
        {
            // internally set server.created property
            Server.Created = msg.Message;
        }

        /// <summary>
        /// Called wen an IRC message with reply code <see cref="IrcReplyCode.RPL_MYINFO"/> is received.
        /// </summary>
        /// 
        /// <param name="msg">The message received from the server.</param>
        protected virtual void OnIrcMyInfo(MyInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_MYINFO)]
        private void OnIrcMyInfoInternal(MyInfoIrcMessage msg)
        {
            Server.MyInfo = msg.Message;
        }

        [IrcMessageSink(IrcReplyCode.RPL_ISUPPORT)] protected virtual void OnIrcISupport(ISupportIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_BOUNCE)] protected virtual void OnIrcBounce(BounceIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_UMODEIS)] protected virtual void OnIrcUModeIs(UModeIsIrcMessage msg) { }
        /*[IrcMessageSink(IrcReplyCode.RPL_LUSERCLIENT)] protected virtual void OnIrcLUserClient(LUserClientIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSEROP)] protected virtual void OnIrcLUserOp(LUserOpIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSERUNKNOWN)] protected virtual void OnIrcLUserUnknown(LUserUnknownIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSERCHANNELS)] protected virtual void OnIrcLUserChannels(LUserChannelsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LUSERME)] protected virtual void OnIrcLUserMe(LUserMeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINME)] protected virtual void OnIrcAdminMe(AdminMeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINLOC1)] protected virtual void OnIrcAdminLoc1(AdminInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINLOC2)] protected virtual void OnIrcAdminLoc2(AdminInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ADMINEMAIL)] protected virtual void OnIrcAdminEmail(AdminInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_TRYAGAIN)] protected virtual void OnIrcTryAgain(TryAgainIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LOCALUSERS)] protected virtual void OnIrcLocalUsers(LocalUsersIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_GLOBALUSERS)] protected virtual void OnIrcGlobalUsers(GlobalUsersIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISCERTFP)] protected virtual void OnIrcWhoIsCertFp(WhoIsCertFpIrcMessage msg) { }
        //[IrcMessageSink(IrcReplyCode.RPL_NONE)] protected virtual void OnIrcNone(RawIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_AWAY)] protected virtual void OnIrcAway(AwayIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_USERHOST)] protected virtual void OnIrcUserHost(UserHostIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_UNAWAY)] protected virtual void OnIrcUnaway(UnawayIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_NOWAWAY)] protected virtual void OnIrcNowAway(NowAwayIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOREPLY)] protected virtual void OnIrcWhoReply(WhoReplyIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFWHO)] protected virtual void OnIrcEndOfWho(EndOfWhoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISREGNICK)] protected virtual void OnIrcWhoIsRegNick(WhoIsRegNickIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISUSER)] protected virtual void OnIrcWhoIsUser(WhoIsUserIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISSERVER)] protected virtual void OnIrcWhoIsServer(WhoIsServerIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISOPERATOR)] protected virtual void OnIrcWhoIsOperator(WhoIsOperatorIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOWASUSER)] protected virtual void OnIrcWhoWasUser(WhoWasUserIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISIDLE)] protected virtual void OnIrcWhoIsIdle(WhoIsIdleIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFWHOIS)] protected virtual void OnIrcEndOfWhoIs(EndOfWhoIsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISCHANNELS)] protected virtual void OnIrcWhoIsChannels(WhoIsChannelsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISSPECIAL)] protected virtual void OnIrcWhoIsSpecial(WhoIsSpecialIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LISTSTART)] protected virtual void OnIrcListStart(ListStartIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LIST)] protected virtual void OnIrcList(ListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LISTEND)] protected virtual void OnIrcListEnd(ListEndIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_CHANNELMODEIS)] protected virtual void OnIrcChannelModeIs(ChannelModeIsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_CREATIONTIME)] protected virtual void OnIrcCreationTime(CreationTimeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISACCOUNT)] protected virtual void OnIrcWhoIsAccount(WhoIsAccountIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_NOTOPIC)] protected virtual void OnIrcNoTopic(TopicIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_TOPIC)] protected virtual void OnIrcTopic(TopicIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_TOPICWHOTIME)] protected virtual void OnIrcTopicWhoTime(TopicWhoTimeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_INVITELIST)] protected virtual void OnIrcInviteList(InviteListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFINVITELIST)] protected virtual void OnIrcEndOfInviteList(EndOfInviteListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISACTUALLY)] protected virtual void OnIrcWhoIsActually(WhoIsActuallyIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_INVITING)] protected virtual void OnIrcInviting(InvitingIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_INVEXLIST)] protected virtual void OnIrcInvExList(InvExListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFINVEXLIST)] protected virtual void OnIrcEndOfInvExList(EndOfInvExListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_EXCEPTLIST)] protected virtual void OnIrcExceptList(ExceptListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFEXCEPTLIST)] protected virtual void OnIrcEndOfExceptList(EndOfExceptListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_VERSION)] protected virtual void OnIrcVersion(VersionIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_NAMREPLY)] protected virtual void OnIrcNamReply(NamReplyIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFNAMES)] protected virtual void OnIrcEndOfNames(EndOfNamesIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LINKS)] protected virtual void OnIrcLinks(LinksIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFLINKS)] protected virtual void OnIrcEndOfLinks(EndOfLinksIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_BANLIST)] protected virtual void OnIrcBanList(BanListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFBANLIST)] protected virtual void OnIrcEndOfBanList(EndOfBanListIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFWHOWAS)] protected virtual void OnIrcEndOfWhoWas(EndOfWhoWasIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_INFO)] protected virtual void OnIrcInfo(InfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFINFO)] protected virtual void OnIrcEndOfInfo(EndOfInfoIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_MOTDSTART)] protected virtual void OnIrcMotdStart(MotdStartIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_MOTD)] protected virtual void OnIrcMotd(MotdIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFMOTD)] protected virtual void OnIrcEndOfMotd(EndOfMotdIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISHOST)] protected virtual void OnIrcWhoIsHost(WhoIsHostIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISMODES)] protected virtual void OnIrcWhoIsModes(WhoIsModesIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_YOUREOPER)] protected virtual void OnIrcYoureOper(YoureOperIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_REHASHING)] protected virtual void OnIrcRehashing(RehashingIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_TIME)] protected virtual void OnIrcTime(TimeIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.ERR_UNKNOWNERROR)] protected virtual void OnIrcUnknownError(UnknownErrorIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOSUCHNICK)] protected virtual void OnIrcNoSuchNick(NoSuchNickIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOSUCHSERVER)] protected virtual void OnIrcNosuchServer(NoSuchServerIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOSUCHCHANNEL)] protected virtual void OnIrcNoSuchChannel(NoSuchChannelIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_CANNOTSENDTOCHAN)] protected virtual void OnIrcCannotSendToChan(CannotSendToChannelIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_TOOMANYCHANNELS)] protected virtual void OnIrcTooManyChannels(TooManyChannelsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_WASNOSUCHNICK)] protected virtual void OnIrcWasNoSuchNick(WasNoSuchNickIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOORIGIN)] protected virtual void OnIrcNoOrigin(NoOriginIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_INPUTTOOLONG)] protected virtual void OnIrcInputTooLong(InputTooLongIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_UNKNOWNCOMMAND)] protected virtual void OnIrcUnknownCommand(UnknownCommandIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOMOTD)] protected virtual void OnIrcNoMotd(NoMotdIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_ERRONEUSNICKNAME)] protected virtual void OnIrcErroneusNickname(ErroneusNicknameIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NICKNAMEINUSE)] protected virtual void OnIrcNicknameInUse(NicknameInUseIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_USERNOTINCHANNEL)] protected virtual void OnIrcUserNotInChannel(UserNotInChannelIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOTONCHANNEL)] protected virtual void OnIrcNotOnChannel(NotOnChannelIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_USERONCHANNEL)] protected virtual void OnIrcUserOnChannel(UserOnChannelIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOTREGISTERED)] protected virtual void OnIrcNotRegistered(NotRegisteredIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NEEDMOREPARAMS)] protected virtual void OnIrcNeedMoreParams(NeedMoreParamsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_ALREADYREGISTERED)] protected virtual void OnIrcAlreadyRegistered(AlreadyRegisteredIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_PASSWDMISMATCH)] protected virtual void OnIrcPasswdMismatch(PasswdMismatchIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_YOUREBANNEDCREEP)] protected virtual void OnIrcYoureBannedCreep(YoureBannedCreepIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_CHANNELISFULL)] protected virtual void OnIrcChannelIsFull(ChannelIsFullIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_UNKNOWNMODE)] protected virtual void OnIrcUnknownMode(UnknownModeIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_INVITEONLYCHAN)] protected virtual void OnIrcInviteOnlyChan(InviteOnlyChanIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_BANNEDFROMCHAN)] protected virtual void OnIrcBannedFromChan(BannedFromChanIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_BADCHANNELKEY)] protected virtual void OnIrcBadChannelKey(BadChannelKeyIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_BADCHANMASK)] protected virtual void OnIrcBadChanMask(BadChanMaskIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOPRIVILEGES)] protected virtual void OnIrcNoPrivileges(NoPrivilegesIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_CHANOPRIVSNEEDED)] protected virtual void OnIrcChanOPrivsNeeded(ChanOPrivsNeededIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_CANTKILLSERVER)] protected virtual void OnIrcCantKillServer(CantKillServerIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOOPERHOST)] protected virtual void OnIrcNoOperHost(NoOperHostIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_UMODEUNKNOWNFLAG)] protected virtual void OnIrcUModeUnknownFlag(UModeUnknownFlagIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_USERSDONTMATCH)] protected virtual void OnIrcUsersDontMatch(UsersDontMatchIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_HELPNOTFOUND)] protected virtual void OnIrcHelpNotFound(HelpNotFoundIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_INVALIDKEY)] protected virtual void OnIrcInvalidKey(InvalidKeyIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.RPL_STARTTLS)] protected virtual void OnIrcStartTls(StartTlsIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_WHOISSECURE)] protected virtual void OnIrcWhoIsSecure(WhoIsSecureIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.ERR_STARTTLS)] protected virtual void OnIrcStartTlsErr(StartTlsErrIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_INVALIDMODEPARAM)] protected virtual void OnIrcInvalidModeParam(InvalidModeParamIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.RPL_HELPSTART)] protected virtual void OnIrcHelpStart(HelpStartIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_HELPTXT)] protected virtual void OnIrcHelpTxt(HelpTxtIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_ENDOFHELP)] protected virtual void OnIrcEndOfHelp(EndOfHelpIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_NOPRIVS)] protected virtual void OnIrcNoPrivs(NoPrivsIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.RPL_LOGGEDIN)] protected virtual void OnIrcLoggedIn(LoggedInIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.RPL_LOGGEDOUT)] protected virtual void OnIrcLoggedOut(LoggedOutIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.ERR_NICKLOCKED)] protected virtual void OnIrcNickLocked(NickLockedIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.RPL_SASLSUCCESS)] protected virtual void OnIrcSaslSuccess(SaslSuccessIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.ERR_SASLFAIL)] protected virtual void OnIrcSaslFail(SaslFailIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_SASLTOOLONG)] protected virtual void OnIrcSaslTooLong(SaslTooLongIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_SASLABORTED)] protected virtual void OnIrcSaslAborted(SaslAbortedIrcMessage msg) { }
        [IrcMessageSink(IrcReplyCode.ERR_SASLALREADY)] protected virtual void OnIrcSaslAlready(SaslAlreadyIrcMessage msg) { }

        [IrcMessageSink(IrcReplyCode.RPL_SASLMECHS)] protected virtual void OnIrcSaslMechs(SaslMechsIrcMessage msg) { }
        */

        /// <summary>
        /// Called when a PING message is received. The base implementation simply responds using <see cref="IrcClientIrcMethods.IrcPong(IrcClient, string)"/>.
        /// </summary>
        /// 
        /// <param name="msg">The ping message.</param>
        [IrcMessageSink(IrcCommands.PING)]
        protected virtual void OnIrcPing(PingIrcMessage msg)
        {
            this.IrcPong(msg.Token);
        }

        /// <summary>
        /// Called when a PrivMsg message is received.
        /// </summary>
        /// 
        /// <param name="from">The user the message is from.</param>
        /// <param name="msg">The message.</param>
        protected virtual void OnIrcPrivMsg(IrcUser from, PrivMsgIrcMessage msg){ }
        [IrcMessageSink(IrcCommands.PRIVMSG)]
        private void OnIrcPrivMsgInternal(PrivMsgIrcMessage msg)
        {
            OnIrcPrivMsg(new IrcUser(this, msg.Source), msg);
        }

        #endregion sinks

        #region ctcp

        /// <summary>
        /// Called every time any CTCP message is received.
        /// </summary>
        /// 
        /// <param name="msg">The received CTCP message.</param>
        protected virtual void OnRawCtcpMessage(RawCtcpMessage msg) { }

        /// <summary>
        /// Called when a CTCP ping is received. The base implementation responds with a CTCP pong.
        /// </summary>
        /// 
        /// <param name="msg">The CTCP PING message.</param>
        [CtcpMessageSink(CtcpCommands.PING)]
        protected virtual void OnCtcpPing(PingCtcpMessage msg)
        {
            this.Send(new RawCtcpMessage().AsReply());
        }

        #endregion ctcp

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
