using PathRenderingLab.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathRenderingLab.SvgContents
{
    public enum TransformFunctionType { Matrix, Translate, Rotate, Scale, Skew, SkewX, SkewY }

    public struct TransformFunction
    {
        public readonly TransformFunctionType Type;
        private readonly double A, B, C, D, E, F;

        public Double2 Translation => new Double2(A, B);
        public Double2 ScaleFactor => new Double2(A, B);

        public double SkewAngleX => A;
        public double SkewAngleY => B;

        public double RotationAngle => A;
        public Double2 RotationCenter => new Double2(B, C);

        private TransformFunction(TransformFunctionType type, double a, double b = 0, double c = 0,
            double d = 0, double e = 0, double f = 0)
        {
            Type = type;
            A = a;
            B = b;
            C = c;
            D = d;
            E = e;
            F = f;
        }

        public static TransformFunction Matrix(DoubleMatrix m) =>
            new TransformFunction(TransformFunctionType.Matrix, m.A, m.B, m.C, m.D, m.E, m.F);

        public static TransformFunction Translate(double x, double y) => new TransformFunction(TransformFunctionType.Translate, x, y);

        public static TransformFunction Rotate(double angle) => new TransformFunction(TransformFunctionType.Rotate, angle);
        public static TransformFunction Rotate(double angle, Double2 center)
            => new TransformFunction(TransformFunctionType.Rotate, angle, center.X, center.Y);

        public static TransformFunction Scale(double x, double y) => new TransformFunction(TransformFunctionType.Scale, x, y);

        public static TransformFunction Skew(double x, double y) => new TransformFunction(TransformFunctionType.Skew, x, y);
        public static TransformFunction SkewX(double x) => new TransformFunction(TransformFunctionType.SkewX, x, 0);
        public static TransformFunction SkewY(double y) => new TransformFunction(TransformFunctionType.SkewY, 0, y);

        public DoubleMatrix ToMatrix()
        {
            switch (Type)
            {
                case TransformFunctionType.Matrix: return new DoubleMatrix(A, B, C, D, E, F);
                case TransformFunctionType.Translate: return DoubleMatrix.Translate(A, B);
                case TransformFunctionType.Rotate:
                    if (B == 0 && C == 0) return DoubleMatrix.Rotate(A, new Double2(B, C));
                    else return DoubleMatrix.Rotate(A);
                case TransformFunctionType.Scale: return DoubleMatrix.Scale(A, B);
                case TransformFunctionType.Skew:
                case TransformFunctionType.SkewX:
                case TransformFunctionType.SkewY:
                    return DoubleMatrix.Skew(A, B);
                default: throw new InvalidOperationException("Unrecognized transform function type!");
            }
        }

        public static explicit operator DoubleMatrix(TransformFunction tf) => tf.ToMatrix();
    }

    public static class TransformFunctionCollection
    {
        public static DoubleMatrix ToMatrix(this TransformFunction[] tfs) => tfs.Cast<DoubleMatrix>().Aggregate(DoubleMatrix.Apply);
        public static TransformFunction[] Concat(this TransformFunction[] tf1, TransformFunction[] tf2)
            => Enumerable.Concat(tf1, tf2).ToArray();
    }
}
