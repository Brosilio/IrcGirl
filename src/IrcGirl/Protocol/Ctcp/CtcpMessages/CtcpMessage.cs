using IrcGirl.Protocol.IrcV3;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace IrcGirl.Protocol.Ctcp.CtcpMessages
{
    public abstract class CtcpMessage
    {
        private static readonly Dictionary<string, Type> _types;
        private static readonly Dictionary<Type, Func<RawCtcpMessage, CtcpMessage>> _typeInitializers;

        /// <summary>
        /// The raw IRC message this message was created from, if any.
        /// 
        /// For new messages, this will be a RawCtcpMessage representing the content of this CtcpMessage.
        /// </summary>
        public RawCtcpMessage RawCtcpMessage { get; protected set; }

        /// <summary>
        /// The source of this CTCP message, if any (the raw IRC message's Prefix property)
        /// </summary>
        public string Source
        {
            get
            {
                return RawCtcpMessage.RawIrcMessage.Prefix;
            }
        }

        static CtcpMessage()
        {
            _types = new Dictionary<string, Type>();
            _typeInitializers = new Dictionary<Type, Func<RawCtcpMessage, CtcpMessage>>();

            RegisterMessageTypesFromAssembly(Assembly.GetExecutingAssembly());
        }

        public static implicit operator RawCtcpMessage(CtcpMessage message)
        {
            return message.RawCtcpMessage;
        }

        /// <summary>
        /// Create a new instance of <see cref="IrcMessage"/> from a <see cref="RawIrcMessage"/>.
        /// Use <see cref="CreateInstance(RawIrcMessage)"/> to create a strongly-typed IRC message
        /// instance and also validate it.
        /// </summary>
        /// 
        /// <param name="raw">The raw IRC message.</param>
        public CtcpMessage(RawCtcpMessage raw)
        {
            this.RawCtcpMessage = raw;
        }

        public CtcpMessage()
        {

        }

        public override string ToString()
        {
            return RawCtcpMessage.ToString();
        }

        /// <summary>
        /// Load types from the specified assembly that have been marked with the
        /// <see cref="CtcpMessageAttribute"/> attribute.
        /// </summary>
        /// 
        /// <param name="source">The source assembly to load types from.</param>
        public static void RegisterMessageTypesFromAssembly(Assembly source)
        {
            // if we aren't loading defaults, we overwrite default handlers
            bool isDefault = source.Equals(Assembly.GetExecutingAssembly());

            foreach (Type target in source.ExportedTypes)
            {
                foreach (var attrib in target.GetCustomAttributes<CtcpMessageAttribute>())
                {
                    if (isDefault && _types.ContainsKey(attrib.Command))
                        throw new InvalidOperationException($"Duplicate CtcpMessageAttribute({attrib.Command}) defined in assembly");

                    _types[attrib.Command] = target;
                }
            }
        }

        /// <summary>
        /// Check if the loaded types could convert a RawCtcpMessage with the specified command to
        /// a fancier wrapper type.
        /// </summary>
        /// 
        /// <param name="command">The command</param>
        /// 
        /// <returns>
        /// <see langword="true"/> if the command is known, <see langword="false"/> if it is not.
        /// </returns>
        public static bool CanFancifyMessageType(string command)
        {
            return _types.ContainsKey(command);
        }

        /// <summary>
        /// Create and initialize an instance of a CtcpMessage using the message types
        /// loaded by <see cref="RegisterMessageTypesFromAssembly(Assembly)"/>.
        /// </summary>
        /// 
        /// <param name="raw">The raw message input.</param>
        /// 
        /// <returns>
        /// A CtcpMessage that may be cast to a more specific type of CtcpMessage, or null if there
        /// is no type capable of handling the raw message.
        /// </returns>
        public static CtcpMessage CreateInstance(RawCtcpMessage raw)
        {
            if (!CanFancifyMessageType(raw.Command))
                return null;

            Type tMsg = _types[raw.Command];

            // if we have a cached new() expression for the type, use it
            if (_typeInitializers.ContainsKey(tMsg))
                return _typeInitializers[tMsg](raw);

            // otherwise, create the initializer and cache it
            ConstructorInfo ctor = tMsg.GetConstructor(new[] { typeof(RawCtcpMessage) });

            if (ctor == null)
                throw new Exception("Target type does not have valid constructor");

            ParameterExpression paramExpr = Expression.Parameter(typeof(RawCtcpMessage));
            NewExpression newExpr = Expression.New(ctor, paramExpr);

            var func = Expression.Lambda<Func<RawCtcpMessage, CtcpMessage>>(newExpr, paramExpr).Compile();
            _typeInitializers[tMsg] = func;

            return func(raw);
        }
    }
}
