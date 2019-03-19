using System.Collections.Generic;

namespace PathRenderingLab.PathCompiler
{
    /// <summary>
    /// Represents a triangle in the plane
    /// </summary>
    public struct Triangle
    {
        public readonly Double2 A, B, C;

        public Triangle(Double2 a, Double2 b, Double2 c)
        {
            bool cw = (b - a).Cross(c - a) < 0;

            A = a;
            B = cw ? c : b;
            C = cw ? b : c;
        }

        public bool ContainsPoint(Double2 p)
        {
            // Pulled from https://stackoverflow.com/a/20861130
            // Barycentric coordinates
            var s = C.Cross(A) + p.Cross(C - A);
            var t = A.Cross(B) + p.Cross(A - B);

            if ((s < 0) != (t < 0)) return false;

            // Area
            var a = B.Cross(C) + A.Cross(B - C);
            if (a < 0) return s <= 0 && s + t >= a;
            else return s >= 0 && s + t <= a;
        }

        public bool IsDegenerate => DoubleUtils.RoughlyZero((B - A).Cross(C - A));

        public static IEnumerable<Triangle> MakeTriangleFan(Double2[] vertices)
        {
            if (vertices.Length < 3) yield break;
            for (int i = 2; i < vertices.Length; i++)
                yield return new Triangle(vertices[0], vertices[i - 1], vertices[i]);
        }

        public override string ToString() => $"{A} / {B} / {C}";
    }
}