using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Svg.Pathing;

namespace PathRenderingLab.PathCompiler
{
    public static class PathUtils
    {
        public static Double2 ToDouble2(this System.Drawing.PointF p) => new Double2(p.X, p.Y);

        public static IEnumerable<CurveData> SplitCurves(this SvgPathSegmentList pathSegments)
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

            foreach (var seg in pathSegments)
            {
                var target = seg.End.ToDouble2();

                // Branch into the segments as necessary
                if (seg is SvgMoveToSegment moveTo)
                {
                    // Yield the last curve list if any
                    if (curves.Count > 0) yield return NewCurveList(false);
                    firstVec = target;
                }
                else if (seg is SvgLineSegment line)
                    curves.Add(Curve.Line(line.Start.ToDouble2(), line.End.ToDouble2()));
                else if (seg is SvgQuadraticCurveSegment quad)
                    curves.Add(Curve.QuadraticBezier(quad.Start.ToDouble2(), quad.ControlPoint.ToDouble2(), quad.End.ToDouble2()));
                else if (seg is SvgCubicCurveSegment cubic)
                    curves.Add(Curve.CubicBezier(cubic.Start.ToDouble2(), cubic.FirstControlPoint.ToDouble2(),
                        cubic.SecondControlPoint.ToDouble2(), cubic.End.ToDouble2()));
                else if (seg is SvgArcSegment arc)
                    curves.Add(Curve.EllipticArc(arc.Start.ToDouble2(), new Double2(arc.RadiusX, arc.RadiusY),
                        ((double)arc.Angle).ToRadians(), arc.Size == SvgArcSize.Large, arc.Sweep == SvgArcSweep.Positive,
                        arc.End.ToDouble2()));
                else if (seg is SvgClosePathSegment)
                {
                    target = firstVec;
                    curves.Add(Curve.Line(prevVec, target));
                    if (curves.Count > 0) yield return NewCurveList(true);
                }

                prevVec = target;
            }

            if (curves.Count > 0) yield return NewCurveList(false);
        }

        public static SvgPathSegmentList NormalizeAndTruncate(this SvgPathSegmentList path, out DoubleMatrix transform, double halfSize = 2048)
        {
            // Calculate the bounding box of the path
            var points = (from cd in path.SplitCurves()
                          from curve in cd.Curves
                          from p in curve.EnclosingPolygon
                          select p).ToArray();

            // Don't normalize an empty path
            if (points.Length == 0)
            {
                transform = DoubleMatrix.Identity;
                return path;
            }

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
            var dm = Double2.Zero;

            // And transform the path
            var newPath = new SvgPathSegmentList();
            foreach (var segment in path)
            {
                if (segment is SvgMoveToSegment move)
                    newPath.Add(new SvgMoveToSegment(Transform(move.Start)));
                else if (segment is SvgLineSegment line)
                    newPath.Add(new SvgLineSegment(Transform(line.Start), Transform(line.End)));
                else if (segment is SvgQuadraticCurveSegment quad)
                    newPath.Add(new SvgQuadraticCurveSegment(Transform(quad.Start), Transform(quad.ControlPoint), Transform(quad.End)));
                else if (segment is SvgCubicCurveSegment cubic)
                    newPath.Add(new SvgCubicCurveSegment(Transform(cubic.Start), Transform(cubic.FirstControlPoint),
                        Transform(cubic.SecondControlPoint), Transform(cubic.End)));
                else if (segment is SvgArcSegment arc)
                    newPath.Add(new SvgArcSegment(Transform(arc.Start), (float)(arc.RadiusX * d).Truncate(),
                        (float)(arc.RadiusY * d).Truncate(), arc.Angle, arc.Size, arc.Sweep, Transform(arc.End)));
                else if (segment is SvgClosePathSegment)
                    newPath.Add(new SvgClosePathSegment());
            }

            // Mount the inverse matrix
            transform = new DoubleMatrix(1 / d, 0, 0, 1 / d, center.X, center.Y);
            return newPath;

            System.Drawing.PointF Transform(System.Drawing.PointF point)
            {
                var pt = (point.ToDouble2() * d - center * d).Truncate() + dm;
                return new System.Drawing.PointF((float)pt.X, (float)pt.Y);
            }
        }
    }
}
