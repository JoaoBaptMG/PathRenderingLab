using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace PathRenderingLab
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionDoubleCurve : IVertexType
    {
        public Vector3 Position;
        public Vector4 CurveCoord1, CurveCoord2;

        public static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration(
            new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
            new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
            new VertexElement(28, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1));

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionDoubleCurve(Vector3 position, Vector4 curveCoord1, Vector4 curveCoord2)
        {
            Position = position;
            CurveCoord1 = curveCoord1;
            CurveCoord2 = curveCoord2;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ (CurveCoord1.GetHashCode() * 10973) ^ (CurveCoord2.GetHashCode() * 65537);
            }
        }

        public override string ToString()
        {
            return "{{Position:" + Position + " CurveCoord1:" + CurveCoord1 + " CurveCoord2:" + CurveCoord2 + "}}";
        }

        public static bool operator ==(VertexPositionDoubleCurve left, VertexPositionDoubleCurve right)
            => (left.Position == right.Position) && (left.CurveCoord1 == right.CurveCoord1) && (left.CurveCoord2 == right.CurveCoord2);

        public static bool operator !=(VertexPositionDoubleCurve left, VertexPositionDoubleCurve right) => !(left == right);

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != GetType()) return false;
            return this == ((VertexPositionDoubleCurve)obj);
        }
    }
}
