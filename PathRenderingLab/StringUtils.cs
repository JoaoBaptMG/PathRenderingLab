using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab
{
    public static class StringUtils
    {
        public static string[] SplitIntoWords(this string str)
        {
            var strs = new List<string>();

            int cur = 0;
            for (int i = 1; i < str.Length; i++)
            {
                if (char.IsUpper(str[i]))
                {
                    strs.Add(str.Substring(cur, i - cur));
                    cur = i;
                }
            }

            strs.Add(str.Substring(cur));
            return strs.ToArray();
        }

        public static string ConvertToCSSCase(string name) => string.Join("-", name.SplitIntoWords().Select(x => x.ToLowerInvariant()));

        // Used only for parsers
        public static bool MatchesOn(this string str, ref int pos, string match)
        {
            if (pos + match.Length <= str.Length && str.Substring(pos, match.Length) == match)
            {
                pos += match.Length;
                return true;
            }
            return false;
        }
    }
}
