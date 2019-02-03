using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace PathRenderingLab
{
    /// <summary>
    /// A class that parses a string according to the SVG Documentation in a series of path commands.
    /// See also: https://svgwg.org/specs/paths/
    /// </summary>
    public class PathParser
    {
        // Space characters
        public static readonly char[] SpaceCharacters = "\x9\x20\xA\xC\xD".ToCharArray();
        public static readonly char[] CommandCharacters = "ZzMmLlHhVvCcSsQqTtAaBb".ToCharArray();

        // Float regex
        public static readonly Regex FloatRegex = new Regex(@"\G[+-]?(\d+(\.\d*)?|\.\d+)([Ee][+-]?\d+)?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Singleline);

        // Internal list of commands
        List<PathCommand> commands;

        /// <summary>
        /// Get the commands parsed by this parser
        /// </summary>
        public ReadOnlyCollection<PathCommand> Commands => commands.AsReadOnly();

        readonly string pathString;
        int index;

        double currentBearing, lastTangent;
        Double2 lastValue, lastControl;
        PathCommandType lastCommand;

        public PathParser(string parseString)
        {
            pathString = parseString;
            commands = new List<PathCommand>();
            index = 0;
            currentBearing = 0;
            lastTangent = 0;

            lastValue = Double2.Zero;
            lastControl = Double2.Zero;
            lastCommand = PathCommandType.None;

            Parse();
        }

        // Skip whitespace
        void SkipWhitespace()
        {
            while (true)
            {
                if (index >= pathString.Length) throw new EndParseException();

                if (SpaceCharacters.Contains(pathString[index])) index++;
                else break;
            }
        }

        public static bool IsCommand(char c) => CommandCharacters.Contains(c);

        // Skip delimiter
        void SkipWhitespaceAndDelimiters()
        {
            // Only a single comma must be skipped
            SkipWhitespace();
            if (pathString[index] == ',')
            {
                index++;
                SkipWhitespace();
                if (pathString[index] == ',')
                    throw new PathParserException($"Unexpected double comma delimiter at position {index}.");
            }
        }

        // Parse a doubleing-point number
        double? ParseFloat()
        {
            SkipWhitespace();

            // Search for a doubleing-point match
            var match = FloatRegex.Match(pathString, index);

            // If the match isn't successful, return null
            if (!match.Success) return null;

            index += match.Length;
            return double.Parse(match.Value, CultureInfo.InvariantCulture);
        }

        // Parse a flag (used for that arc command)
        bool? ParseFlag()
        {
            SkipWhitespace();

            var ch = pathString[index++];
            if (ch == '0') return false;
            else if (ch == '1') return true;
            else return null;
        }

        // Parse a command
        char ParseCommand(out bool relative)
        {
            SkipWhitespace();

            // If not a command, return an unrecognized command
            if (!IsCommand(pathString[index]))
            {
                relative = false;
                return '!';
            }

            var ch = pathString[index++];
            relative = char.IsLower(ch);
            return char.ToUpperInvariant(ch);
        }

        // Get the vector resulting from the current command and the relative flag
        Double2 ProcessRelative(Double2 cmd, bool relative, bool updateLast = true)
        {
            var lval = lastValue;

            if (relative)
            {
                var cb = Math.Cos(currentBearing);
                var sb = Math.Sin(currentBearing);
                var rotatedCmd = new Double2(cmd.X * cb - cmd.Y * sb, cmd.X * sb + cmd.Y * cb);
                lval += rotatedCmd;
            }
            else lval = cmd;

            if (updateLast) lastValue = lval;
            return lval;
        }

        // Helper function to iterate through doubleing points delimited by commas
        IEnumerable<double> DelimitedSequence()
        {
            double? param = double.NaN;
            while (true)
            {
                param = ParseFloat();
                if (!param.HasValue) yield break;
                yield return param.Value;
                SkipWhitespaceAndDelimiters();
            }
        }

        // Helper function to iterate through collections of vertices)
        IEnumerable<Double2[]> DelimitedPositionTuple(int n)
        {
            int i = 0;
            bool first = true;
            var output = new Double2[n];

            // This try-block will catch a possible EndParseException
            try
            {
                while (true)
                {
                    foreach (double v in DelimitedSequence())
                    {
                        if (first) output[i].X = v;
                        else
                        {
                            output[i].Y = v;
                            i++;
                        }

                        first = !first;

                        if (i == n)
                        {
                            yield return output;
                            i = 0;
                        }
                    }

                    // Check for early closepath commands
                    if (i != 0 && (pathString[index] == 'z' || pathString[index] == 'Z'))
                    {
                        // Make sure that an even number of coordinates is supplied
                        if (!first)
                            throw new PathParserException("Supply early closepath command after an EVEN number of coordinates." +
                                $" At position {index}.");

                        index++;
                        // Fill the remaining blanks with NaN's
                        for (; i < n; i++) output[i].X = output[i].Y = double.NaN;
                        yield return output;
                        i = 0;
                    }
                    else break;
                }
            }
            finally
            {
                // Check if the wrong number of arguments was given
                // This is inside a 'finally' block to catch a possible EndParseException, which would
                // report the wrong exception
                if (i != 0 || !first) throw new PathParserException($"Wrong number of parameters passed to command at position {index}.");
            }
        }

        // Main parse loop
        void Parse()
        {
            // Use an exception to break out the loop
            try
            {
                bool isfirst = true;

                while (true)
                {
                    // Pick the command list
                    var cmd = ParseCommand(out var relative);

                    // First command MUST be a moveto
                    if (isfirst && cmd != 'M')
                        throw new PathParserException($"First path command MUST be a moveto! Error at position {index}.");

                    // Moveto command
                    if (cmd == 'M')
                    {
                        bool first = true;
                        bool onlyone = true;

                        foreach (var vs in DelimitedPositionTuple(1))
                        {
                            onlyone = first;

                            var lpos = lastValue;
                            var pos = ProcessRelative(vs[0], relative);
                            commands.Add(first ? PathCommand.MoveTo(pos) : PathCommand.LineTo(pos));
                            lastControl = pos;
                            lastTangent = first ? 0 : lpos.AngleFacing(pos);

                            first = false;
                        }

                        lastCommand = onlyone ? PathCommandType.MoveTo : PathCommandType.LineTo;
                    }
                    // Lineto command
                    else if (cmd == 'L')
                    {

                        foreach (var vs in DelimitedPositionTuple(1))
                        {
                            var lpos = lastValue;
                            var pos = ProcessRelative(vs[0], relative);
                            commands.Add(PathCommand.LineTo(pos));
                            lastTangent = lpos.AngleFacing(pos);
                            lastControl = pos;
                        }

                        lastCommand = PathCommandType.LineTo;
                    }
                    // Horizontal lineto command
                    else if (cmd == 'H')
                    {
                        foreach (var h in DelimitedSequence())
                        {
                            var lpos = lastValue;
                            var v = relative ? 0 : lpos.Y;
                            var pos = ProcessRelative(new Double2(h, v), relative);
                            commands.Add(PathCommand.LineTo(pos));
                            lastTangent = lpos.AngleFacing(pos);
                            lastControl = pos;
                        }

                        lastCommand = PathCommandType.LineTo;
                    }
                    // Vertical lineto command
                    else if (cmd == 'V')
                    {
                        foreach (var v in DelimitedSequence())
                        {
                            var lpos = lastValue;
                            var h = relative ? 0 : lpos.X;
                            var pos = ProcessRelative(new Double2(h, v), relative);
                            commands.Add(PathCommand.LineTo(pos));
                            lastTangent = lpos.AngleFacing(pos);
                            lastControl = pos;
                        }

                        lastCommand = PathCommandType.LineTo;
                    }
                    // Quadratic bezier command
                    else if (cmd == 'Q')
                    {
                        foreach (var vs in DelimitedPositionTuple(2))
                        {
                            var ctl = ProcessRelative(vs[0], relative, false);
                            var pos = ProcessRelative(vs[1], relative);
                            commands.Add(PathCommand.QuadraticCurveTo(ctl, pos));
                            lastTangent = ctl.AngleFacing(pos);
                            lastControl = ctl;
                        }

                        lastCommand = PathCommandType.QuadraticCurveTo;
                    }
                    // Smooth quadratic bezier command
                    else if (cmd == 'T')
                    {
                        foreach (var vs in DelimitedPositionTuple(1))
                        {
                            var lpos = lastValue;
                            var lctl = lastCommand == PathCommandType.QuadraticCurveTo ? lastControl : lastValue;
                            var ctl = 2 * lpos - lctl;
                            var pos = ProcessRelative(vs[0], relative);
                            commands.Add(PathCommand.QuadraticCurveTo(ctl, pos));
                            lastTangent = ctl.AngleFacing(pos);
                        }

                        lastCommand = PathCommandType.QuadraticCurveTo;
                    }
                    // Cubic bezier command
                    else if (cmd == 'C')
                    {
                        foreach (var vs in DelimitedPositionTuple(3))
                        {
                            var ctl = ProcessRelative(vs[0], relative, false);
                            var ctl2 = ProcessRelative(vs[1], relative, false);
                            var pos = ProcessRelative(vs[2], relative);
                            commands.Add(PathCommand.CubicCurveTo(ctl, ctl2, pos));
                            lastTangent = ctl2.AngleFacing(pos);
                            lastControl = ctl2;
                        }

                        lastCommand = PathCommandType.CubicCurveTo;
                    }
                    // Smooth cubic bezier command
                    else if (cmd == 'S')
                    {
                        foreach (var vs in DelimitedPositionTuple(2))
                        {
                            var lpos = lastValue;
                            var lctl = lastCommand == PathCommandType.CubicCurveTo ? lastControl : lastValue;
                            var ctl = 2 * lpos - lctl;
                            var ctl2 = ProcessRelative(vs[0], relative, false);
                            var pos = ProcessRelative(vs[1], relative);
                            commands.Add(PathCommand.CubicCurveTo(ctl, ctl2, pos));
                            lastTangent = ctl.AngleFacing(pos);
                            lastControl = ctl2;
                        }

                        lastCommand = PathCommandType.CubicCurveTo;
                    }
                    // Arc command
                    else if (cmd == 'A')
                    {
                        double ThrowF() => throw new PathParserException($"Invalid parameter passed to arc command at position {index}.");
                        bool ThrowB() => ThrowF() == 0;

                        // Pick parameters
                        while (true)
                        {
                            // If there isn't a next double, end the command
                            var next = ParseFloat();
                            if (!next.HasValue) break;

                            // Pick the doubles and flags in order
                            var rx = next.Value;
                            SkipWhitespaceAndDelimiters();
                            var ry = ParseFloat() ?? ThrowF();
                            SkipWhitespaceAndDelimiters();
                            var rAngle = ParseFloat() ?? ThrowF();
                            SkipWhitespaceAndDelimiters();
                            var largeArc = ParseFlag() ?? ThrowB();
                            SkipWhitespaceAndDelimiters();
                            var sweep = ParseFlag() ?? ThrowB();

                            Double2 target;
                            // Check if the target isn't a premature closepath
                            next = ParseFloat();
                            SkipWhitespaceAndDelimiters();
                            if (!next.HasValue)
                            {
                                if (pathString[index] == 'z' || pathString[index] == 'Z')
                                    target = new Double2(double.NaN, double.NaN);
                                else ThrowF();
                            }

                            // Assign the target
                            target.X = next.Value;
                            target.Y = ParseFloat() ?? ThrowF();

                            // Adjust for relative
                            rAngle = (rAngle.ToRadians() + (relative ? currentBearing : 0)).WrapAngle();
                            target = ProcessRelative(target, relative);

                            // Add the command
                            commands.Add(PathCommand.ArcTo(new Double2(rx, ry), rAngle, largeArc, sweep, target));
                        }
                    }
                    // Bearing command
                    else if (cmd == 'B')
                    {
                        if (!relative) currentBearing = DelimitedSequence().Last().ToRadians();
                        else currentBearing += DelimitedSequence().Sum().ToRadians();

                        lastCommand = PathCommandType.None;
                    }
                    // Complete closepath command
                    else if (cmd == 'Z')
                    {
                        commands.Add(PathCommand.ClosePath());
                        lastCommand = PathCommandType.ClosePath;
                    }
                    // Unknown command
                    else throw new PathParserException($"Unrecognized command at position {index-1}.");

                    isfirst = false;
                }
            }
            catch (EndParseException)
            {
                // The expression says it
            }
        }
    }

    [Serializable]
    internal class EndParseException : Exception
    {
        public EndParseException() {}
        public EndParseException(string message) : base(message) {}
        public EndParseException(string message, Exception innerException) : base(message, innerException) {}
        protected EndParseException(SerializationInfo info, StreamingContext context) : base(info, context) {}
    }

    [Serializable]
    public class PathParserException : Exception
    {
        public PathParserException() { }
        public PathParserException(string message) : base(message) { }
        public PathParserException(string message, Exception inner) : base(message, inner) { }
        protected PathParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}