using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace PathRenderingLab
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionCurveStroke : IVertexType
    {
        public Vector3 Position;
        public Vector4 CurveCoord, CurveCoordX, CurveCoordY;
        public static readonly VertexDeclaration VertexDeclaration;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionCurveStroke(Vector3 position, Vector4 curveCoord, Vector4 cx, Vector4 cy)
        {
            Position = position;
            CurveCoord = curveCoord;
            CurveCoordX = cx;
            CurveCoordY = cy;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ (CurveCoord.GetHashCode() * 10973) ^ (CurveCoordX.GetHashCode() * 29937) ^
                    CurveCoordY.GetHashCode();
            }
        }

        public override string ToString()
        {
            return "{{Position:" + Position + " CurveCoord:" + CurveCoord + "}}";
        }

        public static bool operator ==(VertexPositionCurveStroke left, VertexPositionCurveStroke right)
            => (left.Position == right.Position) && (left.CurveCoord == right.CurveCoord) &&
            (left.CurveCoordX == right.CurveCoordX) && (left.CurveCoordY == right.CurveCoordY);

        public static bool operator !=(VertexPositionCurveStroke left, VertexPositionCurveStroke right) => !(left == right);

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            if (obj.GetType() != GetType())
            {
                return false;
            }
            return this == ((VertexPositionCurveStroke)obj);
        }

        static VertexPositionCurveStroke()
        {
            VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(28, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 1),
                new VertexElement(44, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 2),
            };

            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

    }
}
