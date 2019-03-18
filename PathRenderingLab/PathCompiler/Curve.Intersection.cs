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
            if (c1.Type == CurveType.Line)
            {
                switch (c2.Type)
                {
                    case CurveType.Line: return LineLineIntersections(c1, c2);
                    case CurveType.QuadraticBezier: return LineQuadraticIntersections(c1, c2);
                    case CurveType.CubicBezier: return LineCubicIntersections(c1, c2);
                    case CurveType.EllipticArc: return LineArcIntersections(c1, c2);
                    default: throw new InvalidOperationException("Unknown c2's type.");
                }
            }
            else if (c1.Type == CurveType.QuadraticBezier)
            {
                switch (c2.Type)
                {
                    case CurveType.Line: return LineQuadraticIntersections(c2, c1).Select(p => p.Flip());
                    case CurveType.QuadraticBezier: return GeneralCurveIntersections(c1, c2);
                    case CurveType.CubicBezier: return GeneralCurveIntersections(c1, c2);
                    case CurveType.EllipticArc: return QuadraticArcIntersections(c1, c2);
                    default: throw new InvalidOperationException("Unknown c2's type.");
                }
            }
            else if (c1.Type == CurveType.CubicBezier)
            {
                switch (c2.Type)
                {
                    case CurveType.Line: return LineCubicIntersections(c2, c1).Select(p => p.Flip());
                    case CurveType.QuadraticBezier: return GeneralCurveIntersections(c1, c2);
                    case CurveType.CubicBezier: return GeneralCurveIntersections(c1, c2);
                    case CurveType.EllipticArc: return CubicArcIntersections(c1, c2);
                    default: throw new InvalidOperationException("Unknown c2's type.");
                }
            }
            else if (c1.Type == CurveType.EllipticArc)
            {
                switch (c2.Type)
                {
                    case CurveType.Line: return LineArcIntersections(c2, c1).Select(p => p.Flip());
                    case CurveType.QuadraticBezier: return QuadraticArcIntersections(c2, c1).Select(p => p.Flip());
                    case CurveType.CubicBezier: return CubicArcIntersections(c2, c1).Select(p => p.Flip());
                    case CurveType.EllipticArc:
                        if (RoughlyEquals(c1.Radii.X, c1.Radii.Y) && RoughlyEquals(c2.Radii.X, c2.Radii.Y))
                            return CircleCircleIntersections(c1, c2);
                        return GeneralCurveIntersections(c1, c2);
                    default: throw new InvalidOperationException("Unknown c2's type.");
                }
            }
            else throw new InvalidOperationException("Unknown c1's type.");
        }

        public static IEnumerable<RootPair> LineLineIntersections(Curve l1, Curve l2)
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

        public static IEnumerable<RootPair> LineQuadraticIntersections(Curve l, Curve q)
        {
            // Transform the quadratic Bézier
            var a = l.ProjectiveTransform(q.A);
            var b = l.ProjectiveTransform(q.B);
            var c = l.ProjectiveTransform(q.C);

            // Mount the polynomial
            var c2 = a - 2 * b + c;
            var c1 = 2 * (b - a);
            var c0 = a;

            // Solve the equation and determine the subdivision points
            var roots = Equations.SolveQuadratic(c2.Y, c1.Y, c0.Y);
            var subdivision = roots.Where(Inside01);

            return subdivision.Select(t => new RootPair(c2.X * t * t + c1.X * t + c0.X, t)).Where(p => Inside01(p.A));
        }

        public static IEnumerable<RootPair> LineCubicIntersections(Curve l, Curve c)
        {
            // Transform the cubic Bézier
            var ak = l.ProjectiveTransform(c.A);
            var bk = l.ProjectiveTransform(c.B);
            var ck = l.ProjectiveTransform(c.C);
            var dk = l.ProjectiveTransform(c.D);

            // Mount the polynomial
            var c3 = -ak + 3 * bk - 3 * ck + dk;
            var c2 = 3 * ak - 6 * bk + 3 * ck;
            var c1 = -3 * ak + 3 * bk;
            var c0 = ak;

            // Solve the equation and determine the subdivision points
            var roots = Equations.SolveCubic(c3.Y, c2.Y, c1.Y, c0.Y);
            var subdivision = roots.Where(Inside01);
            return subdivision.Select(t => new RootPair(c3.X * t * t * t + c2.X * t * t + c1.X * t + c0.X, t))
                .Where(p => Inside01(p.A));
        }

        public static IEnumerable<RootPair> LineArcIntersections(Curve l, Curve a)
        {
            // Transform the arc
            var center = l.ProjectiveTransform(a.Center);
            var transformRot = l.ProjectiveTransformDirection(a.ComplexRot);
            // The rotation will actually be a rotoscale, but it suffices our needs

            // The maxima in y
            var ky = Math.Sqrt(transformRot.X * transformRot.X * a.Radii.Y * a.Radii.Y
                + transformRot.Y * transformRot.Y * a.Radii.X * a.Radii.X);

            // Check if there are really roots
            if (center.Y > ky || center.Y < -ky) return Enumerable.Empty<RootPair>();
            else
            {
                RootPair MakeRoot(double t) => new RootPair(center.X + a.Radii.X * transformRot.X * Math.Cos(t)
                    - a.Radii.Y * transformRot.Y * Math.Sin(t), a.AngleToParameter(t));

                // The two roots present
                var rotAngle = Math.Atan2(-a.Radii.X * transformRot.Y, a.Radii.Y * transformRot.X);
                var correctAngle = -Math.Asin(center.Y / ky);
                return new[] { MakeRoot(rotAngle + correctAngle), MakeRoot(rotAngle + DoubleUtils.Pi - correctAngle) }
                    .Where(p => Inside01(p.A) && Inside01(p.B));
            }
        }

        static bool FinelyZero(double x) => RoughlyZero(128 * x);
        static bool FinelyEquals(Double2 a, Double2 b) => RoughlyEquals(128 * a, 128 * b);

        public static IEnumerable<RootPair> GeneralCurveIntersections(Curve c1, Curve c2,
            double t1l = 0f, double t1r = 1f, double t2l = 0f, double t2r = 1f, int depth = 0)
        {
            // Check first for extrema

            if (RoughlyEquals(c1.At(t1l), c2.At(t2l)))
                yield return new RootPair(t1l, t2l);
            if (RoughlyEquals(c1.At(t1l), c2.At(t2r)))
                yield return new RootPair(t1l, t2r);
            if (RoughlyEquals(c1.At(t1r), c2.At(t2l)))
                yield return new RootPair(t1r, t2l);
            if (RoughlyEquals(c1.At(t1r), c2.At(t2r)))
                yield return new RootPair(t1r, t2r);

            var c1s = c1.Subcurve(t1l, t1r);
            var c2s = c2.Subcurve(t2l, t2r);

            // Flags to whether the curves are degenerate
            bool deg1 = c1s.IsDegenerate;
            bool deg2 = c2s.IsDegenerate;

            // The midpoints
            var t1m = (t1l + t1r) / 2;
            var t2m = (t2l + t2r) / 2;

            // If both are, time to bail out
            if (deg1 && deg2)
                yield return new RootPair(t1m, t2m);
            // Else, we will check the enclosing polygons
            else
            {
                var p1 = c1s.EnclosingPolygon;
                var p2 = c2s.EnclosingPolygon;

                if (deg2)
                {
                    var p1l = c1.Subcurve(t1l, t1m).EnclosingPolygon;
                    var p1r = c1.Subcurve(t1m, t1r).EnclosingPolygon;

                    if (PolygonsOverlap(p1l, p2, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1m, t2l, t2r, depth + 1)) yield return r;
                    if (PolygonsOverlap(p1r, p2, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1m, t1r, t2l, t2r, depth + 1)) yield return r;
                }
                else if (deg1)
                {
                    var p2l = c2.Subcurve(t2l, t2m).EnclosingPolygon;
                    var p2r = c2.Subcurve(t2m, t2r).EnclosingPolygon;

                    if (PolygonsOverlap(p1, p2l, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1r, t2l, t2m, depth + 1)) yield return r;
                    if (PolygonsOverlap(p1, p2r, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1r, t2m, t2r, depth + 1)) yield return r;
                }
                else
                {
                    var p1l = c1.Subcurve(t1l, t1m).EnclosingPolygon;
                    var p1r = c1.Subcurve(t1m, t1r).EnclosingPolygon;

                    var p2l = c2.Subcurve(t2l, t2m).EnclosingPolygon;
                    var p2r = c2.Subcurve(t2m, t2r).EnclosingPolygon;

                    if (PolygonsOverlap(p1l, p2l, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1m, t2l, t2m, depth + 1)) yield return r;
                    if (PolygonsOverlap(p1l, p2r, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1m, t2m, t2r, depth + 1)) yield return r;
                    if (PolygonsOverlap(p1r, p2l, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1m, t1r, t2l, t2m, depth + 1)) yield return r;
                    if (PolygonsOverlap(p1r, p2r, true))
                        foreach (var r in GeneralCurveIntersections(c1, c2, t1m, t1r, t2m, t2r, depth + 1)) yield return r;
                }
            }
        }

        public static IEnumerable<RootPair> CircleCircleIntersections(Curve c1, Curve c2)
        {
            // Detect their geometric intersections
            var points = CircleIntersection(c1.Center, c1.Radii.X, c2.Center, c2.Radii.X);

            // For each point, generate the angle
            foreach (var p in points)
            {
                var a1 = c1.ComplexRot.AngleBetween(p - c1.Center);
                var a2 = c2.ComplexRot.AngleBetween(p - c2.Center);

                // And get their parameters
                var t1 = c1.AngleToParameter(a1);
                if (!Inside01(t1)) continue;

                var t2 = c2.AngleToParameter(a2);
                if (!Inside01(t2)) continue;

                // Return the intersection
                yield return new RootPair(t1, t2);
            }
        }

        public static IEnumerable<RootPair> QuadraticArcIntersections(Curve q, Curve arc)
        {
            // Scaled dot product
            var rx = arc.Radii.X;
            var ry = arc.Radii.Y;
            double ScaledDot(Double2 m, Double2 n) => m.X * n.X / rx / rx + m.Y * n.Y / ry / ry;

            // Transform the cubic Bézier
            var center = arc.Center;
            var vrot = arc.ComplexRot.NegateY;
            Double2 Untransform(Double2 x) => (x - center).RotScale(vrot);

            var a = Untransform(q.A);
            var b = Untransform(q.B);
            var c = Untransform(q.C);

            // Mount the coefficients
            var c2 = a - 2 * b + c;
            var c1 = 2 * (b - a);
            var c0 = a;

            // Mount the polynomial
            var k = new[]
            {
                ScaledDot(c2, c2),
                2 * ScaledDot(c2, c1),
                ScaledDot(c1, c1) + 2 * ScaledDot(c2, c0),
                2 * ScaledDot(c1, c0),
                ScaledDot(c0, c0) - 1
            };

            // And solve it
            var roots = Equations.SolvePolynomial(k).Where(Inside01);

            // Mount root pairs with it
            var rootPairs = roots.Select(t => new RootPair(t, arc.AngleToParameter(Untransform(q.At(t)).Angle)));

            // Return the sane ones
            return rootPairs.Where(p => Inside01(p.B));
        }

        public static IEnumerable<RootPair> CubicArcIntersections(Curve c, Curve arc)
        {
            // Scaled dot product
            var rx = arc.Radii.X;
            var ry = arc.Radii.Y;
            double ScaledDot(Double2 m, Double2 n) => m.X * n.X / rx / rx + m.Y * n.Y / ry / ry;

            // Transform the cubic Bézier
            var center = arc.Center;
            var vrot = arc.ComplexRot.NegateY;
            Double2 Untransform(Double2 x) => (x - center).RotScale(vrot);

            var ak = Untransform(c.A);
            var bk = Untransform(c.B);
            var ck = Untransform(c.C);
            var dk = Untransform(c.D);

            // Mount the coefficients
            var c3 = -ak + 3 * bk - 3 * ck + dk;
            var c2 = 3 * ak - 6 * bk + 3 * ck;
            var c1 = -3 * ak + 3 * bk;
            var c0 = ak;

            // Mount the polynomial
            var k = new[]
            {
                ScaledDot(c3, c3),
                2 * ScaledDot(c3, c2),
                ScaledDot(c2, c2) + 2 * ScaledDot(c3, c1),
                2 * ScaledDot(c3, c0) + 2 * ScaledDot(c2, c1),
                ScaledDot(c1, c1) + 2 * ScaledDot(c2, c0),
                2 * ScaledDot(c1, c0),
                ScaledDot(c0, c0) - 1
            };

            // And solve it
            var roots = Equations.SolvePolynomial(k).Where(Inside01);

            // Mount root pairs with it
            var rootPairs = roots.Select(t => new RootPair(t, arc.AngleToParameter(Untransform(c.At(t)).Angle)));

            // Return the sane ones
            return rootPairs.Where(p => Inside01(p.B));
        }

        public static void RemoveSimilarRoots(SortedDictionary<double, Double2> rootSet)
        {
            // Identify similar clusters on the rootSet
            var clusters = new List<SortedDictionary<double, Double2>>();
            var curCluster = new SortedDictionary<double, Double2>();

            var prevPair = new KeyValuePair<double, Double2>(double.NaN, new Double2());
            foreach (var kvp in rootSet)
            {
                if (!double.IsNaN(prevPair.Key))
                    // Compare the keys
                    if (RoughComparerSquared.Compare(prevPair.Key, kvp.Key) != 0)
                    {
                        clusters.Add(curCluster);
                        curCluster = new SortedDictionary<double, Double2>();
                    }

                curCluster.Add(kvp.Key, kvp.Value);
                prevPair = kvp;
            }

            // Add the last cluster
            clusters.Add(curCluster);

            // Now, clear the set and, for each cluster, add a single root
            rootSet.Clear();

            foreach (var cluster in clusters)
            {
                // If the cluster has 0 or 1, add the 0 or 1
                if (cluster.ContainsKey(0)) rootSet[0] = cluster[0];
                else if (cluster.ContainsKey(1)) rootSet[1] = cluster[1];

                // Else, pick the average
                else
                {
                    int n = cluster.Count;
                    var value = new Double2(0, 0);
                    double key = 0;

                    foreach (var kvp in cluster)
                    {
                        key += kvp.Key;
                        value += kvp.Value;
                    }

                    rootSet[key / n] = value / n;
                }
            }
        }
    }
}
