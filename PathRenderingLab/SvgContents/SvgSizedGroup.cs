using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents
{
    public class SvgSizedGroup : SvgGroup
    {
        public double Width { get; private set; }

        public double Height { get; private set; }

        public double NormalizedDiagonal => Math.Sqrt(0.5 * (Width * Width + Height * Height));

        public SvgSizedGroup(XmlNode node, SvgNode parent, Svg svg, bool renderable = true, SvgUse overrideNode = null) 
            : base(node, parent, svg, renderable)
        {
            // Pick the width and height from an overrided "use" element
            if (overrideNode != null)
            {
                Width = overrideNode.Width;
                Height = overrideNode.Height;
            }
        }

        protected override void Parse(XmlNode node, Dictionary<string, string> properties)
        {
            base.Parse(node, properties);

            // Parse the "viewBox" attribute (if any)
            double? vWidth, vHeight;
            if (node.LocalName == "svg")
                DeriveViewBoxWidthAndHeight(properties.GetOrDefault("viewBox", ""), out vWidth, out vHeight);
            else vWidth = vHeight = null;

            // Parse the width and height (if they were not already overriden)
            Width = ParseLengthX(properties.GetOrDefault("width")) ?? vWidth ?? 0;
            Height = ParseLengthY(properties.GetOrDefault("height")) ?? vHeight ?? 0;
        }

        private static void DeriveViewBoxWidthAndHeight(string viewBox, out double? vWidth, out double? vHeight)
        {
            vWidth = vHeight = null;

            // Bail out if no string is to be parsed
            if (string.IsNullOrWhiteSpace(viewBox)) return;

            // Split it into multiple numbers
            var numbers = viewBox.Split(' ')
                .Select(DoubleUtils.TryParse)
                .Where(d => d.HasValue)
                .Select(d => d.Value).ToArray();

            // Attribute the numbers whether neccessary
            if (numbers.Length > 2) vWidth = numbers[2];
            if (numbers.Length > 3) vHeight = numbers[3];
        }
    }
}
