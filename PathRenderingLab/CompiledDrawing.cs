using System;
using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab
{
    public class CompiledDrawing
    {
        public Triangle[] Triangles { get; private set; }
        public CurveTriangle[] CurveTriangles { get; private set; }
        public DoubleCurveTriangle[] DoubleCurveTriangles { get; private set; }

        private CompiledDrawing() { }

        public static CompiledDrawing FromFace(FillFace face)
        {
            var fill = new CompiledDrawing();

            // Simplify the face by subdividing overlapping curves
            var newFace = face.SubdivideOverlapping();

            // Build the fill polygons and triangulate them
            fill.Triangles = Triangulator.YMonotone.Triangulate(newFace.Contours.Select(FillPolygon).ToArray());

            // Collect all curve triangles
            var curves = newFace.Contours.SelectMany(v => v.Where(c => c.Type != CurveType.Line)).ToArray();
            var curveTriangles = new List<CurveTriangle>();
            var doubleCurveTriangles = new List<DoubleCurveTriangle>();

            // Check first if the last and first curve aren't joinable
            bool lastFirstJoin = FillFace.AreCurvesFusable(curves[curves.Length - 1], curves[0]);
            if (lastFirstJoin) doubleCurveTriangles.AddRange(FuseCurveTriangles(curves[curves.Length - 1], curves[0]));
            else curveTriangles.AddRange(CurveVertex.MakeTriangleFan(curves[0].CurveVertices));

            // Now, scan the curve list for pairs of scannable curves
            var k = lastFirstJoin ? 1 : 0;
            for (int i = k; i < curves.Length - k; i++)
            {
                if (i < curves.Length - 1 && FillFace.AreCurvesFusable(curves[i], curves[i + 1]))
                {
                    doubleCurveTriangles.AddRange(FuseCurveTriangles(curves[i], curves[i + 1]));
                    i++;
                }
                else curveTriangles.AddRange(CurveVertex.MakeTriangleFan(curves[i].CurveVertices));
            }

            fill.CurveTriangles = curveTriangles.ToArray();
            fill.DoubleCurveTriangles = doubleCurveTriangles.ToArray();

            return fill;
        }

        private static Double2[] FillPolygon(Curve[] contour)
        {
            // "Guess" a capacity for the list, add the first point
            var list = new List<Double2>((int)(1.4 * contour.Length));

            // Check first if the last and first curve aren't joinable
            bool lastFirstJoin = FillFace.AreCurvesFusable(contour[contour.Length - 1], contour[0]);
            if (lastFirstJoin) list.Add(contour[0].At(1));

            // Now, scan the curve list for pairs of scannable curves
            var k = lastFirstJoin ? 1 : 0;
            for (int i = k; i < contour.Length - k; i++)
            {
                if (i < contour.Length - 1 && FillFace.AreCurvesFusable(contour[i], contour[i + 1]))
                {
                    list.Add(contour[i + 1].At(1));
                    i++;
                }
                else if (contour[i].Type == CurveType.Line || contour[i].IsConvex) list.Add(contour[i].At(1));
                else list.AddRange(contour[i].EnclosingPolygon.Skip(1));
            }

            // Sanitize the list; identical vertices
            var indices = new List<int>();
            for (int i = 0; i < list.Count; i++)
                if (DoubleUtils.RoughlyEquals(list[i], list[(i + 1) % list.Count]))
                    indices.Add(i);

            list.ExtractIndices(indices.ToArray());
            return list.ToArray();
        }

        // Generate a curve coordinate extrapolator
        private static Func<Double2,Double4> CoordExtrapolator(CurveVertex[] vertices)
        {
            // Check for the length
            if (vertices.Length == 1)
            {
                var v = vertices[0].CurveCoords;
                return x => v;
            }
            // Extrapolate along the line
            else if (vertices.Length == 2)
            {
                var va = vertices[0];
                var vb = vertices[1];
                var dx = vb.Position - va.Position;

                return delegate (Double2 x)
                {
                    var t = (x - va.Position).Dot(dx) / dx.LengthSquared;
                    return va.CurveCoords + t * (vb.CurveCoords - va.CurveCoords);
                };
            }
            // Extrapolate along a triangle
            else
            {
                var a = vertices[0].Position;
                var dv1 = vertices[1].Position - a;
                var dv2 = vertices[2].Position - a;
                var k = dv1.Cross(dv2);

                var ta = vertices[0].CurveCoords;
                var tb = vertices[1].CurveCoords;
                var tc = vertices[2].CurveCoords;

                return delegate (Double2 x)
                {
                    var u = (x - a).Cross(dv2) / k;
                    var v = -(x - a).Cross(dv1) / k;
                    return ta + u * (tb - ta) + v * (tc - ta);
                };
            }
        }

        private static IEnumerable<DoubleCurveTriangle> FuseCurveTriangles(Curve c1, Curve c2)
        {
            // Extract the curve vertices
            var t1 = c1.CurveVertices;
            var t2 = c2.CurveVertices;

            // Pick their extrapolators
            var ext1 = CoordExtrapolator(t1);
            var ext2 = CoordExtrapolator(t2);

            // Now, we are going to pick the convex hull of the polygon
            var hull = GeometricUtils.ConvexHull(t1.Concat(t2).Select(v => v.Position).ToArray());

            // Finally, calculate the new texture coordinates
            var vertices = hull.Select(p => new DoubleCurveVertex(p, ext1(p), ext2(p)));

            // And return the triangle
            return DoubleCurveVertex.MakeTriangleFan(vertices.ToArray());
        }

        // Join many compiled fills
        public static CompiledDrawing ConcatMany(IEnumerable<CompiledDrawing> fills)
        {
            var triangles = new List<Triangle>();
            var curveTriangles = new List<CurveTriangle>();
            var doubleCurveTriangles = new List<DoubleCurveTriangle>();

            foreach (var fill in fills)
            {
                triangles.AddRange(fill.Triangles);
                curveTriangles.AddRange(fill.CurveTriangles);
                doubleCurveTriangles.AddRange(fill.DoubleCurveTriangles);
            }

            return new CompiledDrawing()
            {
                Triangles = triangles.ToArray(),
                CurveTriangles = curveTriangles.ToArray(),
                DoubleCurveTriangles = doubleCurveTriangles.ToArray(),
            };
        }

        public static CompiledDrawing Empty => new CompiledDrawing()
        {
            Triangles = new Triangle[0],
            CurveTriangles = new CurveTriangle[0],
            DoubleCurveTriangles = new DoubleCurveTriangle[0]
        };
    }
}

