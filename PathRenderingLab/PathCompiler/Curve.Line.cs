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

        private Double2[] Line_EnclosingPolygon => new[] { A, B };

        private double[] Line_IntersectionsWithVerticalLine(double x) => A.X == B.X ? new double[0] : new[] { (x - A.X) / (B.X - A.X) };

        private double[] Line_IntersectionsWithHorizontalLine(double y) => A.Y == B.Y ? new double[0] : new[] { (y - A.Y) / (B.Y - A.Y) };

        private double[] Line_IntersectionsWithSegment(Double2 v1, Double2 v2)
        {
            var k = (v2 - v1).Cross(B - A);
            if (DoubleUtils.RoughlyZeroSquared(k)) return new double[0];
            return new[] { (v2 - v1).Cross(v1 - A) / k };
        }

        private string Line_ToString() => $"L({A.X} {A.Y})-({B.X} {B.Y})";
    }
}