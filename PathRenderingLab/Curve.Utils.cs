using System;

namespace PathRenderingLab
{
    public partial class Curve
    {
        public Double2[] EnclosingPolygon
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return new[] { A, B };
                    case CurveType.QuadraticBezier: return new[] { A, B, C };
                    case CurveType.CubicBezier: return new[] { A, B, C, D };
                    case CurveType.EllipticArc: return EllipticArc_EnclosingPolygon();
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        public double EntryCurvature
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return 0;
                    case CurveType.QuadraticBezier: return 0.5 * (B - A).Cross(C - B) / Math.Pow((B - A).LengthSquared, 1.5);
                    case CurveType.CubicBezier: if (DoubleUtils.RoughlyEquals(A, B)) return 0;
                        return 2 * (B - A).Cross(C - B) / Math.Pow((B - A).LengthSquared, 1.5) / 3;
                    case CurveType.EllipticArc: return EllipticArc_Curvature(0);
                    default: throw new InvalidOperationException("Unrecognized type!");
                }
            }
        }

        public double ExitCurvature
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return 0;
                    case CurveType.QuadraticBezier: return 0.5 * (B - C).Cross(B - A) / Math.Pow((C - B).LengthSquared, 1.5);
                    case CurveType.CubicBezier: if (DoubleUtils.RoughlyEquals(C, D)) return 0;
                        return 2 * (C - D).Cross(C - B) / Math.Pow((D - C).LengthSquared, 1.5) / 3;
                    case CurveType.EllipticArc: return EllipticArc_Curvature(1);
                    default: throw new InvalidOperationException("Unrecognized type!");
                }
            }
        }

        private double EllipticArc_Curvature(double t)
            => Math.Sign(DeltaAngle) * Radii.X * Radii.Y / Math.Pow(DeltaAtInternal(t).LengthSquared, 1.5);

        /// <summary>
        /// Computes the unit tangent vector to the curve at point t
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public Double2 TangentAt(double t)
        {
            var d = Derivative.At(t);
            if (DoubleUtils.RoughlyZero(d)) return Derivative.TangentAt(t);
            return d.Normalized;
        }
    }
}
