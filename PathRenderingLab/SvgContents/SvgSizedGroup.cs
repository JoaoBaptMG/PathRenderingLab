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

        public SvgSizedGroup(XmlNode node, SvgNode parent, Svg svg, bool renderable = true) : base(node, parent, svg, renderable)
        {

        }

        protected override void Parse(Dictionary<string, string> properties)
        {
            base.Parse(properties);

            // Parse the width and height
            Width = ParseLengthX(properties.GetOrDefault("width")) ?? 0;
            Height = ParseLengthY(properties.GetOrDefault("height")) ?? 0;
        }
    }
}
