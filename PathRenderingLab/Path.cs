using System.Collections.Generic;
using System.Linq;
using PathRenderingLab.PathCompiler;
using PathRenderingLab.Parsers;
using System;

namespace PathRenderingLab
{
    public enum FillRule { Nonzero, Evenodd }
    public enum StrokeLineCap { Butt, Round, Square }
    public enum StrokeLineJoin { Bevel, Miter, MiterClip, Round, Arcs }

    public enum PathCommandType { None, MoveTo, LineTo, QuadraticCurveTo, CubicCurveTo, ArcTo, ClosePath }

    public struct PathCommand
    {
        public PathCommandType Type;
        public Double2 Target;

        public Double2 Control;
        public Double2 Control2;

        public static PathCommand None => new PathCommand { Type = PathCommandType.None };
        public static PathCommand MoveTo(Double2 target) => new PathCommand { Type = PathCommandType.MoveTo, Target = target };
        public static PathCommand LineTo(Double2 target) => new PathCommand { Type = PathCommandType.LineTo, Target = target };
        public static PathCommand LineToClose() => LineTo(new Double2(double.NaN, double.NaN));
        public static PathCommand QuadraticCurveTo(Double2 control, Double2 target) =>
            new PathCommand { Type = PathCommandType.QuadraticCurveTo, Target = target, Control = control };
        public static PathCommand QuadraticCurveToClose(Double2 control) => QuadraticCurveTo(control, new Double2(double.NaN, double.NaN));
        public static PathCommand CubicCurveTo(Double2 control1, Double2 control2, Double2 target) =>
            new PathCommand { Type = PathCommandType.CubicCurveTo, Target = target, Control = control1, Control2 = control2 };
        public static PathCommand CubicCurveToClose(Double2 control1, Double2 control2) =>
            CubicCurveTo(control1, control2, new Double2(double.NaN, double.NaN));
        public static PathCommand ClosePath() => new PathCommand { Type = PathCommandType.ClosePath };

        public static PathCommand ArcTo(Double2 radii, double xAngle, bool largeArc, bool sweep, Double2 target) => new PathCommand
        {
            Type = PathCommandType.ArcTo,
            Target = target,
            Control = radii,
            Control2 = new Double2(xAngle, (largeArc ? 1f : 0f) + (sweep ? 2f : 0f))
        };

        public static PathCommand ArcToClose(Double2 radii, double xAngle, bool largeArc, bool sweep)
            => ArcTo(radii, xAngle, largeArc, sweep, new Double2(double.NaN, double.NaN));

        public override string ToString()
        {
            switch (Type)
            {
                case PathCommandType.None: return "none";
                case PathCommandType.MoveTo: return $"M {Target.X},{Target.Y}";
                case PathCommandType.LineTo: return $"L {Target.X},{Target.Y}";
                case PathCommandType.QuadraticCurveTo: return $"Q {Control.X},{Control.Y} {Target.X},{Target.Y}";
                case PathCommandType.CubicCurveTo: return $"C {Control.X},{Control.Y} {Control2.X},{Control2.Y} {Target.X},{Target.Y}";
                case PathCommandType.ArcTo:
                    var largeFlag = ((int)Control2.Y & 1) != 0 ? 1 : 0;
                    var sweep = ((int)Control2.Y & 2) != 0 ? 1 : 0;
                    return $"A {Control.X},{Control.Y} {Control2.X} {largeFlag} {sweep} {Target.X},{Target.Y}";
                case PathCommandType.ClosePath: return "Z";
                default: return "";
            }
        }
    }

    public struct CurveData
    {
        public readonly Curve[] Curves;
        public readonly bool IsClosed;

        public CurveData(Curve[] curves, bool closed)
        {
            Curves = curves;
            IsClosed = closed;
        }
    }

    public class Path
    {
        public PathCommand[] PathCommands;

        public Path() : this(new PathCommand[0]) { }

        public Path(PathCommand[] pathCommands) { PathCommands = pathCommands; }

        public Path(string pathString)
        {
            var parser = new PathParser(pathString);
            PathCommands = parser.Commands.ToArray();
        }

        public static IEnumerable<CurveData> SplitCurves(IEnumerable<PathCommand> pathCommands)
        {
            // Reunite all the shapes
            var curves = new List<Curve>();

            var firstVec = Double2.Zero;
            var prevVec = Double2.Zero;

            CurveData NewCurveList(bool closed)
            {
                var list = curves.SelectMany(c => c.Simplify(true)).Where(c => !c.IsDegenerate).ToArray();
                curves.Clear();
                return new CurveData(list, closed);
            }

            foreach (var cmd in pathCommands)
            {
                // A vector of NaNs indicate the path is closing
                var target = double.IsNaN(cmd.Target.X) ? firstVec : cmd.Target;
                var control = double.IsNaN(cmd.Control.X) ? firstVec : cmd.Control;
                var control2 = double.IsNaN(cmd.Control2.X) ? firstVec : cmd.Control2;

                // By command type:
                switch (cmd.Type)
                {
                    case PathCommandType.None: continue;
                    case PathCommandType.MoveTo:
                        {
                            // Yield the last curve list if any
                            if (curves.Count > 0) yield return NewCurveList(false);
                            firstVec = target; break;
                        }
                    case PathCommandType.LineTo: curves.Add(Curve.Line(prevVec, target)); break;
                    case PathCommandType.QuadraticCurveTo:
                        curves.Add(Curve.QuadraticBezier(prevVec, control, target)); break;
                    case PathCommandType.CubicCurveTo:
                        curves.Add(Curve.CubicBezier(prevVec, control, control2, target)); break;
                    case PathCommandType.ArcTo:
                        {
                            var largeArc = ((int)control2.Y & 1) != 0;
                            var sweep = ((int)control2.Y & 2) != 0;
                            curves.Add(Curve.EllipticArc(prevVec, control, control2.X, largeArc, sweep, target));
                        }
                        break;
                    case PathCommandType.ClosePath:
                        target = firstVec;
                        curves.Add(Curve.Line(prevVec, target));
                        break;
                }

                // Yield the last curve list if any
                bool isClose = cmd.Type == PathCommandType.ClosePath || double.IsNaN(cmd.Target.X)
                    || double.IsNaN(cmd.Control.X) || double.IsNaN(cmd.Control2.X);
                if (isClose && curves.Count > 0) yield return NewCurveList(true);

                prevVec = target;
            }

            if (curves.Count > 0) yield return new CurveData(curves.SelectMany(c => c.Simplify(true)).ToArray(), false);
        }

        public static IEnumerable<Curve> Curves(PathCommand[] pathCommands) => SplitCurves(pathCommands).SelectMany(x => x.Curves);

        public IEnumerable<CurveData> SplitCurves() => SplitCurves(PathCommands);
        public IEnumerable<Curve> Curves() => Curves(PathCommands);

        public DoubleMatrix NormalizeAndTruncate(double halfSize = 2048)
        {
            // Calculate the bounding box of the path
            var points = Curves().SelectMany(c => c.EnclosingPolygon).ToArray();

            // Don't normalize an empty path
            if (points.Length == 0) return DoubleMatrix.Identity;

            var minx = double.PositiveInfinity;
            var maxx = double.NegativeInfinity;
            var miny = double.PositiveInfinity;
            var maxy = double.NegativeInfinity;

            // Find the bounding box
            foreach (var pt in points)
            {
                minx = Math.Min(minx, pt.X);
                maxx = Math.Max(maxx, pt.X);
                miny = Math.Min(miny, pt.Y);
                maxy = Math.Max(maxy, pt.Y);
            }

            // Calculate the points
            var center = new Double2(minx + maxx, miny + maxy) / 2;

            // And the half-sizes
            var hx = (maxx - minx) / 2;
            var hy = (maxy - miny) / 2;

            // Chose the maximum value of it
            var d = halfSize / Math.Max(hx, hy);

            var dm = new Double2(DoubleUtils.Epsilon / 2, DoubleUtils.Epsilon / 2);

            // And transform the points
            for (int i = 0; i < PathCommands.Length; i++)
            {
                switch (PathCommands[i].Type)
                {
                    case PathCommandType.MoveTo:
                    case PathCommandType.LineTo:
                        PathCommands[i].Target = (PathCommands[i].Target * d - center * d).Truncate() + dm; break;
                    case PathCommandType.QuadraticCurveTo:
                        PathCommands[i].Target = (PathCommands[i].Target * d - center * d).Truncate() + dm;
                        PathCommands[i].Control = (PathCommands[i].Control * d - center * d).Truncate() + dm;
                        break;
                    case PathCommandType.CubicCurveTo:
                        PathCommands[i].Target = (PathCommands[i].Target * d - center * d).Truncate() + dm;
                        PathCommands[i].Control = (PathCommands[i].Control * d - center * d).Truncate() + dm;
                        PathCommands[i].Control2 = (PathCommands[i].Control2 * d - center * d).Truncate() + dm;
                        break;
                    case PathCommandType.ArcTo:
                        PathCommands[i].Target = (PathCommands[i].Target * d - center * d).Truncate() + dm;
                        PathCommands[i].Control = (PathCommands[i].Control * d).Truncate();
                        break;
                    default: break;
                }
            }

            // Mount the inverse matrix
            return new DoubleMatrix(1 / d, 0, 0, 1 / d, center.X, center.Y);
        }

        public override string ToString() => string.Join(" ", PathCommands);
    }
}
