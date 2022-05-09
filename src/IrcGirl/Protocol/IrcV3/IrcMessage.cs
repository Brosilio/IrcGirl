using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IrcGirl.Protocol.IrcV3.IrcMessages;

namespace IrcGirl.Protocol.IrcV3
{
    public abstract class IrcMessage
    {
        private static readonly Dictionary<string, Type> _types;
        private static readonly Dictionary<Type, Func<RawIrcMessage, IrcMessage>> _typeInitializers;

        /// <summary>
        /// The raw IRC message this message was created from.
        /// </summary>
        public RawIrcMessage RawIrcMessage { get; private set; }

        static IrcMessage()
        {
            _types = new Dictionary<string, Type>();
            _typeInitializers = new Dictionary<Type, Func<RawIrcMessage, IrcMessage>>();

            RegisterMessageTypesFromAssembly(Assembly.GetExecutingAssembly());
        }

        public static implicit operator RawIrcMessage(IrcMessage message)
        {
            return message.RawIrcMessage;
        }

        public IrcMessage(RawIrcMessage raw)
        {
            this.RawIrcMessage = raw;
        }

        public override string ToString()
        {
            return RawIrcMessage.ToString();
        }

        /// <summary>
        /// Load types from the specified assembly that have been marked with the <see cref="IrcMessageAttribute"/>
        /// attribute.
        /// </summary>
        /// 
        /// <param name="source">The source assembly to load types from.</param>
        public static void RegisterMessageTypesFromAssembly(Assembly source)
        {
            // if we aren't loading defaults, we overwrite default handlers
            bool isDefault = source.Equals(Assembly.GetExecutingAssembly());

            foreach (Type target in source.ExportedTypes)
            {
                foreach (var attrib in target.GetCustomAttributes<IrcMessageAttribute>())
                {
                    if (isDefault && _types.ContainsKey(attrib.Command))
                        throw new InvalidOperationException($"Duplicate IrcMessageAttribute({attrib.Command}) defined in assembly");

                    _types[attrib.Command] = target;
                }
            }
        }

        /// <summary>
        /// Check if the loaded types could convert a RawIrcMessage with the specified command to
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
        /// Create and initialize an instance of an IrcMessage using the message types
        /// loaded by <see cref="RegisterMessageTypesFromAssembly(Assembly)"/>.
        /// </summary>
        /// 
        /// <param name="raw">The raw message input.</param>
        /// 
        /// <returns>
        /// An IrcMessage that may be cast to a more specific type of IrcMessage, or null if there
        /// is no type capable of handling the raw message.
        /// </returns>
        public static IrcMessage CreateInstance(RawIrcMessage raw)
        {
            if (!_types.ContainsKey(raw.Command))
                return null;

            Type tMsg = _types[raw.Command];

            // if we have a cached new() expression for the type, use it
            if (_typeInitializers.ContainsKey(tMsg))
                return _typeInitializers[tMsg](raw);

            // otherwise, create the initializer and cache it
            ConstructorInfo ctor = tMsg.GetConstructor(new[] { typeof(RawIrcMessage) });
            ParameterExpression paramExpr = Expression.Parameter(typeof(RawIrcMessage));
            NewExpression newExpr = Expression.New(ctor, paramExpr);

            var func = Expression.Lambda<Func<RawIrcMessage, IrcMessage>>(newExpr, paramExpr).Compile();
            _typeInitializers[tMsg] = func;

            return func(raw);
        }
    }
}
