using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;
using static PathRenderingLab.GeometricUtils;

namespace PathRenderingLab.PathCompiler
{
    public partial struct Curve
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
            else if (c2.Type == CurveType.Line)
                return Intersections(c2, c1).Select(p => p.Flip());
            else
            {
                // Check for circles
                bool IsCircle(Curve c) => c.Type == CurveType.EllipticArc && RoughlyEquals(c.Radii.X, c.Radii.Y);
                if (IsCircle(c1) && IsCircle(c2)) return CircleCircleIntersections(c1, c2);
                return GeneralCurveIntersections(c1, c2);
            }
        }

        public static IEnumerable<RootPair> LineLineIntersections(Curve l1, Curve l2)
        {
            // Check common endpoints first
            bool a1a2 = RoughlyEquals(l1.A, l2.A);
            bool a1b2 = RoughlyEquals(l1.A, l2.B);
            bool b1a2 = RoughlyEquals(l1.B, l2.A);
            bool b1b2 = RoughlyEquals(l1.B, l2.B);

            // Equal lines:
            if (a1a2 && b1b2) return new[] { new RootPair(0f, 0f), new RootPair(1f, 1f) };
            // Inverse lines
            else if (a1b2 && b1a2) return new[] { new RootPair(0f, 1f), new RootPair(1f, 0f) };
            // Common endpoints
            else if (a1a2) return new[] { new RootPair(0f, 0f) };
            else if (a1b2) return new[] { new RootPair(0f, 1f) };
            else if (b1a2) return new[] { new RootPair(1f, 0f) };
            else if (b1b2) return new[] { new RootPair(1f, 1f) };
            // All other cases
            else
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
                if (RoughlyZero(kk))
                {
                    var rs = rr.Dot(ss) > 0 ? (rr + ss).Normalized : (rr - ss).Normalized;

                    // Check if they are collinear
                    if (RoughlyZero((q - p).Cross(rs)))
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
                    else return Enumerable.Empty<RootPair>();
                }
                // If they don't, calculate t and u
                else
                {
                    var t = (q - p).Cross(s) / k;
                    var u = (q - p).Cross(r) / k;

                    // If both lie at the interval [0, 1], then:
                    if (t >= 0 && t <= 1 && u >= 0 && u <= 1)
                        return new[] { new RootPair(t, u) };
                    // Else, no intersection
                    else return Enumerable.Empty<RootPair>();
                }
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
            bool small1 = FinelyZero(c1.BoundingBox.Width) && FinelyZero(c1.BoundingBox.Height);
            bool small2 = FinelyZero(c2.BoundingBox.Width) && FinelyZero(c2.BoundingBox.Height);

            // break at a specified depth
            if (small1 && small2)
            {
                if (t1l == 0f && t2l == 0f && FinelyEquals(c1.At(0f), c2.At(0f)))
                    return new[] { new RootPair(0f, 0f) };

                if (t1l == 0f && t2r == 1f && FinelyEquals(c1.At(0f), c2.At(1f)))
                    return new[] { new RootPair(0f, 1f) };

                if (t1r == 1f && t2l == 0f && FinelyEquals(c1.At(1f), c2.At(0f)))
                    return new[] { new RootPair(1f, 0f) };

                if (t1r == 1f && t2r == 1f && FinelyEquals(c1.At(1f), c2.At(1f)))
                    return new[] { new RootPair(1f, 1f) };

                return new[] { new RootPair((t1l + t1r) / 2, (t2l + t2r) / 2) };
            }

            return GeneralCurveIntersectionsInternal(c1, c2, t1l, t1r, t2l, t2r, depth, small1, small2);
        }

        static IEnumerable<RootPair> GeneralCurveIntersectionsInternal(Curve c1, Curve c2,
            double t1l, double t1r, double t2l, double t2r, int depth, bool small1, bool small2)
        {
            double t1m = (t1l + t1r) / 2;
            double t2m = (t2l + t2r) / 2;

            // Find both intersection curve pairs
            var c1l = c1.Subcurve(0f, 0.5f);
            var c1r = c1.Subcurve(0.5f, 1f);
            var c2l = c2.Subcurve(0f, 0.5f);
            var c2r = c2.Subcurve(0.5f, 1f);

            if (small1)
            {
                if (c1.BoundingBox.Intersects(c2l.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1, c2l, t1l, t1r, t2l, t2m, depth + 1)) yield return r;

                if (c1.BoundingBox.Intersects(c2r.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1, c2r, t1l, t1r, t2m, t2r, depth + 1)) yield return r;
            }
            else if (small2)
            {
                if (c1l.BoundingBox.Intersects(c2.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1l, c2, t1l, t1m, t2l, t2r, depth + 1)) yield return r;

                if (c1r.BoundingBox.Intersects(c2.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1r, c2, t1m, t1r, t2l, t2r, depth + 1)) yield return r;
            }
            else
            {
                if (c1l.BoundingBox.Intersects(c2l.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1l, c2l, t1l, t1m, t2l, t2m, depth + 1)) yield return r;

                if (c1l.BoundingBox.Intersects(c2r.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1l, c2r, t1l, t1m, t2m, t2r, depth + 1)) yield return r;

                if (c1r.BoundingBox.Intersects(c2l.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1r, c2l, t1m, t1r, t2l, t2m, depth + 1)) yield return r;

                if (c1r.BoundingBox.Intersects(c2r.BoundingBox))
                    foreach (var r in GeneralCurveIntersections(c1r, c2r, t1m, t1r, t2m, t2r, depth + 1)) yield return r;
            }
        }

        public const double SubdivEpsilon = 1d / 512;

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
    }
}
