using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PathRenderingLab.Parsers;

namespace PathRenderingLab.SvgContents
{
    public enum LengthType { Diagonal, Width, Height };

    public class SvgNode
    {
        /// <summary>
        /// The ID of the current node
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The node's parent, of null if it is the root element
        /// </summary>
        public SvgNode Parent { get; private set; }

        /// <summary>
        /// The transform this node is submitted to
        /// </summary>
        public TransformFunction[] Transform { get; private set; }

        /// <summary>
        /// The path style applied to its node
        /// </summary>
        public PathStyle PathStyle { get; private set; }

        protected Svg svg;

        // The most general constructor
        protected SvgNode(XmlNode node, SvgNode parent, Svg svg, string transformAttributeName = "transform")
        {
            // Assign the ID for everyone
            Parent = parent;
            Id = node.SvgAttribute("id")?.Trim();
            this.svg = svg;

            // Add id to the svg-dictionary
            if (!string.IsNullOrWhiteSpace(Id) && !svg.NodesByID.ContainsKey(Id))
                svg.NodesByID[Id] = this;

            // Build the property list
            var properties = new Dictionary<string, string>();

            // Go through all attributes
            foreach (XmlAttribute attr in node.Attributes)
            {
                // Skip if the attribute doesn't belong to the SVG namespace or no namespace
                if (!string.IsNullOrEmpty(attr.NamespaceURI) && attr.NamespaceURI != Svg.Namespace) continue;

                // Skip if the attribute is style or id
                if (attr.LocalName == "style" || attr.LocalName == "id") continue;

                // Attach it to the properties
                if (attr.LocalName == transformAttributeName)
                    properties["transform"] = attr.Value.Trim();
                else properties[attr.LocalName] = attr.Value.Trim();
            }

            // Parse the CSS property list in style
            var styleProperties = node.SvgAttribute("style").ToPropertyList();

            foreach (var kvp in styleProperties)
                properties[kvp.Key] = kvp.Value;

            // Now, parse
            Parse(properties);
        }

        protected virtual void Parse(Dictionary<string, string> properties)
        {
            // Parse the transform
            Transform = TransformFunction.ParseCollectionFromString(properties.GetOrDefault("transform", "none"));
            if (Parent != null) Transform = Parent.Transform.Concat(Transform);

            // Parse the most general arguments: fill, fill-rule, stroke, stroke-width, stroke-linejoin, stroke-linecap
            PathStyle = new PathStyle(properties, Parent?.PathStyle);
        }

        // Utility functions to parse lengths
        protected SvgSizedGroup InnermostSizedGroup()
        {
            for (var parent = Parent; parent != null; parent = parent.Parent)
                if (parent is SvgSizedGroup) return parent as SvgSizedGroup;
            return null;
        }

        protected double? ParseLength(string str, LengthType lengthType = LengthType.Diagonal)
        {
            // First, trim the str and guarantee that there are no spaces left
            var strt = str.Trim();
            if (strt.Any(char.IsWhiteSpace)) return null;

            // Now, match it according to the float regex
            var match = ParserBase.FloatRegex.Match(strt);

            // Discard unsuccessful match
            if (!match.Success) return null;

            // Parse the main value
            if (!double.TryParse(match.Groups[1].Value, out var value)) return null;

            // Now, parse the unit
            switch (strt.Substring(match.Index + match.Length).ToLowerInvariant())
            {
                case "px": break; // base unit
                case "in": value *= 96; break; // inch
                case "cm": value *= 96 / 2.54; break; // centimeters
                case "mm": value *= 96 / 2.54 / 10; break; // milimeters
                case "q": value *= 96 / 2.54 / 40; break; // quarter-milimeters
                case "pc": value *= 16; break; // picas
                case "pt": value *= 4d / 3; break; // points

                // Percentage
                case "%":
                    {
                        // Get the innermost container element that defines a viewport
                        var sized = InnermostSizedGroup();
                        if (sized == null) return null;

                        // According to the length type, parse
                        switch (lengthType)
                        {
                            case LengthType.Diagonal: value *= sized.NormalizedDiagonal; break;
                            case LengthType.Width: value *= sized.Width; break;
                            case LengthType.Height: value *= sized.Height; break;
                            default: throw new ArgumentException("Unknown lengthType!", nameof(lengthType));
                        }

                        value /= 100;
                        break;
                    }

                // Same as above, but with specific things
                case "vw": lengthType = LengthType.Width; goto case "%";
                case "vh": lengthType = LengthType.Height; goto case "%";

                case "vmin":
                    {
                        var sized = InnermostSizedGroup();
                        if (sized == null) return null;
                        lengthType = sized.Width < sized.Height ? LengthType.Width : LengthType.Height;
                        goto case "%";
                    }

                case "vmax":
                    {
                        var sized = InnermostSizedGroup();
                        if (sized == null) return null;
                        lengthType = sized.Width > sized.Height ? LengthType.Width : LengthType.Height;
                        goto case "%";
                    }

                default: return null; // invalid unit
            }

            // Return the value if possible
            return value;
        }

        protected double? ParseLengthX(string str) => ParseLength(str, LengthType.Width);
        protected double? ParseLengthY(string str) => ParseLength(str, LengthType.Height);
    }
}
