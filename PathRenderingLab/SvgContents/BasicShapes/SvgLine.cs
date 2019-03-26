using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents.BasicShapes
{
    public class SvgLine : SvgPath
    {
        public SvgLine(XmlNode child, SvgGroup parent, Svg svg) : base(child, parent, svg)
        {

        }

        protected override Path ParsePath(Dictionary<string, string> properties)
        {
            // Get the attributes of the line
            var x1 = ParseLengthX(properties.GetOrDefault("x1")) ?? 0;
            var y1 = ParseLengthY(properties.GetOrDefault("y1")) ?? 0;

            var x2 = ParseLengthX(properties.GetOrDefault("x2")) ?? 0;
            var y2 = ParseLengthY(properties.GetOrDefault("y2")) ?? 0;

            // And finally return
            return new Path(new[]
            {
                PathCommand.MoveTo(new Double2(x1, y1)),
                PathCommand.LineTo(new Double2(x2, y2))
            });
        }
    }
}
