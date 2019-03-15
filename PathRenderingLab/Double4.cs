using Microsoft.Xna.Framework;
using System;

namespace PathRenderingLab
{
    public struct Double4
    {
        public double X, Y, Z, W;

        public Double4(double x, double y, double z, double w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Double4 operator +(Double4 a, Double4 b) => new Double4(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        public static Double4 operator -(Double4 a, Double4 b) => new Double4(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);

        public static Double4 operator *(double a, Double4 b) => new Double4(a * b.X, a * b.Y, a * b.Z, a * b.W);
        public static Double4 operator *(Double4 a, double b) => new Double4(a.X * b, a.Y * b, a.Z * b, a.W * b);

        public static Double4 operator /(Double4 a, double b) => new Double4(a.X / b, a.Y / b, a.Z / b, a.W / b);

        public static Double4 operator -(Double4 a) => new Double4(-a.X, -a.Y, -a.Z, -a.W);

        public static bool operator ==(Double4 a, Double4 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z && a.W == b.W;
        public static bool operator !=(Double4 a, Double4 b) => !(a == b);

        public double Dot(Double4 o) => X * o.X + Y * o.Y + Z * o.Z + W * o.W;

        public Double4 WithX(double x) => new Double4(x, Y, Z, W);
        public Double4 WithY(double y) => new Double4(X, y, Z, W);
        public Double4 WithZ(double z) => new Double4(X, Y, z, W);
        public Double4 WithW(double w) => new Double4(X, Y, Z, w);
        public Double4 NegateX => new Double4(-X, Y, Z, W);
        public Double4 NegateY => new Double4(X, -Y, Z, W);
        public Double4 NegateZ => new Double4(X, Y, -Z, W);
        public Double4 NegateW => new Double4(X, Y, Z, -W);

        public double LengthSquared => X * X + Y * Y + Z * Z + W * W;
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z + W * W);

        public double DistanceSquaredTo(Double4 o) => (o - this).LengthSquared;
        public double DistanceTo(Double4 o) => (o - this).Length;

        public Double4 Normalized => this / Length;

        public static readonly Double4 Zero = new Double4(0, 0, 0, 0);

        public override string ToString() => $"{X} {Y} {Z} {W}";

        public override bool Equals(object obj) => obj is Double4 && this == (Double4)obj;
        public override int GetHashCode()
        {
            int hash1 = X.GetHashCode();
            int hash2 = Y.GetHashCode();
            int hash3 = Z.GetHashCode();
            int hash4 = W.GetHashCode();

            return hash1 ^ (hash2 << 3) ^ (hash2 >> 29) ^ (hash3 << 9) ^ (hash3 >> 23) ^ (hash4 << 17) ^ (hash4 >> 15);
        }

        public Double4 Truncate() => new Double4(X.Truncate(), Y.Truncate(), Z.Truncate(), W.Truncate());

        public static explicit operator Vector4(Double4 v) => new Vector4((float)v.X, (float)v.Y, (float)v.Z, (float)v.W);
    }
}
