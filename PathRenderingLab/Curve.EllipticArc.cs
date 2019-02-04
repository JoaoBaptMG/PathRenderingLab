using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;

namespace PathRenderingLab
{

    public partial struct Curve
    {
        public Double2 Center => A;
        public Double2 Radii => B;
        public Double2 ComplexRot => C;
        public double FirstAngle => D.X;
        public double DeltaAngle => D.Y;

        private static Curve EllipticArcInternal(Double2 center, Double2 radii, Double2 rot, double t1, double dt)
            => new Curve(CurveType.EllipticArc, center, radii, rot, new Double2(t1, dt));

        /// <summary>
        /// Retrieve an elipse in its center-radius form using parameters from the SVG specification
        /// </summary>
        /// <param name="cur">Current point</param>
        /// <param name="radii">The ellipse radius</param>
        /// <param name="xRotation">The rotation of the ellipse in relation to the x-axis</param>
        /// <param name="largeArc">Wheter the large arc should be selected or not</param>
        /// <param name="sweep">Sweep direction (false for clockwise, true for counterclockwise)</param>
        /// <param name="target">The target point</param>
        /// <returns></returns>
        public static Curve EllipticArc(Double2 cur, Double2 radii, double xRotation, bool largeArc, bool sweep, Double2 target)
        {
            // The algorithm used here is presented on this link: https://svgwg.org/svg2-draft/implnote.html
            var xpun = (cur - target) / 2;
            var xpr = xpun.Rotate(-xRotation);

            // Guarantee that the radii are positive
            radii.X = Math.Abs(radii.X); radii.Y = Math.Abs(radii.Y);

            // Guarantee that the radius are large enough
            var rr = radii.X * radii.X * xpr.Y * xpr.Y + radii.Y * radii.Y * xpr.X * xpr.X;
            var r2 = radii.X * radii.X * radii.Y * radii.Y;
            double skr;
            if (rr > r2)
            {
                radii *= Math.Sqrt(rr / r2);
                skr = 0;
            }
            else skr = Math.Sqrt((r2 - rr) / rr);

            var cpr = new Double2(skr * radii.X * xpr.Y / radii.Y, -skr * radii.Y * xpr.X / radii.X);
            if (largeArc == sweep) cpr = -cpr;

            // Calculate the center
            var cpun = cpr.Rotate(xRotation) + (target + cur) / 2;

            // Calculate the angle
            var t1 = Math.Atan2(radii.X * (xpr.Y - cpr.Y), radii.Y * (xpr.X - cpr.X));
            var t2 = Math.Atan2(radii.X * (-xpr.Y - cpr.Y), radii.Y * (-xpr.X - cpr.X));

            var dt = t2 - t1;
            if (!sweep && dt > 0) dt -= TwoPi;
            else if (sweep && dt < 0) dt += TwoPi;

            return EllipticArcInternal(cpun, radii, Double2.FromAngle(xRotation), t1, dt);
        }

        public static Curve CorrectCircle(Double2 center, double radius, Double2 v1, Double2 v2, bool counterclockwise)
            => EllipticArcInternal(center, new Double2(radius, radius), v1.Normalized, 0,
                (counterclockwise ? 1 : -1) * Math.Atan2(v1.Cross(v2), v1.Dot(v2)).WrapAngle360());

        public Curve EnlargeRadii(double r)
        {
            if (Type != CurveType.EllipticArc) throw new InvalidOperationException("You can only enlarge the radii of an elliptic arc!");
            if (!RoughlyEquals(Radii.X, Radii.Y)) throw new InvalidOperationException("The ellipse must be a circle!");
            return EllipticArcInternal(Center, Radii + new Double2(r, r), ComplexRot, FirstAngle, DeltaAngle);
        }

        private Double2 DeltaAtInternal(double t)
        {
            var theta = FirstAngle + t * DeltaAngle;
            return new Double2(Radii.X * Math.Cos(theta), Radii.Y * Math.Sin(theta));
        }

        /// <summary>
        /// Evaluates the unrotated arc delta at point t
        /// </summary>
        /// <param name="t">The interpolation value</param>
        /// <returns>The point requested</returns>
        public Double2 DeltaAt(double t)
        {
            if (Type != CurveType.EllipticArc) throw new InvalidOperationException("DeltaAt should only be called at elliptic arcs");
            return DeltaAtInternal(t);
        }

        private Double2 EllipticArc_At(double t) => LocalSpaceToGlobal(DeltaAtInternal(t));

        private Double2 LocalSpaceToGlobal(Double2 p) => Center + ComplexRot.RotScale(p);

        private Curve EllipticArc_Derivative => EllipticArcInternal(Double2.Zero, Math.Abs(DeltaAngle) * Radii,
            ComplexRot, (FirstAngle + Math.Sign(DeltaAngle) * Pi_2).WrapAngle(), DeltaAngle);

        private Curve EllipticArc_Subcurve(double l, double r) => EllipticArcInternal(Center, Radii, ComplexRot,
            FirstAngle + l * DeltaAngle, (r - l) * DeltaAngle);

        public double LesserAngle => Math.Min(FirstAngle, FirstAngle + DeltaAngle);
        public double GreaterAngle => Math.Max(FirstAngle, FirstAngle + DeltaAngle);

        // Check the parameter said angle is contained in
        public double AngleToParameter(double theta)
        {
            if (Type != CurveType.EllipticArc) throw new InvalidOperationException("This query is only relevant on an elliptic arc");
            theta = theta.WrapAngle();

            // Test the angle and up to two double-turns before and after
            for (int i = -2; i <= 2; i++)
            {
                var cand = theta + i * TwoPi;
                if (LesserAngle <= cand && cand <= GreaterAngle)
                    return (cand - FirstAngle) / DeltaAngle;
            }

            return double.PositiveInfinity;
        }

        private Curve EllipticArc_Reverse => EllipticArcInternal(Center, Radii, ComplexRot, FirstAngle + DeltaAngle, -DeltaAngle);

        private DoubleRectangle EllipticArc_BoundingBox
        {
            get
            {
                var int1 = LesserAngle;
                var int2 = GreaterAngle;

                bool AngleInsideInterval(double angle) =>
                    (int1 <= angle && angle <= int2) || (int1 <= angle + TwoPi && angle + TwoPi <= int2);

                var points = new List<Double2>(6) { EllipticArc_At(0), EllipticArc_At(1) };

                var ax2 = ComplexRot.X * ComplexRot.X;
                var ay2 = ComplexRot.Y * ComplexRot.Y;
                var rx2 = Radii.X * Radii.X;
                var ry2 = Radii.Y * Radii.Y;

                var twistFactor = ComplexRot.X * ComplexRot.Y * (rx2 - ry2);

                var kx = Math.Sqrt(ax2 * rx2 + ay2 * ry2);
                var ky = Math.Sqrt(ax2 * ry2 + ay2 * rx2);

                var vpx = new Double2(kx, twistFactor / kx);
                var vpy = new Double2(twistFactor / ky, ky);

                var angleX = Math.Atan2(-Radii.Y * ComplexRot.Y, Radii.X * ComplexRot.X);
                var angleY = Math.Atan2(Radii.Y * ComplexRot.X, Radii.X * ComplexRot.Y);

                if (AngleInsideInterval(angleX)) points.Add(Center + vpx);
                if (AngleInsideInterval(angleX + Pi)) points.Add(Center - vpx);
                if (AngleInsideInterval(angleY)) points.Add(Center + vpy);
                if (AngleInsideInterval(angleY + Pi)) points.Add(Center - vpy);

                double x1 = double.PositiveInfinity, x2 = double.NegativeInfinity;
                double y1 = double.PositiveInfinity, y2 = double.NegativeInfinity;

                foreach (var p in points)
                {
                    if (x1 > p.X) x1 = p.X;
                    if (x2 < p.X) x2 = p.X;
                    if (y1 > p.Y) y1 = p.Y;
                    if (y2 < p.Y) y2 = p.Y;
                }

                return new DoubleRectangle(x1, y1, x2 - x1, y2 - y1);
            }
        }

        private double EllipticArc_Winding
        {
            get
            {
                var p1 = DeltaAtInternal(1);
                var p0 = DeltaAtInternal(0);

                return DeltaAngle * Radii.X * Radii.Y + Center.Cross(ComplexRot.RotScale(p1 - p0));
            }
        }

        private OuterAngles EllipticArc_OuterAngles
        {
            get
            {
                var pr = DeltaAtInternal(0);

                var angle = (ComplexRot.Angle + Math.Atan2(Math.Sign(DeltaAngle) * pr.Y, -Math.Sign(DeltaAngle) * pr.X)).WrapAngle();
                var dAngle = -DeltaAngle * Radii.X * Radii.Y / pr.LengthSquared;
                var ddAngle = 2 * dAngle * Math.Sin(2 * FirstAngle) * (Radii.X * Radii.X - Radii.Y * Radii.Y);

                return new OuterAngles(angle, dAngle, ddAngle);
            }
        }

        private Double2 EllipticArc_EntryTangent => EllipticArc_Derivative.EllipticArc_At(0).Normalized;
        private Double2 EllipticArc_ExitTangent => EllipticArc_Derivative.EllipticArc_At(1).Normalized;

        private bool EllipticArc_IsDegenerate => RoughlyZero(Radii);

        private string EllipticArc_ToString()
        {
            string Show(Double2 v) => $"({v.X} {v.Y})";
            var deg = ComplexRot.Angle.ToDegrees();
            var t1d = FirstAngle.ToDegrees();
            var dtd = DeltaAngle.ToDegrees();
            var ord = dtd > 0 ? "CCW" : "CW";
            return $"A{Show(Center)}-{Show(Radii)}:{deg}, {t1d}{Show(At(0))}, {t1d + dtd}{Show(At(1))} {ord}";
        }

        private IEnumerable<Curve> EllipticArc_Simplify()
        {
            // If the ellipse arc is a (rotated) line
            if (RoughlyZero(Radii.X) || RoughlyZero(Radii.Y))
            {
                // Find the maximum points
                var tests = new List<double>() { 0f, 1f };

                for (int k = (int)Math.Ceiling(LesserAngle / Pi_2);
                    k <= (int)Math.Floor(GreaterAngle / Pi_2); k++)
                {
                    var t = AngleToParameter(k * Pi_2);
                    if (GeometricUtils.Inside01(t)) tests.Add(t);
                }

                tests.Sort();

                // Subdivide the lines
                for (int i = 1; i < tests.Count; i++)
                    yield return Line(EllipticArc_At(tests[i - 1]), EllipticArc_At(tests[i]));
            }
            // Not degenerate
            else yield return this;
        }

        private double EllipticArc_NearestPointTo(Double2 v)
        {
            // First, center and un-rotate the point
            var p = (v - Center).RotScale(ComplexRot.NegateY);
            var k = p.X * p.X / (Radii.X * Radii.X) + p.Y * p.Y / (Radii.Y * Radii.Y) - 1;

            // If it is a circle, just calculate its angle
            double t;
            if (RoughlyEquals(Radii.X, Radii.Y)) t = p.Angle;
            // If the point is over the ellipse, calculate its angle the "dumb" form
            else if (RoughlyZero(k)) t = Math.Atan2(Radii.X * p.Y, Radii.Y * p.X);
            else
            {
                // Make the point on the first quadrant
                bool nx = p.X < 0, ny = p.Y < 0;
                if (nx) p.X = -p.X;
                if (ny) p.Y = -p.Y;

                // Radii difference
                var rx = Radii.X;
                var ry = Radii.Y;
                var rr = ry * ry - rx * rx;

                // Function to test for roots
                double f(double tx) => 0.5 * rr * Math.Sin(2 * tx) + rx * p.X * Math.Sin(tx) - ry * p.Y * Math.Cos(tx);

                // There will always be a single root on this equation
                t = Equations.Bisection(f, 0, Pi_2);

                // Now, update the guess for the correct quadrants
                if (nx && ny) t += Pi;
                else if (nx) t = Pi - t;
                else t = -t;
            }

            // If the guess is inside the interval
            var tu = AngleToParameter(t);
            if (!double.IsInfinity(tu)) return tu;
            // Else, pick the nearest of 0 or 1
            return EllipticArc_At(0).DistanceSquaredTo(v) < EllipticArc_At(1).DistanceSquaredTo(v) ? 0 : 1;
        }

        private Double2[] EllipticArc_EnclosingPolygonLocalSpace()
        {
            // Get the possible "problematic" derivative points
            var t1 = FirstAngle; var dt = DeltaAngle;

            var plist = new SortedSet<double>(
                Pi_2.MultiplesBetween(LesserAngle, GreaterAngle).Select(AngleToParameter), RoughComparer) { 0, 1 }.ToList();

            var pointList = new Double2[plist.Count + 1];
            pointList[0] = DeltaAtInternal(plist[0]);
            pointList[plist.Count] = DeltaAtInternal(plist[plist.Count - 1]);
            var d = EllipticArc_Derivative;

            // Add the points at ray intersections
            for (int i = 1; i < plist.Count; i++)
            {
                var c0 = DeltaAtInternal(plist[i - 1]);
                var d0 = d.DeltaAtInternal(plist[i - 1]);
                var c1 = DeltaAtInternal(plist[i]);
                var d1 = d.DeltaAtInternal(plist[i]);

                // Intersections
                var k = d0.Cross(d1); // k is guaranteed to be nonzero
                var p0 = c0 + d0 * (c1 - c0).Cross(d1) / k;
                var p1 = c1 + d1 * (c1 - c0).Cross(d0) / k;

                // Average bost estimates
                pointList[i] = (p0 + p1) / 2;
            }
            return pointList;
        }

        private Double2[] EllipticArc_EnclosingPolygon()
        {
            var arr = EllipticArc_EnclosingPolygonLocalSpace();
            for (int i = 0; i < arr.Length; i++)
                arr[i] = LocalSpaceToGlobal(arr[i]);
            return arr;
        }

        private CurveVertex[] EllipticArc_CurveVertices()
        {
            // Apply the Loop-Blinn method to get the texture coordinates
            // Available on https://www.microsoft.com/en-us/research/wp-content/uploads/2005/01/p1000-loop.pdf

            // This case is not described in the paper, but it is easily derived
            // The texture coordinates are actually, x/a, 1 - y/b, 1 + y/b in local space

            var sign = IsConvex ? 1 : -1;
            var localPoints = EllipticArc_EnclosingPolygonLocalSpace();
            var vertices = new CurveVertex[localPoints.Length];

            for (int i = 0; i < localPoints.Length; i++)
            {
                var p = localPoints[i];
                var kx = p.X / Radii.X;
                var ky = p.Y / Radii.Y;

                // Apply the vertex
                vertices[i] = new CurveVertex(LocalSpaceToGlobal(p), new Double4(kx, 1 - ky, 1 + ky, sign));
            }

            return vertices;
        }
    }
}