using Microsoft.Xna.Framework;
using System;

namespace PathRenderingLab
{
    public struct Double2
    {
        public double X, Y;

        public Double2(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Double2 operator +(Double2 a, Double2 b) => new Double2(a.X + b.X, a.Y + b.Y);
        public static Double2 operator -(Double2 a, Double2 b) => new Double2(a.X - b.X, a.Y - b.Y);

        public static Double2 operator *(double a, Double2 b) => new Double2(a * b.X, a * b.Y);
        public static Double2 operator *(Double2 a, double b) => new Double2(a.X * b, a.Y * b);

        public static Double2 operator /(Double2 a, double b) => new Double2(a.X / b, a.Y / b);

        public static Double2 operator -(Double2 a) => new Double2(-a.X, -a.Y);

        public static bool operator ==(Double2 a, Double2 b) => a.X == b.X && a.Y == b.Y;
        public static bool operator !=(Double2 a, Double2 b) => !(a == b);

        public double Dot(Double2 o) => X * o.X + Y * o.Y;
        public double Cross(Double2 o) => X * o.Y - Y * o.X;

        public Double2 WithX(double x) => new Double2(x, Y);
        public Double2 WithY(double y) => new Double2(X, y);
        public Double2 NegateX => new Double2(-X, Y);
        public Double2 NegateY => new Double2(X, -Y);

        public double LengthSquared => X * X + Y * Y;
        public double Length => Math.Sqrt(X * X + Y * Y);

        public double DistanceSquaredTo(Double2 o) => (o - this).LengthSquared;
        public double DistanceTo(Double2 o) => (o - this).Length;

        public Double2 Normalized => this / Length;

        public Double2 RotScale(Double2 u) => new Double2(X * u.X - Y * u.Y, X * u.Y + Y * u.X);
        public Double2 Rotate(Double2 u) => RotScale(u.Normalized);
        public Double2 Rotate(double a) => RotScale(FromAngle(a));
        public Double2 CCWPerpendicular => new Double2(-Y, X);

        public double Angle => Math.Atan2(Y, X);
        public double AngleFacing(Double2 u) => Math.Atan2(u.Y - Y, u.X - X);

        public static readonly Double2 Zero = new Double2(0, 0);
        public static Double2 FromAngle(double a) => new Double2(Math.Cos(a), Math.Sin(a));

        public double AngleBetween(Double2 v) => RotScale(v.NegateY).Angle;

        public override string ToString() => $"{X} {Y}";

        public override bool Equals(object obj) => obj is Double2 && this == (Double2)obj;
        public override int GetHashCode()
        {
            int hash1 = X.GetHashCode();
            int hash2 = Y.GetHashCode();

            return hash1 ^ (hash2 << 3) ^ (hash2 >> 29);
        }

        public static explicit operator Vector2(Double2 v) => new Vector2((float)v.X, (float)v.Y);
    }
}
