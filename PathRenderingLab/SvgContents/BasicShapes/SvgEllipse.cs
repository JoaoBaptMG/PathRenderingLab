using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents.BasicShapes
{
    public class SvgEllipse : SvgPath
    {
        public SvgEllipse(XmlNode child, SvgGroup parent) : base(child, parent)
        {

        }

        protected override Path ParsePath(Dictionary<string, string> properties)
        {
            // Get the attributes of the ellipse
            var cx = DoubleUtils.TryParse(properties.GetOrDefault("cx")) ?? 0;
            var cy = DoubleUtils.TryParse(properties.GetOrDefault("cy")) ?? 0;

            var radii = new Double2();
            radii.X = DoubleUtils.TryParse(properties.GetOrDefault("rx")) ?? double.NaN;
            radii.Y = DoubleUtils.TryParse(properties.GetOrDefault("ry")) ?? double.NaN;

            // Check both radii
            if (double.IsNaN(radii.X) && double.IsNaN(radii.Y)) radii.X = radii.Y = 0;
            else if (double.IsNaN(radii.X)) radii.X = radii.Y;
            else if (double.IsNaN(radii.Y)) radii.Y = radii.X;

            // Take out the sign and clamp them
            radii.X = Math.Abs(radii.X);
            radii.Y = Math.Abs(radii.Y);

            // Quit on invalid values
            if (radii.X <= 0 || radii.Y <= 0) return new Path();

            // Mount the path commands
            return new Path(new[]
            {
                PathCommand.MoveTo(new Double2(cx + radii.X, cy)),
                PathCommand.ArcTo(radii, 0, false, true, new Double2(cx, cy + radii.Y)),
                PathCommand.ArcTo(radii, 0, false, true, new Double2(cx - radii.X, cy)),
                PathCommand.ArcTo(radii, 0, false, true, new Double2(cx, cy - radii.Y)),
                PathCommand.ArcToClose(radii, 0, false, true)
            });
        }
    }
}
