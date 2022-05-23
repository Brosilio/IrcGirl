using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3
{
    public static class IrcCommand
    {
        public const string
            CAP = "CAP",
            AUTHENTICATE = "AUTHENTICATE",
            PASS = "PASS",
            NICK = "NICK",
            USER = "USER",
            PING = "PING",
            PONG = "PONG",
            OPER = "OPER",
            QUIT = "QUIT",
            ERROR = "ERROR",

            JOIN = "JOIN",
            PART = "PART",
            TOPIC = "TOPIC",
            NAMES = "NAMES",
            LIST = "LIST",
            INVITE = "INVITE",
            KICK = "KICK",

            MOTD = "MOTD",
            VERSION = "VERSION",
            ADMIN = "ADMIN",
            CONNECT = "CONNECT",
            LUSERS = "LUSERS",
            TIME = "TIME",
            STATS = "STATS",
            HELP = "HELP",
            INFO = "INFO",
            MODE = "MODE",

            PRIVMSG = "PRIVMSG",
            NOTICE = "NOTICE",

            WHO = "WHO",
            WHOIS = "WHOIS",
            WHOWAS = "WHOWAS",

            KILL = "KILL",
            REHASH = "REHASH",
            RESTART = "RESTART",
            SQUIT = "SQUIT",

            AWAY = "AWAY",
            LINKS = "LINKS",
            USERHOST = "USERHOST",
            WALLOPS = "WALLOPS";
    }
}
