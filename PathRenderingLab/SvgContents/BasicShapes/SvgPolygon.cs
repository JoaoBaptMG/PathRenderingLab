using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents.BasicShapes
{
    public class SvgPolygon : SvgPath
    {
        public SvgPolygon(XmlNode node, SvgNode parent, Svg svg) : base(node, parent, svg)
        {

        }

        protected override Path ParsePath(Dictionary<string, string> properties)
        {
            // Get the attributes of the polygon
            var points = properties.GetOrDefault("points");

            // No path for a return string
            if (string.IsNullOrWhiteSpace(points)) return new Path();

            // Check the syntax of the points
            bool IsCharacterAllowed(char c) => char.IsDigit(c) || c == '+' || c == '-' || c == '.'
                || c == 'E' || c == 'e' || char.IsWhiteSpace(c);
            if (!points.All(IsCharacterAllowed)) return new Path();

            // Use the path parser to parse the polygon
            return new Path("M " + points + " Z");
        }
    }
}
