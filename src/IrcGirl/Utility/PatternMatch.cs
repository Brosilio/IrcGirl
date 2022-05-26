using System;
using System.Collections.Generic;
using System.Text;

namespace IrcGirl.Utility
{
    /// <summary>
    /// A utility class for basic glob style (some*thing?) style pattern matching.
    /// </summary>
    public unsafe class PatternMatch
    {
        /// <summary>
        /// Determine if the specified input matches the specified pattern.
        /// </summary>
        /// 
        /// <param name="pattern">The pattern.</param>
        /// <param name="input">The input</param>
        /// 
        /// <returns>
        /// <see langword="true"/> if the input matches the pattern, <see langword="false"/> otherwise.
        /// </returns>
        public static bool IsMatch(string pattern, string input)
        {
            int px = 0;
            int ix = 0;
            int nextPx = 0;
            int nextIx = 0;

            while (px < pattern.Length || ix < input.Length)
            {
                if (px < pattern.Length)
                {
                    char c = pattern[px];
                    switch (c)
                    {
                        default:
                            if (ix < input.Length && input[ix] == c)
                            {
                                px++;
                                ix++;
                                continue;
                            }
                            break;

                        case '?':
                            if (ix < input.Length)
                            {
                                px++;
                                ix++;
                                continue;
                            }
                            break;

                        case '*':
                            if (px == pattern.Length - 1)
                                return true;

                            nextPx = px;
                            nextIx = ix + 1;
                            px++;
                            continue;
                    }
                }

                if (0 < nextIx && nextIx < input.Length)
                {
                    px = nextPx;
                    ix = nextIx;
                    continue;
                }

                return false;
            }

            return true;
        }
    }
}
