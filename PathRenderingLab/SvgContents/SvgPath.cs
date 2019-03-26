using System.Collections.Generic;
using System.Xml;

namespace PathRenderingLab.SvgContents
{
    /// <summary>
    /// A class that represents an SVG path element
    /// </summary>
    public class SvgPath : SvgNode
    {
        /// <summary>
        /// The path data for this path
        /// </summary>
        public Path Path { get; private set; }

        /// <summary>
        /// The path length, used for dashing
        /// </summary>
        public double PathLength { get; private set; }

        public SvgPath(XmlNode child, SvgGroup parent, Svg svg) : base(child, parent, svg)
        {

        }

        // Override the parse
        protected override void Parse(Dictionary<string, string> properties)
        {
            // Parse the common properties
            base.Parse(properties);

            // Parse the path data
            Path = ParsePath(properties);

            // And the path length
            PathLength = DoubleUtils.TryParse(properties.GetOrDefault("pathLength")) ?? double.NaN;
        }

        protected virtual Path ParsePath(Dictionary<string, string> properties)
            => new Path(properties.GetOrDefault("d", ""));
    }
}