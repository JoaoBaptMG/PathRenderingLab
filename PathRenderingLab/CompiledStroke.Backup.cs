#if false

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PathRenderingLab.DoubleUtils;

namespace PathRenderingLab
{
    public class CompiledStroke
    {
        public Triangle[] Triangles { get; private set; }
        public CurveTriangle[] CurveTriangles { get; private set; }

        private CompiledStroke() { }

        public static CompiledStroke FromSegment(Curve[] curves, bool closed, double halfWidth, StrokeLineCap lineCap,
            StrokeLineJoin lineJoin, double miterLimit)
        {
            var triangles = new List<Triangle>();
            var curveTriangles = new List<CurveTriangle>();

            // Generate the main lines
            for (int i = 0; i < curves.Length; i++)
            {
                var prevAngle = curves[(i + curves.Length - 1) % curves.Length].ExitAngle;
                var nextAngle = curves[(i + 1) % curves.Length].EntryAngle;

                if (!closed && i == 0) prevAngle = curves[0].EntryAngle;
                if (!closed && i == curves.Length - 1)
                    nextAngle = curves[curves.Length - 1].ExitAngle;

                // Generate the right triangles for the right curve types
                if (curves[i].Type == CurveType.Line)
                    GenerateLineTriangles(triangles, curves[i], halfWidth, prevAngle, nextAngle);
                else GenerateCurveTriangles(curveTriangles, curves[i], halfWidth, prevAngle, nextAngle);
            }

            // Generate the line joins
            for (int i = 1; i < curves.Length; i++)
                GenerateLineJoints(triangles, curveTriangles, curves[i - 1], curves[i], halfWidth, lineJoin, miterLimit);

            if (closed) GenerateLineJoints(triangles, curveTriangles, curves[curves.Length - 1], curves[0], halfWidth, lineJoin, miterLimit);
            else
            {
                // Generate the line caps
                GenerateLineCaps(triangles, curveTriangles, curves[0], false, halfWidth, lineCap);
                GenerateLineCaps(triangles, curveTriangles, curves[curves.Length - 1], true, halfWidth, lineCap);
            }

            // Generate the new compiled stroke
            return new CompiledStroke()
            {
                Triangles = triangles.ToArray(),
                CurveTriangles = curveTriangles.ToArray()
            };
        }

        // Join many compiled strokes
        public static CompiledStroke ConcatMany(IEnumerable<CompiledStroke> strokes)
        {
            var triangles = new List<Triangle>();
            var curveTriangles = new List<CurveTriangle>();

            foreach (var stroke in strokes)
            {
                triangles.AddRange(stroke.Triangles);
                curveTriangles.AddRange(stroke.CurveTriangles);
            }

            return new CompiledStroke()
            {
                Triangles = triangles.ToArray(),
                CurveTriangles = curveTriangles.ToArray(),
            };
        }

        private static void GenerateLineTriangles(List<Triangle> triangles, Curve curve,
            double halfWidth, double prevAngle, double nextAngle)
        {
            var vertices = new List<Double2>(6);

            // Entry and exit angle
            var angle = curve.A.AngleFacing(curve.B);

            // Add the vertex extremes
            AddExtremeVertex(vertices, halfWidth, curve.A, prevAngle, angle, true);
            AddExtremeVertex(vertices, halfWidth, curve.B, angle, nextAngle, false);

            // Finally, add the triangles made by the curve
            triangles.AddRange(Triangle.MakeTriangleFan(vertices.ToArray()));
        }

        private static void AddExtremeVertex(List<Double2> vertices, double halfWidth, Double2 x,
            double entryAngle, double exitAngle, bool startVertex)
        {
            var offset = halfWidth * Double2.FromAngle((startVertex ? exitAngle : entryAngle) + Pi_2);
            if (!startVertex) offset = -offset;

            // There are two cases to process on the start point:
            // When the angles are equal, there is no need to generate the "gap" to be filled by the joint
            if (RoughlyEquals(exitAngle, entryAngle))
            {
                vertices.Add(x + offset);
                vertices.Add(x - offset);
            }
            // When the angles are different, we generate the perpendicular bisector
            else
            {
                // The sign difference between the entry angle and the previous curve's exit angle will tell
                // where the angle bisector will need to be put
                var diff = (exitAngle - entryAngle).WrapAngle();
                var bisectorOffset = halfWidth / Math.Cos(Math.Abs(diff) / 2) * Double2.FromAngle(entryAngle + diff / 2 + Pi_2);
                if (!startVertex) bisectorOffset = -bisectorOffset;

                // If the difference is positive, the bisector is to the right (i.e. AFTER the first point)
                if (startVertex == (diff > 0))
                {
                    vertices.Add(x + bisectorOffset);
                    vertices.Add(x);
                    vertices.Add(x - offset);
                }
                // If it is negative, the bisector is to the left (i.e. BEFORE the point)
                else
                {
                    vertices.Add(x + offset);
                    vertices.Add(x);
                    vertices.Add(x - bisectorOffset);
                }
            }
        }

        private static void GenerateCurveTriangles(List<CurveTriangle> curveTriangles, Curve curve,
            double halfWidth, double prevAngle, double nextAngle)
        {
            // First, to generate the curve, pick the polygons
            var poly = curve.CurveVertices;

            // Fall back to line triangles
            if (poly.Length < 3) return;

            // Pick three vertices
            Double2 a = poly[0].Position, b = poly[1].Position, c = poly[2].Position;
            Double4 ta = poly[0].CurveCoords, tb = poly[1].CurveCoords, tc = poly[2].CurveCoords;
            var k = (b - a).Cross(c - a);

            // Generate extrusion
            CurveVertex Extrapolate(Double2 x)
            {
                var u = (x - a).Cross(c - a) / k;
                var v = -(x - a).Cross(b - a) / k;
                return new CurveVertex(x, ta + u * (tb - ta) + v * (tc - ta));
            }

            // The curve's entry angle and exit angle
            var entryAngle = curve.EntryAngle;
            var exitAngle = curve.ExitAngle;

            // Generate the polygon list
            var list = new List<Double2>(poly.Length + 4);

            // Add the vertex extremes
            AddExtremeVertex(list, halfWidth, poly[0].Position, prevAngle, entryAngle, true);

            // The order of the points is different for whether the curve is convex or not
            // Concave is: start - end - controls (reversed)
            // Convex is: start - controls - end
            if (!curve.IsConvex)
            {
                AddExtremeVertex(list, halfWidth, poly[poly.Length - 1].Position, exitAngle, nextAngle, false);
                Array.Reverse(poly);
            }

            // Extrapolate each vertex
            for (int i = 1; i < poly.Length - 1; i++)
            {
                var dv1 = (poly[i - 1].Position - poly[i].Position).Normalized;
                var dv2 = (poly[i + 1].Position - poly[i].Position).Normalized;

                // Use the half-arc sine rule here to calcualte the tangent vector
                var sec = 1 / Math.Sqrt(0.5 - 0.5 * dv1.Dot(dv2));
                if (true)
                {
                    list.Add(poly[i].Position + halfWidth * dv1.Rotate(Pi_2));
                    list.Add(poly[i].Position - halfWidth * dv2.Rotate(Pi_2));
                }
                else list.Add(poly[i].Position - halfWidth * sec * (dv1 + dv2).Normalized);
            }

            if (curve.IsConvex) AddExtremeVertex(list, halfWidth, poly[poly.Length - 1].Position, exitAngle, nextAngle, false);

            // Triangulate the extrapolated list
            curveTriangles.AddRange(CurveVertex.MakeTriangleFan(list.Select(Extrapolate).ToArray()));
        }

        private static void GenerateLineJoints(List<Triangle> triangles, List<CurveTriangle> curveTriangles,
            Curve prevCurve, Curve nextCurve, double halfWidth, StrokeLineJoin lineJoin, double miterLimit)
        {
            // First, calculate the difference between the angles to check where the joint need to be formed
            var exitAngle = prevCurve.ExitAngle;
            var entryAngle = nextCurve.EntryAngle;
            var diff = (exitAngle - entryAngle).WrapAngle();
            var sd = Math.Sign(diff);

            // Skip creating the joint if the diff is small enough
            if (RoughlyZero(diff)) return;

            // The common point and the offset vectors
            var p = (prevCurve.At(1) + nextCurve.At(0)) / 2;
            var entryOffset = sd * halfWidth * Double2.FromAngle(entryAngle + Pi_2);
            var exitOffset = sd * halfWidth * Double2.FromAngle(exitAngle + Pi_2);

            // Calculate the bisector and miter length
            var miter = halfWidth / Math.Cos(Math.Abs(diff) / 2);
            var bisectorOffset = sd * miter * Double2.FromAngle(entryAngle + diff / 2 + Pi_2);

            // Utility function for miter and round
            Double2[] GenerateClippedTriangle(bool forRound)
            {
                var miterWidth = halfWidth * (forRound ? 1f : miterLimit);
                if (miter < miterWidth) return new[] { Double2.Zero, entryOffset, bisectorOffset, exitOffset };
                else
                {
                    // Clip the miter
                    var p1 = entryOffset + miterWidth * (bisectorOffset - entryOffset) / miter;
                    var p2 = exitOffset + miterWidth * (bisectorOffset - exitOffset) / miter;
                    return new[] { Double2.Zero, entryOffset, p1, p2, exitOffset };
                }
            }

            // Now, create the next triangles if necessary
            switch (lineJoin)
            {
                case StrokeLineJoin.Bevel:
                    // Create the bevel triangle
                    triangles.Add(new Triangle(p, p + entryOffset, p + exitOffset));
                    break;
                case StrokeLineJoin.Miter:
                case StrokeLineJoin.MiterClip:
                    {
                        // Check the conditions for the miter (only clip if miter-clip is explicity selected)
                        if (lineJoin == StrokeLineJoin.Miter && miter >= halfWidth * miterLimit) break;

                        // Generate the miter
                        var polygon = GenerateClippedTriangle(false).Select(v => p + v).ToArray();
                        triangles.AddRange(Triangle.MakeTriangleFan(polygon));
                        break;
                    }
                case StrokeLineJoin.Round:
                    {
                        // Generate the round triangle
                        var curvePolygon = GenerateClippedTriangle(true)
                            .Select(v => new CurveVertex(p + v, new Double4(v.X, v.Y, -v.Y, 1f))).ToArray();
                        curveTriangles.AddRange(CurveVertex.MakeTriangleFan(curvePolygon));
                        break;
                    }
                case StrokeLineJoin.Arcs:
                    {
                        // Compute the curvatures of the curves
                        var exitKappa = prevCurve.ExitCurvature;
                        var entryKappa = nextCurve.EntryCurvature;

                        // If both of them are zero, fall back to miter
                        if (RoughlyZero(exitKappa) && RoughlyZero(entryKappa))
                            goto case StrokeLineJoin.MiterClip;

                        throw new NotImplementedException("Later i'll end it");
                    }
                    break;
                default: break;
            }
        }

        private static void GenerateLineCaps(List<Triangle> triangles, List<CurveTriangle> curveTriangles,
            Curve curve, bool atEnd, double width, StrokeLineCap lineCap)
        {
            throw new NotImplementedException();
        }
    }
}

#endif