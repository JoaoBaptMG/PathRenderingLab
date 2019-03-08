using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathRenderingLab
{
    public struct DoubleMatrix
    {
        public double A, B, C, D, E, F;

        public DoubleMatrix(double a, double b, double c, double d, double e, double f)
        {
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
        }

        public static readonly DoubleMatrix Identity = new DoubleMatrix(1, 0, 0, 1, 0, 0);

        public static DoubleMatrix Translate(double x, double y) => new DoubleMatrix(1, 0, 0, 1, x, y);
        public static DoubleMatrix Translate(Double2 pos) => Translate(pos.X, pos.Y);

        public static DoubleMatrix Scale(double x, double y) => new DoubleMatrix(x, 0, 0, y, 0, 0);
        public static DoubleMatrix Scale(Double2 amount) => Scale(amount.X, amount.Y);
        public static DoubleMatrix Scale(double s) => Scale(s, s);

        public static DoubleMatrix Rotate(double angle)
        {
            var rot = Double2.FromAngle(angle);
            return new DoubleMatrix(rot.X, rot.Y, -rot.Y, rot.X, 0, 0);
        }

        public static DoubleMatrix Rotate(double angle, Double2 center)
            => Translate(center) * Rotate(angle) * Translate(-center);

        public static DoubleMatrix Skew(double angleX, double angleY)
            => new DoubleMatrix(1, Math.Tan(angleY), Math.Tan(angleX), 1, 0, 0);

        public static DoubleMatrix operator *(DoubleMatrix m1, DoubleMatrix m2)
            => new DoubleMatrix(
                m1.A * m2.A + m1.C * m2.B,
                m1.B * m2.A + m1.D * m2.B,
                m1.A * m2.C + m1.C * m2.D,
                m1.B * m2.C + m1.D * m2.D,
                m1.A * m2.E + m1.C * m2.F + m1.E,
                m1.B * m2.E + m2.D * m2.F + m1.F);

        public static Double2 operator *(DoubleMatrix m, Double2 v) => m.Apply(v);
            
        public DoubleMatrix Apply(DoubleMatrix o) => this * o;
        public Double2 Apply(Double2 v) => new Double2(A * v.X + C * v.Y + E, B * v.X + D * v.Y + F);

        public static DoubleMatrix Apply(DoubleMatrix m1, DoubleMatrix m2) => m1 * m2;
        public static Double2 Apply(DoubleMatrix m, Double2 v) => m * v;

        public override string ToString() => $"[{A} {B} {C} {D} {E} {F}]";
    }
}
