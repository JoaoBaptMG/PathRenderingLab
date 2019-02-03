using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Runtime.InteropServices;

namespace PathRenderingLab
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VertexPositionCurve : IVertexType
    {
        public Vector3 Position;
        public Vector4 CurveCoord;
        public static readonly VertexDeclaration VertexDeclaration;

        VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;

        public VertexPositionCurve(Vector3 position, Vector4 curveCoord)
        {
            Position = position;
            CurveCoord = curveCoord;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ (CurveCoord.GetHashCode() * 10973);
            }
        }

        public override string ToString()
        {
            return "{{Position:" + Position + " CurveCoord:" + CurveCoord + "}}";
        }

        public static bool operator ==(VertexPositionCurve left, VertexPositionCurve right)
            => (left.Position == right.Position) && (left.CurveCoord == right.CurveCoord);

        public static bool operator !=(VertexPositionCurve left, VertexPositionCurve right) => !(left == right);

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
            return this == ((VertexPositionCurve)obj);
        }

        static VertexPositionCurve()
        {
            VertexElement[] elements = new VertexElement[]
            {
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector4, VertexElementUsage.TextureCoordinate, 0),
            };

            VertexDeclaration declaration = new VertexDeclaration(elements);
            VertexDeclaration = declaration;
        }

    }
}
