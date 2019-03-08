using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Globalization;

namespace PathRenderingLab.Parsers
{
    public abstract class ParserBase
    {
        // Space characters
        public static readonly char[] SpaceCharacters = "\x9\x20\xA\xC\xD".ToCharArray();

        // Float regex
        public static readonly Regex FloatRegex = new Regex(@"\G[+-]?(\d+(\.\d*)?|\.\d+)([Ee][+-]?\d+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        protected readonly string parseString;
        protected int index;

        protected ParserBase(string parseString)
        {
            this.parseString = parseString;
            index = 0;
        }

        // Skip whitespace
        protected void SkipWhitespace()
        {
            while (true)
            {
                if (index >= parseString.Length) throw new EndParseException();
                if (SpaceCharacters.Contains(parseString[index])) index++;
                else break;
            }
        }

        // Skip delimiter
        // Returns whether a comma was read and skipped
        protected bool SkipWhitespaceAndDelimiters()
        {
            // Only a single comma must be skipped
            SkipWhitespace();
            if (parseString[index] == ',')
            {
                index++;
                SkipWhitespace();
                if (parseString[index] == ',')
                    throw new ParserException($"Unexpected double comma delimiter at position {index}.");
                return true;
            }
            else return false;
        }

        // Parse a double-precision floating-point number
        protected double? ParseDouble()
        {
            SkipWhitespace();

            // Search for a doubleing-point match
            var match = FloatRegex.Match(parseString, index);

            // If the match isn't successful, return null
            if (!match.Success) return null;

            index += match.Length;
            return double.Parse(match.Value, CultureInfo.InvariantCulture);
        }

        // Helper function to iterate through floating points delimited by commas
        protected IEnumerable<double> DelimitedSequence(int minValue = 0, int maxValue = int.MaxValue)
        {
            double? param = null;
            for (int i = 0; i < maxValue; i++)
            {
                param = ParseDouble();
                if (!param.HasValue)
                {
                    if (i < minValue) throw new ParserException($"Delimited floating-point sequence expected " +
                        $"minimum of {minValue} values.");
                    yield break;
                }
                yield return param.Value;
                SkipWhitespaceAndDelimiters();
            }
            throw new ParserException($"Delimited floating-point sequence expected maximum of {maxValue} values.");
        }

    }
}
