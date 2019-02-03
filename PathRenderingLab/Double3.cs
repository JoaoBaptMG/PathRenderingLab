using Microsoft.Xna.Framework;
using System;

namespace PathRenderingLab
{
    public struct Double3
    {
        public double X, Y, Z;

        public Double3(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Double3 operator +(Double3 a, Double3 b) => new Double3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Double3 operator -(Double3 a, Double3 b) => new Double3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);

        public static Double3 operator *(double a, Double3 b) => new Double3(a * b.X, a * b.Y, a * b.Z);
        public static Double3 operator *(Double3 a, double b) => new Double3(a.X * b, a.Y * b, a.Z * b);

        public static Double3 operator /(Double3 a, double b) => new Double3(a.X / b, a.Y / b, a.Z / b);

        public static Double3 operator -(Double3 a) => new Double3(-a.X, -a.Y, -a.Z);

        public static bool operator ==(Double3 a, Double3 b) => a.X == b.X && a.Y == b.Y && a.Z == b.Z;
        public static bool operator !=(Double3 a, Double3 b) => !(a == b);

        public double Dot(Double3 o) => X * o.X + Y * o.Y + Z * o.Z;
        public Double3 Cross(Double3 o) => new Double3(Y * o.Z - Z * o.Y, Z * o.X - X * o.Z, X * o.Y - Y * o.X);

        public Double3 WithX(double x) => new Double3(x, Y, Z);
        public Double3 WithY(double y) => new Double3(X, y, Z);
        public Double3 WithZ(double z) => new Double3(X, Y, z);
        public Double3 NegateX => new Double3(-X, Y, Z);
        public Double3 NegateY => new Double3(X, -Y, Z);
        public Double3 NegateZ => new Double3(X, Y, -Z);

        public double LengthSquared => X * X + Y * Y + Z * Z;
        public double Length => Math.Sqrt(X * X + Y * Y + Z * Z);

        public double DistanceSquaredTo(Double3 o) => (o - this).LengthSquared;
        public double DistanceTo(Double3 o) => (o - this).Length;

        public Double3 Normalized => this / Length;

        public static readonly Double3 Zero = new Double3(0, 0, 0);

        public override string ToString() => $"{X} {Y} {Z}";

        public override bool Equals(object obj) => obj is Double3 && this == (Double3)obj;
        public override int GetHashCode()
        {
            int hash1 = X.GetHashCode();
            int hash2 = Y.GetHashCode();
            int hash3 = Z.GetHashCode();

            return hash1 ^ (hash2 << 3) ^ (hash2 >> 29) ^ (hash3 << 9) ^ (hash3 >> 23);
        }

        public static explicit operator Vector3(Double3 v) => new Vector3((float)v.X, (float)v.Y, (float)v.Z);
    }
}
