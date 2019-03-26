using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents.BasicShapes
{
    public class SvgCircle : SvgPath
    {
        public SvgCircle(XmlNode child, SvgGroup parent, Svg svg) : base(child, parent, svg)
        {

        }

        protected override Path ParsePath(Dictionary<string, string> properties)
        {
            // Get the attributes of the circle
            var cx = DoubleUtils.TryParse(properties.GetOrDefault("cx")) ?? 0;
            var cy = DoubleUtils.TryParse(properties.GetOrDefault("cy")) ?? 0;
            var r = DoubleUtils.TryParse(properties.GetOrDefault("r")) ?? 0;

            // Quit on invalid values
            if (r <= 0) return new Path();

            var rr = new Double2(r, r);
            // Mount the path commands
            return new Path(new[]
            {
                PathCommand.MoveTo(new Double2(cx + r, cy)),
                PathCommand.ArcTo(rr, 0, false, true, new Double2(cx, cy + r)),
                PathCommand.ArcTo(rr, 0, false, true, new Double2(cx - r, cy)),
                PathCommand.ArcTo(rr, 0, false, true, new Double2(cx, cy - r)),
                PathCommand.ArcToClose(rr, 0, false, true)
            });
        }
    }
}
