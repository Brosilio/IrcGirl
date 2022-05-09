using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Protocol.IrcV3
{
	public unsafe class IrcMessageParser : IIrcMessageParser
	{
		public static IrcMessageParser Default { get; } = new IrcMessageParser();

		internal const int MAX_PARAMS = 15;

		internal const sbyte UTF8_SPACE = (sbyte)' ';
		internal const sbyte UTF8_COLON = (sbyte)':';
		internal const sbyte UTF8_AT = (sbyte)'@';

		public IrcMessage Parse(string raw)
		{
			//if (raw == null || raw.Length == 0)
			  //return null;

			IrcMessage msg = new IrcMessage();

			int _pos = 0;
			int _argIdx = 0;
			int _len = raw.Length;
			//int _len = len;

			fixed (char* _raw = raw)
			//fixed(sbyte* _raw = d)
			{
				//SkipSpaces(_raw);
				for (; _pos < _len && *(_raw + _pos) == UTF8_SPACE; _pos++) ;

				if (*(_raw + _pos) == UTF8_AT)
				{
					_pos++;
					msg.Tag = ReadWord(_raw, ref _pos, ref _len);
				}

				//SkipSpaces(_raw);
				for (; _pos < _len && *(_raw + _pos) == UTF8_SPACE; _pos++) ;

				if (*(_raw + _pos) == UTF8_COLON)
				{
					_pos++;
					msg.Prefix = ReadWord(_raw, ref _pos, ref _len);
				}

				//SkipSpaces(_raw);
				for (; _pos < _len && *(_raw + _pos) == UTF8_SPACE; _pos++) ;

				msg.Command = ReadWord(_raw, ref _pos, ref _len);

				while (_pos < _len)
				{
					//SkipSpaces(_raw);
					for (; _pos < _len && *(_raw + _pos) == UTF8_SPACE; _pos++) ;

					if (*(_raw + _pos) == UTF8_COLON)
					{
						_pos++;
						//args[_argIdx] = ReadToEnd(_raw);
						msg.Parameters[_argIdx] = new string(_raw, _pos, _len - _pos);
						break;
					}

					msg.Parameters[_argIdx] = ReadWord(_raw, ref _pos, ref _len);
					_argIdx++;

					if (_argIdx >= MAX_PARAMS)
						break;
				}

				msg.ParameterCount = _argIdx + 1;

				return msg;
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string ReadWord(char* _raw, ref int _pos, ref int _len)
		//private static string ReadWord(sbyte* _raw, ref int _pos, ref int _len)
		{
			int len = 0;
			for (; len + _pos < _len; len++)
			{
				if (*(_raw + _pos + len) == UTF8_SPACE) break;
			}

			string result = new string(_raw, _pos, len);

			_pos += len;

			return result;

		}
	}
}
