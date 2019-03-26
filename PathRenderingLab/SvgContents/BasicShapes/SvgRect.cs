using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents.BasicShapes
{
    public class SvgRect : SvgPath
    {
        public SvgRect(XmlNode child, SvgGroup parent, Svg svg) : base(child, parent, svg)
        {

        }

        protected override Path ParsePath(Dictionary<string, string> properties)
        {
            // Get the attributes of the rectangle
            var x = ParseLengthX(properties.GetOrDefault("x")) ?? 0;
            var y = ParseLengthY(properties.GetOrDefault("y")) ?? 0;
            var width = ParseLengthX(properties.GetOrDefault("width")) ?? 0;
            var height = ParseLengthY(properties.GetOrDefault("height")) ?? 0;

            var radii = new Double2();
            radii.X = ParseLengthX(properties.GetOrDefault("rx")) ?? double.NaN;
            radii.Y = ParseLengthY(properties.GetOrDefault("ry")) ?? double.NaN;

            // Quit on invalid values
            if (width <= 0 || height <= 0) return new Path();

            // Check both radii
            if (double.IsNaN(radii.X) && double.IsNaN(radii.Y)) radii.X = radii.Y = 0;
            else if (double.IsNaN(radii.X)) radii.X = radii.Y;
            else if (double.IsNaN(radii.Y)) radii.Y = radii.X;

            // Take out the sign and clamp them
            radii.X = Math.Abs(radii.X);
            radii.Y = Math.Abs(radii.Y);

            if (radii.X > width / 2) radii.X = width / 2;
            if (radii.Y > height / 2) radii.Y = height / 2;

            // Now mount the path commands
            PathCommand[] pathCommands;
            if (radii.X == 0 || radii.Y == 0) pathCommands = new[]
            {
                PathCommand.MoveTo(new Double2(x, y)),
                PathCommand.LineTo(new Double2(x + width, y)),
                PathCommand.LineTo(new Double2(x + width, y + height)),
                PathCommand.LineTo(new Double2(x, y + height)),
                PathCommand.ClosePath()
            };
            else pathCommands = new[]
            {
                PathCommand.MoveTo(new Double2(x + radii.X, y)),
                PathCommand.LineTo(new Double2(x + width - radii.X, y)),
                PathCommand.ArcTo(radii, 0, false, true, new Double2(x + width, y + radii.Y)),
                PathCommand.LineTo(new Double2(x + width, y + height - radii.Y)),
                PathCommand.ArcTo(radii, 0, false, true, new Double2(x + width - radii.X, y + height)),
                PathCommand.LineTo(new Double2(x + radii.X, y + height)),
                PathCommand.ArcTo(radii, 0, false, true, new Double2(x, y + height - radii.Y)),
                PathCommand.LineTo(new Double2(x, y + radii.Y)),
                PathCommand.ArcToClose(radii, 0, false, true)
            };

            // And finally return
            return new Path(pathCommands);
        }
    }
}
