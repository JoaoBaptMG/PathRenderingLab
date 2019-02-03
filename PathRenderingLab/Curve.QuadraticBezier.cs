using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab
{

    public partial struct Curve
    {
        public static Curve QuadraticBezier(Double2 a, Double2 b, Double2 c) => new Curve(CurveType.QuadraticBezier, a, b, c);
        private Curve QuadraticBezier_Reverse => QuadraticBezier(C, B, A);

        private Double2 QuadraticBezier_At(double t)
        {
            var ct = 1 - t;
            return A * ct * ct + 2 * B * ct * t + C * t * t;
        }

        private Curve QuadraticBezier_Derivative => Line(2 * (B - A), 2 * (C - B));

        private Curve QuadraticBezier_Subcurve(double l, double r)
        {
            // The endpoints
            var a = QuadraticBezier_At(l);
            var c = QuadraticBezier_At(r);

            // Calculate the derivatives
            var dd = QuadraticBezier_Derivative;
            var d1 = dd.Line_At(l) * (r - l);
            var d2 = dd.Line_At(r) * (r - l);

            // Two estimations for the midpoint
            var b1 = d1 / 2 + a;
            var b2 = c - d2 / 2;

            return QuadraticBezier(a, (b1 + b2) / 2, c);
        }

        private DoubleRectangle QuadraticBezier_BoundingBox
        {
            get
            {
                // Get derivative's x and y roots
                var d = QuadraticBezier_Derivative;
                var tx = d.A.X / (d.A.X - d.B.X);
                var ty = d.A.Y / (d.A.Y - d.B.Y);

                var tests = new[] { 0f, 1f, tx, ty }.Where(GeometricUtils.Inside01);

                double x1 = double.PositiveInfinity, x2 = double.NegativeInfinity;
                double y1 = double.PositiveInfinity, y2 = double.NegativeInfinity;

                foreach (var t in tests)
                {
                    var p = QuadraticBezier_At(t);
                    if (x1 > p.X) x1 = p.X;
                    if (x2 < p.X) x2 = p.X;
                    if (y1 > p.Y) y1 = p.Y;
                    if (y2 < p.Y) y2 = p.Y;
                }

                return new DoubleRectangle(x1, y1, x2 - x1, y2 - y1);
            }
        }

        private double QuadraticBezier_Winding => (2 * A.Cross(B) + 2 * B.Cross(C) + A.Cross(C)) / 3;
        private bool QuadraticBezier_IsDegenerate => DoubleUtils.RoughlyEquals(A, B) && DoubleUtils.RoughlyEquals(B, C);

        private OuterAngles QuadraticBezier_OuterAngles
        {
            get
            {
                var dv1 = B - A;
                var dv2 = C - B;

                // If dv1 is zero, the following angles will fall apart, so we take the limit
                if (DoubleUtils.RoughlyZero(dv1)) return QuadraticBezier_Derivative.Line_OuterAngles;

                double dAngle = dv1.Cross(dv2 - dv1) / dv1.LengthSquared;
                double ddAngle = -2 * dv1.Dot(dv2 - dv1) * dAngle / dv1.LengthSquared;
                return new OuterAngles(dv1.Angle, dAngle, ddAngle);
            }
        }

        private Double2 QuadraticBezier_EntryTangent => (B - A).Normalized;
        private Double2 QuadraticBezier_ExitTangent => (C - B).Normalized;

        private string QuadraticBezier_ToString() => $"Q({A.X} {A.Y})-({B.X} {B.Y})-({C.X} {C.Y})";

        private IEnumerable<Curve> QuadraticBezier_Simplify()
        {
            // If the quadratic bezier is roughly a line, treat it as a line
            if (DoubleUtils.RoughlyZero((C - B).Normalized.Cross((B - A).Normalized)))
            {
                // Find the maximum point
                var d = QuadraticBezier_Derivative;
                var tm = d.A.X / (d.A.X - d.B.X);
                // if both are equal, we will have +inf or -inf both will be discarded

                // If the maximum point is outside the range
                if (tm < 0 || tm > 1) yield return Line(A, C);
                else
                {
                    var mi = QuadraticBezier_At(tm);
                    yield return Line(A, mi);
                    yield return Line(mi, C);
                }
            }
            // Not a degenerate curve
            else yield return this;
        }

        private double QuadraticBezier_NearestPointTo(Double2 p)
        {
            // Canonical form (plus the P)
            var c2 = A - 2 * B + C;
            var c1 = 2 * (B - A);
            var c0 = A - p;

            // Derivative's canonical form
            var d = QuadraticBezier_Derivative;
            var d1 = d.B - d.A;
            var d0 = d.A;

            // Build the dot-polynomial
            var k3 = c2.Dot(d1);
            var k2 = c2.Dot(d0) + c1.Dot(d1);
            var k1 = c1.Dot(d0) + c0.Dot(d1);
            var k0 = c0.Dot(d0);

            double minRoot = 0;
            var roots = Equations.SolveCubic(k3, k2, k1, k0);

            // For each root, test it
            foreach (var root in roots)
            {
                // Discard roots outside the curve
                if (root < 0 || root > 1) continue;

                // Assign minimum roots
                if (QuadraticBezier_At(minRoot).DistanceSquaredTo(p) > QuadraticBezier_At(root).DistanceSquaredTo(p))
                    minRoot = root;
            }

            // Check the 1 root
            if (QuadraticBezier_At(minRoot).DistanceSquaredTo(p) > C.DistanceSquaredTo(p))
                minRoot = 1;

            return minRoot;
        }

        private CurveVertex[] QuadraticBezier_CurveVertices()
        {
            // Apply the Loop-Blinn method to get the texture coordinates
            // Available on https://www.microsoft.com/en-us/research/wp-content/uploads/2005/01/p1000-loop.pdf
            // The simplest case
            double sign = IsConvex ? 1 : -1;
            return new[]
            {
                new CurveVertex(A, new Double4(0.0, 0.0, 1.0, sign)),
                new CurveVertex(B, new Double4(0.5, 0.0, 1.0, sign)),
                new CurveVertex(C, new Double4(1.0, 1.0, 1.0, sign))
            };
        }
    }
}