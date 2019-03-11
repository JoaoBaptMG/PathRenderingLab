using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;

namespace PathRenderingLab.PathCompiler
{

    public partial class Curve
    {
        public static Curve CubicBezier(Double2 a, Double2 b, Double2 c, Double2 d)
            => new Curve(CurveType.CubicBezier, a, b, c, d);
        private Curve CubicBezier_Reverse => CubicBezier(D, C, B, A);

        private Double2 CubicBezier_At(double t)
        {
            var ct = 1 - t;
            return A * ct * ct * ct + 3 * B * ct * ct * t + 3 * C * ct * t * t + D * t * t * t;
        }

        private Curve CubicBezier_Derivative => QuadraticBezier(3 * (B - A), 3 * (C - B), 3 * (D - C));

        private Curve CubicBezier_Subcurve(double l, double r)
        {
            // The endpoints
            var a = CubicBezier_At(l);
            var d = CubicBezier_At(r);

            // Calculate the derivatives
            var dd = CubicBezier_Derivative;
            var d1 = dd.QuadraticBezier_At(l) * (r - l);
            var d2 = dd.QuadraticBezier_At(r) * (r - l);

            return CubicBezier(a, d1 / 3 + a, d - d2 / 3, d);
        }

        private DoubleRectangle CubicBezier_BoundingBox
        {
            get
            {
                // Get derivative's x and y roots
                var d = CubicBezier_Derivative;
                var tx = Equations.SolveQuadratic(d.A.X - 2 * d.B.X + d.C.X, 2 * (d.B.X - d.A.X), d.A.X);
                var ty = Equations.SolveQuadratic(d.A.Y - 2 * d.B.Y + d.C.Y, 2 * (d.B.Y - d.A.Y), d.A.Y);

                var tests = new[] { 0d, 1d }.Concat(tx).Concat(ty).Where(GeometricUtils.Inside01);

                double x1 = double.PositiveInfinity, x2 = double.NegativeInfinity;
                double y1 = double.PositiveInfinity, y2 = double.NegativeInfinity;

                foreach (var t in tests)
                {
                    var p = CubicBezier_At(t);
                    if (x1 > p.X) x1 = p.X;
                    if (x2 < p.X) x2 = p.X;
                    if (y1 > p.Y) y1 = p.Y;
                    if (y2 < p.Y) y2 = p.Y;
                }

                return new DoubleRectangle(x1, y1, x2 - x1, y2 - y1);
            }
        }

        private double CubicBezier_Winding => (6 * A.Cross(B) + 3 * A.Cross(C) + A.Cross(D) + 3 * B.Cross(C)
            + 3 * B.Cross(D) + 6 * C.Cross(D)) / 10;

        private bool CubicBezier_IsDegenerate => RoughlyEquals(A, B) &&
            RoughlyEquals(B, C) && RoughlyEquals(C, D);

        private OuterAngles CubicBezier_OuterAngles
        {
            get
            {
                var dv1 = B - A;
                var dv2 = C - B;
                var dv3 = D - C;

                // If dv1 is zero, the following angles will fall apart, so we take the limit
                if (RoughlyZero(dv1)) return CubicBezier_Derivative.QuadraticBezier_OuterAngles;

                double dAngle = 2 * dv1.Cross(dv2 - dv1) / dv1.LengthSquared;
                double ddAngle = (2 * dv1.Cross(dv3 - 2 * dv2 + dv1) -
                    8 * dv1.Dot(dv2 - dv1) * dAngle) / dv1.LengthSquared;
                return new OuterAngles(dv1.Angle, dAngle, ddAngle);
            }
        }

        private Double2 CubicBezier_EntryTangent =>
            RoughlyEquals(B, A) ? CubicBezier_Derivative.QuadraticBezier_ExitTangent : (B - A).Normalized;
        private Double2 CubicBezier_ExitTangent =>
            RoughlyEquals(C, D) ? CubicBezier_Derivative.QuadraticBezier_ExitTangent : (D - C).Normalized;

        private string CubicBezier_ToString() => $"C({A.X} {A.Y})-({B.X} {B.Y})-({C.X} {C.Y})-({D.X} {D.Y})";

        private IEnumerable<Curve> CubicBezier_Simplify(bool split = false)
        {
            // If the Bézier is lines
            if (RoughlyEquals(A, B) && RoughlyEquals(C, D))
                yield return Line((A + B) / 2, (C + D) / 2);
            else if ((RoughlyEquals(A, B) || RoughlyZero((B - A).Normalized.Cross((C - B).Normalized)))
                && (RoughlyEquals(C, D) || RoughlyZero((D - C).Normalized.Cross((C - B).Normalized))))
            {
                var d = Derivative;
                var rx = Equations.SolveQuadratic(d.A.X - 2 * d.B.X + d.C.X, 2 * (d.B.X - d.A.X), d.A.X);

                // Get the tests
                var tests = rx.Concat(new[] { 0d, 1d }).Where(GeometricUtils.Inside01).ToArray();
                Array.Sort(tests);

                // Subdivide the lines
                for (int i = 1; i < tests.Length; i++)
                    yield return Line(CubicBezier_At(tests[i - 1]), CubicBezier_At(tests[i]));
            }
            // If the Bézier should be a quadratic instead
            else if (RoughlyZero(A - 3 * B + 3 * C - D))
            {
                // Estimates
                var b1 = 3 * B - A;
                var b2 = 3 * C - D;

                // Subdivide as if it were a cube
                yield return QuadraticBezier(A, (b1 + b2) / 4, D);
            }
            // Detect loops, cusps and inflection points if required
            else if (split)
            {
                var roots = new List<double>(7) { 0f, 1f };

                // Here we use the method based on https://stackoverflow.com/a/38644407
                var da = B - A;
                var db = C - B;
                var dc = D - C;

                // Check for no loops on cross product
                double ab = da.Cross(db), ac = da.Cross(dc), bc = db.Cross(dc);
                if (ac * ac <= 4 * ab * bc)
                {
                    // The coefficients of the canonical form
                    var c3 = da + dc - 2 * db;

                    // Rotate it beforehand if necessary
                    if (!RoughlyZero(c3.Y))
                    {
                        c3 = c3.NegateY;
                        da = da.RotScale(c3);
                        db = db.RotScale(c3);
                        dc = dc.RotScale(c3);
                        c3 = da + dc - 2 * db;
                    }

                    var c2 = 3 * (db - da);
                    var c1 = 3 * da;

                    // Calculate the coefficients of the loop polynomial
                    var bb = -c1.Y / c2.Y;
                    var s1 = c1.X / c3.X;
                    var s2 = c2.X / c3.X;

                    // Find the roots (that happen to be the loop points)
                    roots.AddRange(Equations.SolveQuadratic(1, -bb, bb * (bb + s2) + s1));
                }

                // The inflection point polynom
                double axby = A.X * B.Y, axcy = A.X * C.Y, axdy = A.X * D.Y;
                double bxay = B.X * A.Y, bxcy = B.X * C.Y, bxdy = B.X * D.Y;
                double cxay = C.X * A.Y, cxby = C.X * B.Y, cxdy = C.X * D.Y;
                double dxay = D.X * A.Y, dxby = D.X * B.Y, dxcy = D.X * C.Y;

                double k2 = axby - 2 * axcy + axdy - bxay + 3 * bxcy - 2 * bxdy + 2 * cxay - 3 * cxby + cxdy - dxay + 2 * dxby - dxcy;
                double k1 = -2 * axby + 3 * axcy - axdy + 2 * bxay - 3 * bxcy + bxdy - 3 * cxay + 3 * cxby + dxay - dxby;
                double k0 = axby - axcy - bxay + bxcy + cxay - cxby;

                // Add the roots of the inflection points
                roots.AddRange(Equations.SolveQuadratic(k2, k1, k0));

                // Sort and the roots and remove duplicates, subdivide the curve
                roots.RemoveAll(r => !GeometricUtils.Inside01(r));
                roots.RemoveDuplicatedValues();

                for (int i = 1; i < roots.Count; i++)
                    yield return CubicBezier_Subcurve(roots[i - 1], roots[i]);
            }
            else yield return this;
        }

        private double CubicBezier_NearestPointTo(Double2 p)
        {
            // Canonical form (plus the P)
            var c3 = -A + 3 * B - 3 * C + D;
            var c2 = 3 * A - 6 * B + 3 * C;
            var c1 = -3 * A + 3 * B;
            var c0 = A - p;

            // Derivative's canonical form
            var d = CubicBezier_Derivative;
            var d2 = d.A - 2 * d.B + d.C;
            var d1 = 2 * (d.B - d.A);
            var d0 = d.A;

            // Build the dot-polynomial
            var k = new[]
            {
                c3.Dot(d2),
                c3.Dot(d1) + c2.Dot(d2),
                c3.Dot(d0) + c2.Dot(d1) + c1.Dot(d2),
                c2.Dot(d0) + c1.Dot(d1) + c0.Dot(d2),
                c1.Dot(d0) + c0.Dot(d1),
                c0.Dot(d0)
            };

            double minRoot = 0;
            var roots = Equations.SolvePolynomial(k);

            // For each root, test it
            foreach (var root in roots)
            {
                // Discard roots outside the curve
                if (root < 0 || root > 1) continue;

                // Assign minimum roots
                if (CubicBezier_At(minRoot).DistanceSquaredTo(p) > CubicBezier_At(root).DistanceSquaredTo(p))
                    minRoot = root;
            }

            // Check the 1 root
            if (CubicBezier_At(minRoot).DistanceSquaredTo(p) > D.DistanceSquaredTo(p))
                minRoot = 1;

            return minRoot;
        }

        private CurveVertex[] CubicBezier_CurveVertices()
        {
            // Apply the Loop-Blinn method to get the texture coordinates
            // Available on https://www.microsoft.com/en-us/research/wp-content/uploads/2005/01/p1000-loop.pdf

            // Use the computations in Chapter 4 of the Loop-Blinn paper
            // The canonical form of the curve
            var c3 = -A + 3 * B - 3 * C + D;
            var c2 = 3 * A - 6 * B + 3 * C;
            var c1 = -3 * A + 3 * B;
            var c0 = A;

            // Again the inflection point polynomials, this time the homogeneous form
            var d3 = c1.Cross(c2);
            var d2 = c3.Cross(c1);
            var d1 = c2.Cross(c3);

            // The texture coordinates in canonical form
            Double4 f0, f1, f2, f3;

            void NegateSigns()
            {
                f0 = f0.NegateX.NegateY;
                f1 = f1.NegateX.NegateY;
                f2 = f2.NegateX.NegateY;
                f3 = f3.NegateX.NegateY;
            }

            // Check for the "true" cubics first
            if (d1 != 0)
            {
                // Discriminant
                var D = 3 * d2 * d2 - 4 * d3 * d1;

                // Serpentine or cusp
                if (D > -double.Epsilon)
                {
                    // Find the roots of the equation
                    var dv = d2 >= 0 ? Math.Sqrt(D / 3) : -Math.Sqrt(D / 3);
                    var q = 0.5 * (d2 + dv);

                    var x1 = q / d1;
                    var x2 = (d3 / 3) / q;

                    var l = Math.Min(x1, x2);
                    var m = Math.Max(x1, x2);

                    f0 = new Double4(l * m, l * l * l, m * m * m, 0);
                    f1 = new Double4(-l - m, -3 * l * l, -3 * m * m, 0);
                    f2 = new Double4(1, 3 * l, 3 * m, 0);
                    f3 = new Double4(0, -1, -1, 0);

                    // Guarantee that the signs are correct
                    if (d1 < 0) NegateSigns();
                }
                // Loop
                else
                {
                    // Find the roots of the equation
                    var dv = d2 >= 0 ? Math.Sqrt(-D) : -Math.Sqrt(-D);
                    var q = 0.5 * (d2 + dv);

                    var x1 = q / d1;
                    var x2 = (d2 * d2 / d1 - d3) / q;

                    var d = Math.Min(x1, x2);
                    var e = Math.Max(x1, x2);

                    f0 = new Double4(d * e, d * d * e, d * e * e, 0);
                    f1 = new Double4(-d - e, -d * d - 2 * e * d, -e * e - 2 * d * e, 0);
                    f2 = new Double4(1, e + 2 * d, d + 2 * e, 0);
                    f3 = new Double4(0, -1, -1, 0);

                    // Guarantee that the signs are correct
                    var h1 = d3 * d1 - d2 * d2;
                    var h2 = d3 * d1 - d2 * d2 + d1 * d2 - d1 * d1;
                    var h = Math.Abs(h1) > Math.Abs(h2) ? h1 : h2;
                    var h12 = d3 * d1 - d2 * d2 + d1 * d2 / 2 - d1 * d1 / 4;
                    if (Math.Abs(h12) > Math.Abs(h)) h = h12;

                    if (d1 * h > 0) NegateSigns();
                }
            }
            // Other kind of cusp
            else if (d2 != 0)
            {
                // A single root
                var l = d3 / (3 * d2);

                f0 = new Double4(l, l * l * l, 1, 0);
                f1 = new Double4(-1, -3 * l * l, 0, 0);
                f2 = new Double4(0, -3 * l, 0, 0);
                f3 = new Double4(0, -1, 0, 0);
            }
            // Degenerate forms for the cubic - first, a quadratic
            else if (d3 != 0)
            {
                f0 = new Double4(0, 0, 1, 0);
                f1 = new Double4(1, 0, 1, 0);
                f2 = new Double4(0, 1, 0, 0);
                f3 = new Double4(0, 0, 0, 0);
            }
            // A line or a point - no triangles
            else return new CurveVertex[0];

            // Put the texture coordinates back into "normal" form
            return new[]
            {
                new CurveVertex(A, f0),
                new CurveVertex(B, f0 + f1 / 3),
                new CurveVertex(C, f0 + (2 * f1 + f2) / 3),
                new CurveVertex(D, f0 + f1 + f2 + f3)
            };
        }
    }
}