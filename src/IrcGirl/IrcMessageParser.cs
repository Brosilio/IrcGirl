using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl
{
    public unsafe class IrcMessageParser
    {
        private int _pos;
        private int _len;
        private int _argIdx;

        //private string tag;
        //private string prefix;
        //private string command;
        //private string[] args;

        public IrcMessage Parse(string raw)
        {
            //if (raw == null || raw.Length == 0)
              //  return null;

            //tag = null;
            //prefix = null;
            //command = null;
            //args = new string[15];
            IrcMessage msg = new IrcMessage();

            _pos = 0;
            _len = raw.Length;
            _argIdx = 0;

            fixed (char* _raw = raw)
            {
                //SkipSpaces(_raw);
                for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

                if (*(_raw + _pos) == '@')
                {
                    _pos++;
                    msg.Tag = ReadWord(_raw);
                }

                //SkipSpaces(_raw);
                for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

                if (*(_raw + _pos) == ':')
                {
                    _pos++;
                    msg.Prefix = ReadWord(_raw);
                }

                //SkipSpaces(_raw);
                for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

                msg.Command = ReadWord(_raw);

                while (_pos < _len)
                {
                    //SkipSpaces(_raw);
                    for (; _pos < _len && *(_raw + _pos) == ' '; _pos++) ;

                    if (*(_raw + _pos) == ':')
                    {
                        _pos++;
                        //args[_argIdx] = ReadToEnd(_raw);
                        msg.parameters[_argIdx] = new string(_raw, _pos, _len - _pos);
                        break;
                    }

                    msg.parameters[_argIdx] = ReadWord(_raw);
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
        private string ReadWord(char* _raw)
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

        private string ReadToEnd(char* _raw)
        {
            string result = new string(_raw, _pos, _len - _pos);
            _pos = _len;

            return result;
        }

        private void SkipSpaces(char* _raw)
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
