using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab
{
    public class CompiledDrawing
    {
        public Triangle[] Triangles { get; private set; }
        public CurveTriangle[] CurveTriangles { get; private set; }

        private CompiledDrawing() { }

        public static CompiledDrawing FromFace(FillFace face)
        {
            var fill = new CompiledDrawing();

            // Simplify the face by subdividing overlapping curves
            var newFace = face.SubdivideOverlapping();

            // Build the fill polygons and triangulate them
            fill.Triangles = Triangulator.YMonotone.Triangulate(newFace.Contours.Select(FillPolygon).ToArray());

            // Collect all curve triangles
            var curves = newFace.Contours.SelectMany(v => v.Where(c => c.Type != CurveType.Line));
            fill.CurveTriangles = curves.SelectMany(curve => CurveVertex.MakeTriangleFan(curve.CurveVertices)).ToArray();

            return fill;
        }

        private static Double2[] FillPolygon(Curve[] contour)
        {
            // "Guess" a capacity for the list, add the first point
            var list = new List<Double2>((int)(1.4 * contour.Length));
            list.Add(contour[0].At(0));

            // Complete the remaining curves by checking if they are convex or not
            foreach (var curve in contour)
            {
                if (curve.Type == CurveType.Line || curve.IsConvex) list.Add(curve.At(1));
                else list.AddRange(curve.EnclosingPolygon.Skip(1));
            }

            // Sanitize the list; identical vertices
            var indices = new List<int>();
            for (int i = 0; i < list.Count; i++)
                if (DoubleUtils.RoughlyEquals(list[i], list[(i + 1) % list.Count]))
                    indices.Add(i);

            list.ExtractIndices(indices.ToArray());
            return list.ToArray();
        }

        // Join many compiled fills
        public static CompiledDrawing ConcatMany(IEnumerable<CompiledDrawing> fills)
        {
            var triangles = new List<Triangle>();
            var curveTriangles = new List<CurveTriangle>();

            foreach (var fill in fills)
            {
                triangles.AddRange(fill.Triangles);
                curveTriangles.AddRange(fill.CurveTriangles);
            }

            return new CompiledDrawing()
            {
                Triangles = triangles.ToArray(),
                CurveTriangles = curveTriangles.ToArray()
            };
        }

        public static CompiledDrawing Empty => new CompiledDrawing()
        {
            Triangles = new Triangle[0],
            CurveTriangles = new CurveTriangle[0]
        };
    }
}

