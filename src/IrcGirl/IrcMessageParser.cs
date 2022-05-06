using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl
{
	public static unsafe class IrcMessageParser
	{
		//private string tag;
		//private string prefix;
		//private string command;
		//private string[] args;

		public static IrcMessage Parse(string raw)
		{
			//if (raw == null || raw.Length == 0)
			//  return null;

			//tag = null;
			//prefix = null;
			//command = null;
			//args = new string[15];
			IrcMessage msg = new IrcMessage();

			int _pos = 0;
			int _argIdx = 0;
			int _len = raw.Length;

			fixed (char* _raw = raw)
			{
				//SkipSpaces(_raw);
				for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

				if (*(_raw + _pos) == '@')
				{
					_pos++;
					msg.Tag = ReadWord(_raw, ref _pos, ref _len);
				}

				//SkipSpaces(_raw);
				for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

				if (*(_raw + _pos) == ':')
				{
					_pos++;
					msg.Prefix = ReadWord(_raw, ref _pos, ref _len);
				}

				//SkipSpaces(_raw);
				for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

				msg.Command = ReadWord(_raw, ref _pos, ref _len);

				while (_pos < _len)
				{
					//SkipSpaces(_raw);
					for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

					if (*(_raw + _pos) == ':')
					{
						_pos++;
						//args[_argIdx] = ReadToEnd(_raw);
						msg.Parameters[_argIdx] = new string(_raw, _pos, _len - _pos);
						break;
					}

					msg.Parameters[_argIdx] = ReadWord(_raw, ref _pos, ref _len);
					_argIdx++;

					if (_argIdx >= 15)
						break;
				}

				//_argIdx++;

				//IrcMessage msg = new IrcMessage()
				//{
				//    Tag = tag,
				//    Prefix = prefix,
				//    Command = command,
				//    //parameters = new string[_argIdx]
				//    parameters = args
				//};

				//for (int i = 0; i < _argIdx + 1; i++)
				//  msg.parameters[i] = args[i];
				//Array.Copy(args, msg.parameters, _argIdx);

				return msg;
			}
		}

		[System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
		private static string ReadWord(char* _raw, ref int _pos, ref int _len)
		{
			int len = 0;
			for (; len + _pos < _len; len++)
			{
				if (*(_raw + _pos + len) == ' ') break;
			}

			string result = new string(_raw, _pos, len);

			_pos += len;

			return result;

		}

		private static string ReadToEnd(char* _raw, ref int _pos, ref int _len)
		{
			string result = new string(_raw, _pos, _len - _pos);
			_pos = _len;

			return result;
		}

		private static void SkipSpaces(char* _raw, ref int _pos, ref int _len)
		{
			for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

			//for (; _pos < _len; _pos++)
			// {
			//   if (*(_raw + _pos) != ' ')
			//       break;
			//}
		}
	}
}
