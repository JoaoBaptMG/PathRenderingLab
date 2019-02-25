using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace PathRenderingLab.PathCompiler
{
    /// <summary>
    /// Represents a vertex used to draw vertex attributes
    /// </summary>
    public struct DoubleCurveVertex
    {
        public readonly Double2 Position;
        public readonly Double4 CurveCoords1, CurveCoords2;

        public DoubleCurveVertex(Double2 pos, Double4 curve1, Double4 curve2)
        {
            Position = pos;
            CurveCoords1 = curve1;
            CurveCoords2 = curve2;
        }

        public override string ToString() => $"{Position}; {CurveCoords1}; {CurveCoords2}";

        // Transform a bunch of vertices into a triangle fan
        public static IEnumerable<DoubleCurveTriangle> MakeTriangleFan(DoubleCurveVertex[] vertices)
        {
            if (vertices.Length < 3) yield break;
            for (int i = 2; i < vertices.Length; i++)
                yield return new DoubleCurveTriangle(vertices[0], vertices[i - 1], vertices[i]);
        }

        public static explicit operator VertexPositionDoubleCurve(DoubleCurveVertex v)
            => new VertexPositionDoubleCurve(new Vector3((Vector2)v.Position, 0), (Vector4)v.CurveCoords1, (Vector4)v.CurveCoords2);
    }
}