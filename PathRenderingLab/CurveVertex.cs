using System.Collections.Generic;

namespace PathRenderingLab
{
    /// <summary>
    /// Represents a vertex used to draw vertex attributes
    /// </summary>
    public struct CurveVertex
    {
        public readonly Double2 Position;
        public readonly Double4 CurveCoords;

        public CurveVertex(Double2 pos, Double4 curve)
        {
            Position = pos;
            CurveCoords = curve;
        }

        public override string ToString() => $"{Position}; {CurveCoords}";

        // Transform a bunch of vertices into a triangle fan
        public static IEnumerable<CurveTriangle> MakeTriangleFan(CurveVertex[] vertices)
        {
            if (vertices.Length < 3) yield break;
            for (int i = 2; i < vertices.Length; i++)
                yield return new CurveTriangle(vertices[0], vertices[i - 1], vertices[i]);
        }
    }
}