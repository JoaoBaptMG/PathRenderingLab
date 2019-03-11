using System;
using System.Collections.Generic;

namespace PathRenderingLab.PathCompiler
{

    public partial class Curve
    {
        public static Curve Line(Double2 a, Double2 b) => new Curve(CurveType.Line, a, b);
        private Curve Line_Reverse => Line(B, A);

        private Double2 Line_At(double t) => (1 - t) * A + t * B;

        private Curve Line_Derivative => Line(B - A, B - A);

        private IEnumerable<Curve> Line_Simplify() { yield return this; }

        /// <summary>
        /// Transforms v using the transform that maps the line to (0,0)-(1,0)
        /// </summary>
        /// <param name="v">The vector to be transformed.</param>
        /// <returns>The transformed vector</returns>
        public Double2 ProjectiveTransform(Double2 v)
        {
            if (Type != CurveType.Line) throw new InvalidOperationException("Projective methods must be called on lines.");
            return ProjectiveTransformDirection(v - A);
        }

        /// <summary>
        /// Transforms v (as if it were a direction) using the transform that maps the line to (0,0)-(1,0)
        /// </summary>
        /// <param name="v">The vector to be transformed.</param>
        /// <returns>The transformed vector</returns>
        public Double2 ProjectiveTransformDirection(Double2 v)
        {
            if (Type != CurveType.Line) throw new InvalidOperationException("Projective methods must be called on lines.");
            var ba = B - A;
            return new Double2(ba.X * v.X + ba.Y * v.Y, -ba.Y * v.X + ba.X * v.Y) / ba.LengthSquared;
        }

        private Curve Line_Subcurve(double l, double r) => Line(A + l * (B - A), A + r * (B - A));

        private DoubleRectangle Line_BoundingBox
        {
            get
            {
                var x1 = Math.Min(A.X, B.X);
                var x2 = Math.Max(A.X, B.X);
                var y1 = Math.Min(A.Y, B.Y);
                var y2 = Math.Max(A.Y, B.Y);

                return new DoubleRectangle(x1, y1, x2 - x1, y2 - y1);
            }
        }

        private double Line_Winding => A.Cross(B);
        private bool Line_IsDegenerate => DoubleUtils.RoughlyEquals(A, B);

        private OuterAngles Line_OuterAngles => new OuterAngles(A.AngleFacing(B), 0, 0);

        private Double2 Line_EntryTangent => (B - A).Normalized;
        private Double2 Line_ExitTangent => (B - A).Normalized;

        private double Line_NearestPointTo(Double2 p) => DoubleUtils.Clamp((p - A).Dot(B - A) / (B - A).LengthSquared, 0, 1);

        private string Line_ToString() => $"L({A.X} {A.Y})-({B.X} {B.Y})";
    }
}