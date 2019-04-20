using PathRenderingLab.SvgContents;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PathRenderingLab.Parsers
{
    /// <summary>
    /// A class that parses transform function collections according with the SVG syntax of transform
    /// See also: https://www.w3.org/TR/css-transforms-1/#svg-syntax
    /// </summary>
    public class TransformParser : ParserBase
    {
        // Internal list of transform functions
        List<TransformFunction> transformFunctions;

        /// <summary>
        /// Get the transform functions parsed by this parser
        /// </summary>
        public ReadOnlyCollection<TransformFunction> TransformFunctions => transformFunctions.AsReadOnly();

        public TransformParser(string parseString) : base(parseString)
        {
            transformFunctions = new List<TransformFunction>();
            Parse();
        }

        // Parenthesized sequence of delimited floating-point numbers
        double[] DelimitedParenthesizedSequence(int minValue, int maxValue)
        {
            // The temporary list
            var list = new List<double>(maxValue);

            // Skip whitespace
            SkipWhitespace();

            // Check if the first thing is a parenthesis
            if (!Matches('(')) throw new ParserException("Expected '(' to begin parenthesized floating-point sequence!");

            // Now, check for the delimited sequence itself
            double? val = null;
            bool comma = false;

            for (int i = 0; i < maxValue + 1; i++)
            {
                // Throw if there is a comma before the first sequence
                comma = SkipWhitespaceAndDelimiters();
                if (comma && i == 0) throw new ParserException("Unexpected ',' after parenthesized floating-point" +
                    "sequence beginning!");

                val = ParseDouble();

                // Act according to the precense of a value or not
                if (!val.HasValue) break;
                else if (i == maxValue) throw new ParserException($"Delimited floating-point sequence expected " +
                    $"maximum of {maxValue} values.");

                list.Add(val.Value);
            }

            // If there is a comma after the last parameter, it's wrong too
            if (comma) throw new ParserException("Unexpected ',' before parenthesized floating-point sequence end!");

            // Now, close the parentheses
            if (!Matches(')')) throw new ParserException("Expected ')' to end parenthesized floating-point sequence!");

            // Throw away the list if it does not have the required number of parameters
            if (list.Count < minValue) throw new ParserException($"Delimited floating-point sequence expected " +
                $"minimum of {minValue} values.");

            return list.ToArray();
        }

        // Parse a transform function
        TransformFunctionType ParseTransformFunctionType()
        {
            // First, skip whitespace
            SkipWhitespace();

            // Then, check if it matches one of the function names
            if (Matches("translate")) return TransformFunctionType.Translate;
            if (Matches("scale")) return TransformFunctionType.Scale;
            if (Matches("rotate")) return TransformFunctionType.Rotate;
            if (Matches("skewX")) return TransformFunctionType.SkewX;
            if (Matches("skewY")) return TransformFunctionType.SkewY;
            if (Matches("matrix")) return TransformFunctionType.Matrix;

            // If not, unrecognized command
            throw new ParserException("Unrecognized transform function matched!");
        }

        // Main parse loop
        void Parse()
        {
            // First, try to see if the transform is actually "no transform"
            if (parseString.Trim() == "none") return;

            // Flag to report an error if a transform function ends too soon
            bool allowEnd = true;

            try
            {
                while (true)
                {
                    // First, try to extract a command
                    allowEnd = true;
                    var function = ParseTransformFunctionType();
                    allowEnd = false;

                    switch (function)
                    {
                        case TransformFunctionType.Matrix:
                            {
                                var a = DelimitedParenthesizedSequence(6, 6);
                                transformFunctions.Add(TransformFunction.Matrix(a[0], a[1], a[2], a[3], a[4], a[5]));
                                break;
                            }
                        case TransformFunctionType.Translate:
                            {
                                var a = DelimitedParenthesizedSequence(1, 2);
                                transformFunctions.Add(TransformFunction.Translate(a[0], a.TryGetOr(1, 0)));
                                break;
                            }
                        case TransformFunctionType.Rotate:
                            {
                                var a = DelimitedParenthesizedSequence(1, 3);
                                transformFunctions.Add(TransformFunction.Rotate(a[0], new Double2(a.TryGetOr(1, 0), a.TryGetOr(2, 0))));
                                break;
                            }
                        case TransformFunctionType.Scale:
                            {
                                var a = DelimitedParenthesizedSequence(1, 2);
                                transformFunctions.Add(TransformFunction.Scale(a[0], a.TryGetOr(1, a[0])));
                                break;
                            }
                        case TransformFunctionType.SkewX:
                            {
                                var a = DelimitedParenthesizedSequence(1, 1);
                                transformFunctions.Add(TransformFunction.SkewX(a[0].ToRadians()));
                                break;
                            }
                        case TransformFunctionType.SkewY:
                            {
                                var a = DelimitedParenthesizedSequence(1, 1);
                                transformFunctions.Add(TransformFunction.SkewY(a[0].ToRadians()));
                                break;
                            }
                        default:
                            // This code will never come, but the C# compiler cannot detect it
                            throw new ParserException("Unrecognized transform function matched!");
                    }
                }
            }
            catch (EndParseException)
            {
                // The name says it all
                if (!allowEnd) throw new ParserException("Unexpected end of transform string!");
            }
        }
    }
}
