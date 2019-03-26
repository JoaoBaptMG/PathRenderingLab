using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;
using static PathRenderingLab.SwapUtils;

namespace PathRenderingLab.PathCompiler
{
    public static class StrokeUtils
    {
        public static IEnumerable<Curve> ConvertToFill(CurveData data, double halfWidth, StrokeLineCap lineCap,
            StrokeLineJoin lineJoin, double miterLimit)
        {
            // We are going to generate a "stroke fill"
            var leftCurves = new List<Curve>();
            var rightCurves = new List<Curve>();

            // Offset each curve
            for (int i = 0; i < data.Curves.Length; i++)
            {
                leftCurves.AddRange(OffsetCurve(data.Curves[i], halfWidth));
                rightCurves.AddRange(OffsetCurve(data.Curves[i], -halfWidth));

                // Detect where the line join should be added
                if (data.IsClosed || i < data.Curves.Length - 1)
                {
                    var ik = (i + 1) % data.Curves.Length;
                    var prevTangent = data.Curves[i].ExitTangent;
                    var nextTangent = data.Curves[ik].EntryTangent;

                    // If the angles are roughly equal, no line join
                    if (RoughlyEquals(prevTangent, nextTangent)) continue;
                    else
                    {
                        // Else, add to the appropriate list
                        var lineJoint = GenerateLineJoints(data.Curves[i], data.Curves[ik], halfWidth, lineJoin, miterLimit);
                        (prevTangent.Cross(nextTangent) > 0 ? rightCurves : leftCurves).AddRange(lineJoint);
                    }
                }
            }

            // If the curve is open, add the line caps
            if (!data.IsClosed)
            {
                rightCurves.InsertRange(0, GenerateLineCaps(data.Curves[0], false, halfWidth, lineCap));
                leftCurves.AddRange(GenerateLineCaps(data.Curves[data.Curves.Length - 1], true, halfWidth, lineCap));
            }

            // Reverse the inner list and each curve
            var reverseRightCurves = rightCurves.Select(c => c.Reverse).Reverse();

            // Generate the compiled fill
            return leftCurves.Concat(reverseRightCurves).SelectMany(c => c.Simplify(true));
        }

        // Offset a curve by a specified half width (orientation: counterclockwise)
        private static IEnumerable<Curve> OffsetCurve(Curve curve, double halfWidth)
        {
            // If it's a line, much simple:
            if (curve.Type == CurveType.Line) yield return OffsetSimpleCurve(curve, halfWidth);
            else
            {
                // First, reduce the curve into "simple" intervals
                // Based on this implementation: https://github.com/Pomax/bezierjs/blob/gh-pages/lib/bezier.js
                // We'll binary search the intervals instead of dividing into steps
                IEnumerable<Curve> SimplifyCurve(Curve c)
                {
                    // Calculate the cosine of the tangents
                    var tan1 = c.EntryTangent;
                    var tan2 = c.ExitTangent;

                    // The curve is simple if the normals form an angle lesser than a somewhat tight threshold
                    var simple = Math.Abs(tan1.Dot(tan2)) > 0.8;

                    // If the curve is simple, yield itself. Else, split in halves
                    if (simple) yield return c;
                    else
                    {
                        foreach (var k in SimplifyCurve(c.Subcurve(0, 0.5))) yield return k;
                        foreach (var k in SimplifyCurve(c.Subcurve(0.5, 1))) yield return k;
                    }
                }

                // Now, pick the simplified curves and offset each of them individually
                foreach (var cr in SimplifyCurve(curve).SelectMany(c => c.Simplify()))
                    yield return OffsetSimpleCurve(cr, halfWidth);
            }
        }

        private static Curve OffsetSimpleCurve(Curve c, double halfWidth)
        {

            // Depends on the type of the curve
            switch (c.Type)
            {
                case CurveType.Line:
                    {
                        var offset = halfWidth * (c.B - c.A).CCWPerpendicular.Normalized;
                        return Curve.Line(c.A + offset, c.B + offset);
                    }
                case CurveType.QuadraticBezier:
                    {
                        var ps = ExtrudePolyline(new[] { c.A, c.B, c.C }, halfWidth);
                        return Curve.QuadraticBezier(ps[0], ps[1], ps[2]);
                    }
                case CurveType.CubicBezier:
                    {
                        // Equal cases
                        if (RoughlyEquals(c.A, c.B))
                        {
                            var ps = ExtrudePolyline(new[] { (c.A + c.B) / 2, c.C, c.D }, halfWidth);
                            return Curve.CubicBezier(ps[0], ps[0], ps[1], ps[2]);
                        }
                        else if (RoughlyEquals(c.B, c.C))
                        {
                            var ps = ExtrudePolyline(new[] { c.A, (c.B + c.C) / 2, c.D }, halfWidth);
                            return Curve.CubicBezier(ps[0], ps[1], ps[1], ps[2]);
                        }
                        else if (RoughlyEquals(c.C, c.D))
                        {
                            var ps = ExtrudePolyline(new[] { c.A, c.B, (c.C + c.D) / 2 }, halfWidth);
                            return Curve.CubicBezier(ps[0], ps[1], ps[2], ps[2]);
                        }
                        else
                        {
                            var ps = ExtrudePolyline(new[] { c.A, c.B, c.C, c.D }, halfWidth);
                            return Curve.CubicBezier(ps[0], ps[1], ps[2], ps[3]);
                        }
                    }
                case CurveType.EllipticArc: return OffsetEllipticArc(c, halfWidth);
                default: throw new InvalidOperationException("Unrecognized type");
            }
        }

        private static Double2[] ExtrudePolyline(Double2[] points, double halfWidth)
        {
            // Extrude the polygonal line formed by points. It is assumed that the points are different
            Double2 Extrusion(Double2 a, Double2 b) => (a + b).Normalized / Math.Sqrt(0.5 + 0.5 * a.Dot(b));

            var result = new Double2[points.Length];
            int pl = points.Length;

            result[0] = points[0] + halfWidth * (points[1] - points[0]).CCWPerpendicular.Normalized;
            result[pl - 1] = points[pl - 1] + halfWidth * (points[pl - 1] - points[pl - 2]).CCWPerpendicular.Normalized;

            for (int i = 1; i < pl-1; i++)
            {
                var offset0 = (points[i] - points[i - 1]).CCWPerpendicular.Normalized;
                var offset1 = (points[i + 1] - points[i]).CCWPerpendicular.Normalized;

                result[i] = points[i] + halfWidth * Extrusion(offset0, offset1);
            }

            return result;
        }

        private static Curve OffsetEllipticArc(Curve c, double halfWidth)
        {
            // Bail out the simple case quickly
            if (RoughlyEquals(c.Radii.X, c.Radii.Y))
                return c.EnlargeRadii(c.IsConvex ? -halfWidth : halfWidth);

            // This one is tricky, it will involve solving a complicated equation system
            double[] EquationForPosition(Double2 p) =>
                new double[] { p.X * p.X, 2 * p.X * p.Y, p.Y * p.Y, 2 * p.X, 2 * p.Y, 1 };

            double[] EquationForVelocity(Double2 p, Double2 v) =>
                new double[] { p.X * v.X, p.X * v.Y + p.Y * v.X, p.Y * v.Y, v.X, v.Y, 0 };

            Double2 Offset(Double2 p, Double2 h) => p + halfWidth * h.CCWPerpendicular;

            // Find the wanted points
            var x0 = c.At(0);
            var x1 = c.At(1);
            var xh = c.At(0.5);

            var v0 = c.EntryTangent;
            var v1 = c.ExitTangent;
            var vh = c.Derivative.At(0.5).Normalized;

            var xr0 = Offset(x0, v0);
            var xr1 = Offset(x1, v1);
            var xrh = Offset(xh, vh);

            // Mount the equations
            var eqs = new double[][]
            {
                EquationForPosition(xr0),
                EquationForPosition(xr1),
                EquationForPosition(xrh),
                EquationForVelocity(xr0, v0),
                EquationForVelocity(xr1, v1)
            };

            // And solve the homogeneous equation system (which is guaranteed to have a solution)
            var data = Equations.SolveLinearSystem(eqs, new double[5]);
            var pr = data.Value.LinearTerms[0];
            double au = pr[0], bu = pr[1], cu = pr[2], du = pr[3], eu = pr[4], fu = pr[5];
            if (au < 0) { au = -au; bu = -bu; cu = -cu; du = -du; eu = -eu; fu = -fu; }

            // The rotation angle
            var xr = Math.Atan(2 * bu / (cu - au)) / 2;
            var sin2 = Math.Sin(-2 * xr);
            var cos = Math.Cos(-xr);
            var sin = Math.Sin(-xr);

            // Calculate the rotated coefficients
            var ap = au * cos * cos + bu * sin2 + cu * sin * sin;
            var cp = au * sin * sin - bu * sin2 + cu * cos * cos;
            var dp = du * cos + eu * sin;
            var ep = -du * sin + eu * cos;
            var fp = fu;

            // And populate the radii from this
            var rk = dp * dp / ap + ep * ep / cp - fp;
            var rx = Math.Sqrt(rk / Math.Abs(ap));
            var ry = Math.Sqrt(rk / Math.Abs(cp));
            var radii = new Double2(rx, ry);

            // Large arc and sweep
            var largeArc = Math.Abs(c.DeltaAngle) > Pi;
            var sweep = c.DeltaAngle > 0;

            return Curve.EllipticArc(xr0, radii, xr, largeArc, sweep, xr1);
        }

        private static IEnumerable<Curve> GenerateLineJoints(Curve prevCurve, Curve nextCurve, double halfWidth,
            StrokeLineJoin lineJoin, double miterLimit)
        {
            // First, calculate the cross-product between the tangents
            var exitTangent = prevCurve.ExitTangent;
            var entryTangent = nextCurve.EntryTangent;
            var diff = entryTangent.Cross(exitTangent);
            var sd = diff < 0 ? -1 : 1; // Account for half-turns here too

            // The common point and the offset vectors
            var p = (prevCurve.At(1) + nextCurve.At(0)) / 2;
            var entryOffset = sd * halfWidth * entryTangent.CCWPerpendicular;
            var exitOffset = sd * halfWidth * exitTangent.CCWPerpendicular;

            // Now, create the next triangles if necessary
            switch (lineJoin)
            {
                case StrokeLineJoin.Bevel: // Just the bevel line
                    yield return Curve.Line(p + exitOffset, p + entryOffset);
                    break;
                case StrokeLineJoin.Miter:
                case StrokeLineJoin.MiterClip:
                    {
                        // Calculate the bisector and miter length
                        var cos = exitOffset.Dot(entryOffset) / Math.Sqrt(exitOffset.LengthSquared * entryOffset.LengthSquared);
                        var miter = halfWidth / Math.Sqrt(0.5 + 0.5 * cos);

                        // Check the conditions for the miter (only clip if miter-clip is explicity selected)
                        if (lineJoin == StrokeLineJoin.Miter && miter >= halfWidth * miterLimit)
                            goto case StrokeLineJoin.Bevel;

                        // Generate the miter
                        var miterWidth = halfWidth * miterLimit;
                        var bisectorOffset = miter * (entryOffset + exitOffset).Normalized;
                        if (miter < miterWidth)
                        {
                            yield return Curve.Line(p + exitOffset, p + bisectorOffset);
                            yield return Curve.Line(p + bisectorOffset, p + entryOffset);
                        }
                        else
                        {
                            // Clip the miter
                            Double2 p1, p2;

                            // Account for the special case of a 180 degree turn
                            if (double.IsInfinity(miter))
                            {
                                var outwardOffset = entryOffset.Normalized.CCWPerpendicular;
                                p1 = entryOffset + miterWidth * outwardOffset;
                                p2 = exitOffset + miterWidth * outwardOffset;
                            }
                            else
                            {
                                p1 = entryOffset + miterWidth * (bisectorOffset - entryOffset) / miter;
                                p2 = exitOffset + miterWidth * (bisectorOffset - exitOffset) / miter;
                            }

                            yield return Curve.Line(p + exitOffset, p + p2);
                            yield return Curve.Line(p + p2, p + p1);
                            yield return Curve.Line(p + p1, p + entryOffset);
                        }
                        break;
                    }
                case StrokeLineJoin.Round:
                        // Generate the circle
                        yield return Curve.Circle(p, halfWidth, exitOffset, entryOffset, sd < 0);
                        break;
                case StrokeLineJoin.Arcs:
                    {
                        // Compute the curvatures of the curves
                        var exitKappa = prevCurve.ExitCurvature;
                        var entryKappa = nextCurve.EntryCurvature;

                        // If one of the curvatures is too large, fall back to round
                        if (Math.Abs(exitKappa) * halfWidth >= 1 || Math.Abs(entryKappa) * halfWidth >= 1)
                            goto case StrokeLineJoin.Round;

                        // If both of them are zero, fall back to miter
                        if (RoughlyZero(exitKappa) && RoughlyZero(entryKappa))
                            goto case StrokeLineJoin.MiterClip;
                        // If one (or both) are nonzero, build the possible circles
                        else 
                        {
                            // The "arcs" must use a different sign convention
                            var sign = diff > 0 ? 1 : -1;

                            var exitRadius = 1 / exitKappa - sign * halfWidth;
                            var entryRadius = 1 / entryKappa - sign * halfWidth;

                            var exitCenter = exitTangent.CCWPerpendicular / exitKappa;
                            var entryCenter = entryTangent.CCWPerpendicular / entryKappa;

                            // Find the intersection points
                            Double2[] points;
                            if (!RoughlyZero(exitKappa) && !RoughlyZero(entryKappa))
                                points = GeometricUtils.CircleIntersection(exitCenter, exitRadius, entryCenter, entryRadius);
                            else if (!RoughlyZero(exitKappa))
                                points = GeometricUtils.CircleLineIntersection(exitCenter, exitRadius, entryOffset, entryTangent);
                            else points = GeometricUtils.CircleLineIntersection(entryCenter, entryRadius, exitOffset, exitTangent);

                            // If there are no intersections, adjust the curves
                            if (points.Length == 0)
                            {
                                if (!RoughlyZero(exitKappa) && !RoughlyZero(entryKappa))
                                    points = AdjustCircles(ref exitCenter, ref exitRadius, exitTangent.CCWPerpendicular,
                                        ref entryCenter, ref entryRadius, entryTangent.CCWPerpendicular);
                                else if (!RoughlyZero(exitKappa))
                                    points = AdjustCircleLine(ref exitCenter, ref exitRadius, exitTangent.CCWPerpendicular,
                                        entryOffset, entryTangent);
                                else points = AdjustCircleLine(ref entryCenter, ref entryRadius, entryTangent.CCWPerpendicular,
                                    exitOffset, exitTangent);
                            }

                            // If still no solutions are found, go to the miter-clip case
                            if (points.Length == 0) goto case StrokeLineJoin.MiterClip;

                            // Check both which point have less travelled distance
                            double PointParameter(Double2 pt)
                            {
                                if (RoughlyZero(exitKappa))
                                {
                                    var d = exitTangent.Dot(pt - exitOffset);
                                    return d < 0 ? double.PositiveInfinity : d;
                                }
                                return (Math.Sign(exitKappa) * (exitOffset - exitCenter).AngleBetween(pt - exitCenter)).WrapAngle360();
                            }

                            var angles = points.Select(PointParameter).ToArray();

                            // Pick the point with the least travelled angle
                            var point = angles[0] <= angles[1] ? points[0] : points[1];

                            // Generate the arcs to be rendered
                            if (!RoughlyZero(exitKappa))
                                yield return Curve.Circle(p + exitCenter, Math.Abs(exitRadius), exitOffset - exitCenter,
                                    point - exitCenter, exitKappa > 0);
                            else yield return Curve.Line(p + exitOffset, p + point);

                            if (!RoughlyZero(entryKappa))
                                yield return Curve.Circle(p + entryCenter, Math.Abs(entryRadius), point - entryCenter,
                                    entryOffset - entryCenter, entryKappa > 0);
                            else yield return Curve.Line(p + point, p + entryOffset);
                        }
                    }
                    break;
                default: throw new ArgumentException("Unrecognized lineJoin", nameof(lineJoin));
            }
        }

        private static Double2[] AdjustCircles(ref Double2 c1, ref double r1, Double2 n1, ref Double2 c2, ref double r2, Double2 n2)
        {
            Double2 pt1, pt2;

            // Negate if necessary, so we are working with positive values of the radius
            bool neg1 = r1 < 0, neg2 = r2 < 0;
            if (neg1) { r1 = -r1; n1 = -n1; }
            if (neg2) { r2 = -r2; n2 = -n2; }

            // There are two cases: if the circles are outside (their radius sum is less than their distance)
            // and if the circles are inside (their radius difference is less than their distance)
            var rs = r1 + r2;

            // The first case
            if (c1.DistanceSquaredTo(c2) > rs*rs)
            {
                // Compute the coefficients of the polynomials
                var p = n1.DistanceSquaredTo(n2);
                var q = 2 * (c1 - c2).Dot(n1 - n2);
                var a = c1.DistanceSquaredTo(c2);

                // There are two polyomials that, solved, give the necessary displacement for this adjustment
                // One of them is pλ² + qλ + a² - (r1-r2)² and the other is (p-4)λ² + (q - 4(r1+r2))λ + a - (r1+r2)²
                // Experimentally, though, only the latter one gives a "useful" value
                // We still have an edge case, though, as there might be no positive root to this (this happens if
                // the circles have to grow at opposite direction). On this case, return an empty array
                var roots = Equations.SolveQuadratic(p - 4, q - 4 * rs, a - rs * rs);
                if (!roots.Any(r => r >= 0)) pt1 = pt2 = new Double2(double.NaN, double.NaN);
                else
                {
                    var l = roots.Where(r => r >= 0).Min();

                    // Correctly adjust the centers and radii
                    c1 += n1 * l;
                    c2 += n2 * l;
                    r1 += l;
                    r2 += l;

                    // The single point as a double root
                    var dir = (c2 - c1).Normalized;
                    pt1 = c1 + dir * r1;
                    pt2 = c2 - dir * r2;
                }
            }
            // The second case
            else
            {
                // Swap the circles if the first one is smaller
                bool swap = r1 < r2;
                if (swap)
                {
                    Swap(ref c1, ref c2);
                    Swap(ref r1, ref r2);
                    Swap(ref n1, ref n2);
                }

                // Compute the coefficients of the polynomials
                var p = (n1 + n2).LengthSquared;
                var q = 2 * (c1 - c2).Dot(n1 + n2);
                var a = c1.DistanceSquaredTo(c2);

                // Again, here, there are two polynomials: pλ² - qλ + a² - (r1+r2)² and (p-4)λ² + (-q + 4(r1-r2))λ + a - (r1-r2)²
                // And again, experimentally only the second one will give the "correct" result
                var rd = r1 - r2;
                var roots = Equations.SolveQuadratic(p - 4, -q + 4 * rd, a - rd * rd);
                var l = roots.Where(r => r >= 0).Min();

                // Correctly adjust the centers and radii
                c1 -= n1 * l;
                c2 += n2 * l;
                r1 -= l;
                r2 += l;

                // Pick the single point intersection
                var dir = (c2 - c1).Normalized;
                pt1 = c1 + dir * r1;
                pt2 = c2 + dir * r2;

                // Finally, swap them again if we had to swap earlier
                if (swap)
                {
                    Swap(ref c1, ref c2);
                    Swap(ref r1, ref r2);
                }
            }

            // Return back their signs
            if (neg1) r1 = -r1;
            if (neg2) r2 = -r2;

            if (double.IsNaN(pt1.X)) return new Double2[0];
            return new[] { (pt1 + pt2) / 2, (pt1 + pt2) / 2 };
        }

        private static Double2[] AdjustCircleLine(ref Double2 c1, ref double r1, Double2 n1, Double2 c, Double2 v)
        {
            // Negate if necessary, so we are working with positive values of the radius
            bool neg1 = r1 < 0;
            if (neg1) { r1 = -r1; n1 = -n1; }

            Double2 pt;

            // Get the coefficients
            var rot = v.Normalized;
            var h = (c - c1).Cross(rot);
            var k = n1.Cross(rot);

            // If k = -1, there is no solution
            if (RoughlyEquals(k, -1)) pt = new Double2(double.NaN, double.NaN);
            else
            {
                // Get the solution
                var l = (h - r1) / (k + 1);

                r1 += l;
                c1 += n1 * l;

                // Get the solution
                pt = c1 - c - rot * (c1 - c).Dot(rot);
            }

            // Return back their signs
            if (neg1) r1 = -r1;

            if (double.IsNaN(pt.X)) return new Double2[0];
            return new[] { pt, pt };
        }

        private static IEnumerable<Curve> GenerateLineCaps(Curve curve, bool atEnd, double halfWidth, StrokeLineCap lineCap)
        {
            // First, get the offset of the curve
            var tangent = atEnd ? curve.ExitTangent : curve.EntryTangent;
            var p = curve.At(atEnd ? 1 : 0);

            // Now, get the offset, properly accounting for the direction
            var offset = halfWidth * tangent.CCWPerpendicular;

            // Finally, generate the line cap
            switch (lineCap)
            {
                case StrokeLineCap.Butt: yield return Curve.Line(p + offset, p - offset); break;
                case StrokeLineCap.Round: yield return Curve.Circle(p, halfWidth, offset, -offset, !atEnd); break;
                case StrokeLineCap.Square:
                    {
                        // Generate a half-square
                        var ext = offset.CCWPerpendicular;
                        if (atEnd) ext = -ext;
                        yield return Curve.Line(p + offset, p + offset + ext);
                        yield return Curve.Line(p + offset + ext, p - offset + ext);
                        yield return Curve.Line(p - offset + ext, p - offset);
                    }
                    break;
                default: throw new ArgumentException("Unrecognized lineCap", nameof(lineCap));
            }
        }
    }
}
