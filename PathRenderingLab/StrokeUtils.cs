using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;

namespace PathRenderingLab
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
                rightCurves.AddRange(GenerateLineCaps(data.Curves[0], false, halfWidth, lineCap));
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
                    var tan1 = c.TangentAt(0);
                    var tan2 = c.TangentAt(1);

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
            Double2 Extrusion(Double2 a, Double2 b) => (a + b).Normalized / Math.Sqrt(0.5 + 0.5 * a.Dot(b));

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
                        // Calculate the offsets
                        var offset0 = (c.B - c.A).CCWPerpendicular.Normalized;
                        var offset1 = (c.C - c.B).CCWPerpendicular.Normalized;

                        // Calculate the endpoints
                        var pa = c.A + halfWidth * offset0;
                        var pc = c.C + halfWidth * offset1;

                        // Calculate the point needed for the middle point offset
                        var pb = c.B + halfWidth * Extrusion(offset0, offset1);

                        return Curve.QuadraticBezier(pa, pb, pc);
                    }
                case CurveType.CubicBezier:
                    {
                        // Calculate the offsets
                        var offset0 = (c.B - c.A).CCWPerpendicular.Normalized;
                        var offset1 = (c.C - c.B).CCWPerpendicular.Normalized;
                        var offset2 = (c.D - c.C).CCWPerpendicular.Normalized;

                        // Calculate the endpoints
                        var pa = c.A + halfWidth * offset0;
                        var pd = c.D + halfWidth * offset2;

                        // Calculate the points needed for the middle point offsets
                        var pb = c.B + halfWidth * Extrusion(offset0, offset1);
                        var pc = c.C + halfWidth * Extrusion(offset1, offset2);

                        return Curve.CubicBezier(pa, pb, pc, pd);
                    }
                case CurveType.EllipticArc: return OffsetEllipticArc(c, halfWidth);
                default: throw new InvalidOperationException("Unrecognized type");
            }
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
            var vh = c.TangentAt(0.5);

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
                    {
                        // Generate the circle
                        var halfRadii = new Double2(halfWidth, halfWidth);
                        yield return Curve.EllipticArc(p + exitOffset, halfRadii, 0, false, sd < 0, p + entryOffset);
                        break;
                    }
                case StrokeLineJoin.Arcs:
                    {
                        throw new NotImplementedException();

                        // Compute the curvatures of the curves
                        var exitKappa = prevCurve.ExitCurvature;
                        var entryKappa = nextCurve.EntryCurvature;

                        // If both of them are zero, fall back to miter
                        if (RoughlyZero(exitKappa) && RoughlyZero(entryKappa))
                            goto case StrokeLineJoin.MiterClip;
                        // If both of them are nonzero, build the circles
                        else if (!RoughlyZero(exitKappa) && !RoughlyZero(entryKappa))
                        {
                            var exitRadius = 1 / exitKappa - sd * halfWidth;
                            var entryRadius = 1 / entryKappa - sd * halfWidth;

                            var exitCenter = prevCurve.ExitTangent.CCWPerpendicular / exitKappa;
                            var entryCenter = nextCurve.EntryTangent.CCWPerpendicular / entryKappa;

                            // Find the intersection points
                            var points = GeometricUtils.CircleIntersection(exitCenter, exitRadius, entryCenter, entryRadius);

                            // If there are no intersections, adjust the curves (later...)
                            if (points.Length == 0)
                            {
                                
                            }

                            // Check both angles
                            double Angle(Double2 v1, Double2 v2) => Math.Atan2(v1.Cross(v2), v1.Dot(v2)).WrapAngle360();

                            var angles = new double[]
                            {
                                Angle(exitOffset - exitCenter, points[0] - exitCenter),
                                Angle(exitOffset - exitCenter, points[1] - exitCenter)
                            };

                            // Pick the point with the least travelled angle
                            var ik = Math.Abs(angles[0]) <= Math.Abs(angles[1]) ? 0 : 1;

                            // Generate the arcs to be rendered
                            var exitRadii = new Double2(exitRadius, exitRadius);
                            var entryRadii = new Double2(entryRadius, entryRadius);

                            var exitAngle = angles[ik];
                            var entryAngle = Angle(points[ik] - entryCenter, entryOffset - entryCenter);

                            // The curves to be rendered
                            yield return Curve.EllipticArc(p + exitOffset, exitRadii, 0,
                                Math.Abs(exitAngle) > Pi, exitAngle > 0, p + points[ik]);
                            yield return Curve.EllipticArc(p + points[ik], entryRadii, 0,
                                Math.Abs(entryAngle) > Pi, entryAngle > 0, p + entryOffset);
                        }
                        else throw new NotImplementedException();
                    }
                    break;
                default: throw new ArgumentException("Unrecognized lineJoin", nameof(lineJoin));
            }
        }

        private static IEnumerable<Curve> GenerateLineCaps(Curve curve, bool atEnd, double halfWidth, StrokeLineCap lineCap)
        {
            // First, get the offset of the curve
            var tangent = atEnd ? curve.ExitTangent : curve.EntryTangent;
            var p = curve.At(atEnd ? 1 : 0);

            // Now, get the offset, properly accounting for the direction
            var offset = halfWidth * tangent.CCWPerpendicular;
            if (atEnd) offset = -offset;

            // Finally, generate the line cap
            switch (lineCap)
            {
                case StrokeLineCap.Butt: yield return Curve.Line(p + offset, p - offset); break;
                case StrokeLineCap.Round:
                    yield return Curve.EllipticArc(p + offset, new Double2(halfWidth, halfWidth), 0, false, true, p - offset);
                    break;
                case StrokeLineCap.Square:
                    {
                        // Generate a half-square
                        var ext = offset.CCWPerpendicular;
                        yield return Curve.Line(p + offset, p + offset + ext);
                        yield return Curve.Line(p + offset + ext, p - offset + ext);
                        yield return Curve.Line(p - offset + ext, p - offset);
                    }
                    break;
                default: throw new ArgumentException("Unregocnized lineCap", nameof(lineCap));
            }
        }
    }
}
