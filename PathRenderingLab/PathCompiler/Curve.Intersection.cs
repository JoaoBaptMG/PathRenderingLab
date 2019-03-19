using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;
using static PathRenderingLab.GeometricUtils;

namespace PathRenderingLab.PathCompiler
{
    public partial class Curve
    {
        public static IEnumerable<RootPair> Intersections(Curve c1, Curve c2)
        {
            if (c1.Type == CurveType.Line && c2.Type == CurveType.Line)
                return LineLineIntersections(c1, c2);
            return GeneralCurveIntersections(c1, c2);
        }

        static IEnumerable<RootPair> LineLineIntersections(Curve l1, Curve l2)
        {
            // Check if both lines subdivide
            var p = l1.A;
            var q = l2.A;
            var r = l1.B - l1.A;
            var s = l2.B - l2.A;

            var rr = r.Normalized;
            var ss = s.Normalized;

            // Intersection measure
            var k = r.Cross(s);
            var kk = rr.Cross(ss);

            // If they have the same direction
            if (RoughlyZeroSquared(kk))
            {
                var rs = rr.Dot(ss) > 0 ? (rr + ss).Normalized : (rr - ss).Normalized;

                // Check if they are collinear
                if (RoughlyZeroSquared((q - p).Cross(rs)))
                {
                    var tab0 = (q - p).Dot(r) / r.LengthSquared;
                    var tab1 = tab0 + s.Dot(r) / r.LengthSquared;

                    var tba0 = (p - q).Dot(s) / s.LengthSquared;
                    var tba1 = tba0 + r.Dot(s) / s.LengthSquared;

                    // If there is no intersection
                    if (1 <= Math.Min(tab0, tab1) || 0 >= Math.Max(tab0, tab1)) return Enumerable.Empty<RootPair>();
                    // Assemble the lines accordingly (tedious cases...)
                    else
                    {
                        if (0 <= tab0 && tab0 <= 1) // l1.A -- l2.A -- l1.B, with l2.B elsewhere
                        {
                            if (tab1 > 1) // l2.B to the right of l1
                                return new[] { new RootPair(tab0, 0f), new RootPair(1f, tba1) };
                            else if (tab1 < 0) // l2.B to the left of l1
                                return new[] { new RootPair(tab0, 0f), new RootPair(0f, tba0) };
                            else // l2 inside l1
                                return new[] { new RootPair(tab0, 0f), new RootPair(tab1, 1f) };
                        }
                        else if (0 <= tab1 && tab1 <= 1) // l1.A -- l2.B -- l1.B, with l2.A elsewhere
                        {
                            if (tab0 < 0) // l2.A to the left of l1
                                return new[] { new RootPair(0f, tba0), new RootPair(tab1, 1f) };
                            else // l2.A to the right of l1
                                return new[] { new RootPair(1f, tba1), new RootPair(tab1, 1f) };
                        }
                        else // l1 inside l2
                            return new[] { new RootPair(0f, tba0), new RootPair(1f, tba1) };
                    }
                }
                else return new RootPair[0];
            }
            // If they don't, calculate t and u
            else
            {
                var t = (q - p).Cross(s) / k;
                var u = (q - p).Cross(r) / k;

                // If both lie at the interval [0, 1], then:
                if (Inside01(t) && Inside01(u)) return new[] { new RootPair(t, u) };
                // Else, no intersection
                else return new RootPair[0];
            }
        }

        static IEnumerable<RootPair> GeneralCurveIntersections(Curve c1, Curve c2)
        {
            // First, pick the curves' bounding boxes
            var bb1 = c1.BoundingBox;
            var bb2 = c2.BoundingBox;

            // If they do not intersect, the curves also don't
            if (!bb1.Intersects(bb2)) return new RootPair[0];

            // Pick the truncated version and extend its sizes
            var bb = bb1.Intersection(bb2).Truncate();
            bb.Width = Epsilon * NextPowerOfTwo(bb.Width / Epsilon);
            bb.Height = Epsilon * NextPowerOfTwo(bb.Height / Epsilon);

            // Pass it to the actual recursion
            return GeneralCurveIntersectionsInternal(c1, c2, searchingArea: bb);
        }

        static IEnumerable<RootPair> GeneralCurveIntersectionsInternal(Curve c1, Curve c2, DoubleRectangle searchingArea)
        {

        }
    }
}
