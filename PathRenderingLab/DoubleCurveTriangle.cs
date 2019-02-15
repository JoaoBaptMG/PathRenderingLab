namespace PathRenderingLab
{
    public struct DoubleCurveTriangle
    {
        public readonly DoubleCurveVertex A, B, C;

        public DoubleCurveTriangle(DoubleCurveVertex a, DoubleCurveVertex b, DoubleCurveVertex c)
        {
            bool cw = (b.Position - a.Position).Cross(c.Position - a.Position) < 0;

            A = a;
            B = cw ? c : b;
            C = cw ? b : c;
        }

        public bool IsDegenerate => DoubleUtils.RoughlyZero((B.Position - A.Position).Cross(C.Position - A.Position));

        public override string ToString() => $"{A} / {B} / {C}";
    }
}