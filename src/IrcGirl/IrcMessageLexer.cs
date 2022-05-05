using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl
{
	public class IrcMessageLexer
	{
		private string _raw;
		private int _pos;

		public IrcMessage Lex(string raw)
		{
			if (raw == null || raw.Length == 0)
				return null;

			_pos = 0;
			_raw = raw;

			IrcMessage message = new IrcMessage();

			List<string> parameters = new List<string>();
			int tokens = 0;

			for (; _pos < raw.Length; _pos++)
			{
				SkipSpaces();
				string word = ReadWord();

				if (tokens == 0 && word[0] == '@')
				{
					message.tag = word;
					tokens++;
					continue;
				}

				if (tokens <= 2 && message.prefix == null && word[0] == ':')
				{
					message.prefix = word;
					tokens++;
					continue;
				}

				if (message.command == null)
				{
					message.command = word;
					tokens++;
					continue;
				}

				if (word[0] == ':')
				{
					_pos -= word.Length;
					parameters.Add(ReadToEnd());
					tokens++;
				}
				else
				{
					parameters.Add(word);
					tokens++;
				}
			}

			message.parameters = parameters.ToArray();

			return message;
		}

		private string ReadWord()
		{
			int len = 0;
			for (int offset = 0; offset + _pos < _raw.Length; offset++)
			{
				if (_raw[_pos + offset] == ' ') break;
				len++;
			}

			string result = _raw.Substring(_pos, len);
			_pos += len;

			return result;
		}

		private string ReadToEnd()
		{
			string result = _raw.Substring(_pos);
			_pos = _raw.Length - 1;

			return result;
		}

		private void SkipSpaces()
		{
			for (; _pos < _raw.Length; _pos++)
			{
				if (_raw[_pos] != ' ')
					break;
			}
		}
	}
}
