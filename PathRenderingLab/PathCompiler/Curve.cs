using System;
using System.Collections.Generic;

namespace PathRenderingLab.PathCompiler
{
    public enum CurveType { Line, QuadraticBezier, CubicBezier, EllipticArc }

    public struct OuterAngles : IComparable<OuterAngles>
    {
        public double Angle, DAngle, DDAngle;

        public OuterAngles(double angle, double dAngle, double ddAngle)
        {
            Angle = angle;
            DAngle = dAngle;
            DDAngle = ddAngle;
        }

        public int CompareTo(OuterAngles other)
        {
            var comparer = DoubleUtils.RoughComparer;
            var cmp = comparer.Compare(Angle, other.Angle);
            if (cmp == 0) cmp = comparer.Compare(DAngle, other.DAngle);
            if (cmp == 0) cmp = comparer.Compare(DDAngle, other.DDAngle);
            return cmp;
        }

        public override string ToString() => $"{Angle.ToDegrees()}:{DAngle}:{DDAngle}";
    }

    /// <summary>
    /// A variant struct describing an SVG curve, which can be a line, a quadratic or cubic Bézier curve, or an elliptic arc
    /// </summary>
    public partial class Curve
    {
        /// <summary>
        /// The curve's type
        /// </summary>
        public readonly CurveType Type;
        public readonly Double2 A, B, C, D;

        private Curve(CurveType type, Double2 a, Double2 b, Double2 c, Double2 d)
        {
            Type = type;
            A = a;
            B = b;
            C = c;
            D = d;
        }

        private Curve(CurveType type, Double2 a, Double2 b, Double2 c)
            : this(type, a, b, c, Double2.Zero) { }

        private Curve(CurveType type, Double2 a, Double2 b)
            : this(type, a, b, Double2.Zero) { }

        /// <summary>
        /// Evaluates the arc segment at point t
        /// </summary>
        /// <param name="t">The interpolation value</param>
        /// <returns>The point requested</returns>
        public Double2 At(double t)
        {
            switch (Type)
            {
                case CurveType.Line: return Line_At(t);
                case CurveType.QuadraticBezier: return QuadraticBezier_At(t);
                case CurveType.CubicBezier: return CubicBezier_At(t);
                case CurveType.EllipticArc: return EllipticArc_At(t);
                default: throw new InvalidOperationException("Unrecognized type.");
            }
        }

        /// <summary>
        /// Returns the derivative curve of this curve
        /// </summary>
        public Curve Derivative
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return Line_Derivative;
                    case CurveType.QuadraticBezier: return QuadraticBezier_Derivative;
                    case CurveType.CubicBezier: return CubicBezier_Derivative;
                    case CurveType.EllipticArc: return EllipticArc_Derivative;
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        /// <summary>
        /// Calculates the subcurve that spans the interval [l,r], remapping it to [0,1]. The underlying curve is the same.
        /// </summary>
        /// <param name="l">The left parameter</param>
        /// <param name="r">The right parameter</param>
        /// <returns>The subcurve returned from remapping the interval [l,r] to [0,1]</returns>
        public Curve Subcurve(double l, double r)
        {
            if (l == 0 && r == 1) return this;

            switch (Type)
            {
                case CurveType.Line: return Line_Subcurve(l, r);
                case CurveType.QuadraticBezier: return QuadraticBezier_Subcurve(l, r);
                case CurveType.CubicBezier: return CubicBezier_Subcurve(l, r);
                case CurveType.EllipticArc: return EllipticArc_Subcurve(l, r);
                default: throw new InvalidOperationException("Unrecognized type.");
            }
        }

        /// <summary>
        /// Reverts the curve
        /// </summary>
        public Curve Reverse
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return Line_Reverse;
                    case CurveType.QuadraticBezier: return QuadraticBezier_Reverse;
                    case CurveType.CubicBezier: return CubicBezier_Reverse;
                    case CurveType.EllipticArc: return EllipticArc_Reverse;
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        /// <summary>
        /// Simplify the curve if it degenerates to other, simpler curves;
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Curve> Simplify(bool split = false)
        {
            if (IsDegenerate) return new Curve[] { };

            switch (Type)
            {
                case CurveType.Line: return Line_Simplify();
                case CurveType.QuadraticBezier: return QuadraticBezier_Simplify();
                case CurveType.CubicBezier: return CubicBezier_Simplify(split);
                case CurveType.EllipticArc: return EllipticArc_Simplify();
                default: throw new InvalidOperationException("Unrecognized type.");
            }
        }

        DoubleRectangle? cachedBoundingBox;

        /// <summary>
        /// The curve's bounding box
        /// </summary>
        public DoubleRectangle BoundingBox
        {
            get
            {
                if (!cachedBoundingBox.HasValue)
                {
                    switch (Type)
                    {
                        case CurveType.Line: cachedBoundingBox = Line_BoundingBox; break;
                        case CurveType.QuadraticBezier: cachedBoundingBox = QuadraticBezier_BoundingBox; break;
                        case CurveType.CubicBezier: cachedBoundingBox = CubicBezier_BoundingBox; break;
                        case CurveType.EllipticArc: cachedBoundingBox = EllipticArc_BoundingBox; break;
                        default: throw new InvalidOperationException("Unrecognized type.");
                    }
                }

                return cachedBoundingBox.Value;
            }
        }

        /// <summary>
        /// The particular vertex winding - a measure of how much counterclockwise it is.
        /// It is basically the signed area betwen the curve and two lines that go from the origin to the endpoints.
        /// </summary>
        public double Winding
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return Line_Winding;
                    case CurveType.QuadraticBezier: return QuadraticBezier_Winding;
                    case CurveType.CubicBezier: return CubicBezier_Winding;
                    case CurveType.EllipticArc: return EllipticArc_Winding;
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        public double WindingRelativeTo(Double2 v) => Winding - v.Cross(At(1) - At(0));

        public bool IsConvex => WindingRelativeTo((At(0) + At(1)) / 2) > 0;

        /// <summary>
        /// True if the curve is degenerate (that is, if it is represented by a single point)
        /// </summary>
        public bool IsDegenerate
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return Line_IsDegenerate;
                    case CurveType.QuadraticBezier: return QuadraticBezier_IsDegenerate;
                    case CurveType.CubicBezier: return CubicBezier_IsDegenerate;
                    case CurveType.EllipticArc: return EllipticArc_IsDegenerate;
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        /// <summary>
        /// The curve's outer angles (angle, derivative and second derivative)
        /// </summary>
        public OuterAngles OuterAngles
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return Line_OuterAngles;
                    case CurveType.QuadraticBezier: return QuadraticBezier_OuterAngles;
                    case CurveType.CubicBezier: return CubicBezier_OuterAngles;
                    case CurveType.EllipticArc: return EllipticArc_OuterAngles;
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        public Double2 EntryTangent
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return Line_EntryTangent;
                    case CurveType.QuadraticBezier: return QuadraticBezier_EntryTangent;
                    case CurveType.CubicBezier: return CubicBezier_EntryTangent;
                    case CurveType.EllipticArc: return EllipticArc_EntryTangent;
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        public Double2 ExitTangent
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return Line_ExitTangent;
                    case CurveType.QuadraticBezier: return QuadraticBezier_ExitTangent;
                    case CurveType.CubicBezier: return CubicBezier_ExitTangent;
                    case CurveType.EllipticArc: return EllipticArc_ExitTangent;
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        public double NearestPointTo(Double2 p)
        {
            switch (Type)
            {
                case CurveType.Line: return Line_NearestPointTo(p);
                case CurveType.QuadraticBezier: return QuadraticBezier_NearestPointTo(p);
                case CurveType.CubicBezier: return CubicBezier_NearestPointTo(p);
                case CurveType.EllipticArc: return EllipticArc_NearestPointTo(p);
                default: throw new InvalidOperationException("Unrecognized type.");
            }
        }

        public Curve SubcurveCorrectEndpoints(KeyValuePair<double, Double2> l, KeyValuePair<double, Double2> r)
        {
            if (l.Key == 0 && r.Key == 1) return this;
            return Subcurve(l.Key, r.Key).CorrectEndpoints(l.Value, r.Value);
        }

        public Curve CorrectEndpoints(Double2 p0, Double2 p1)
        {
            switch (Type)
            {
                case CurveType.Line: return Line(p0, p1);
                case CurveType.QuadraticBezier: return QuadraticBezier(p0, B, p1);
                case CurveType.CubicBezier: return CubicBezier(p0, B, C, p1);
                case CurveType.EllipticArc:
                    return EllipticArc(p0, Radii, ComplexRot.Angle, Math.Abs(DeltaAngle) > DoubleUtils.Pi, DeltaAngle >= 0, p1);
                default: throw new InvalidOperationException("Unrecognized type.");
            }
        }

        public PathCommand PathCommandFrom()
        {
            switch (Type)
            {
                case CurveType.Line: return PathCommand.LineTo(B);
                case CurveType.QuadraticBezier: return PathCommand.QuadraticCurveTo(B, C);
                case CurveType.CubicBezier: return PathCommand.CubicCurveTo(B, C, D);
                case CurveType.EllipticArc:
                    return PathCommand.ArcTo(Radii, ComplexRot.Angle, Math.Abs(DeltaAngle) > DoubleUtils.Pi, DeltaAngle > 0, At(1));
                default: return PathCommand.None;
            }
        }

        public CurveVertex[] CurveVertices
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return new CurveVertex[0];
                    case CurveType.QuadraticBezier: return QuadraticBezier_CurveVertices();
                    case CurveType.CubicBezier: return CubicBezier_CurveVertices();
                    case CurveType.EllipticArc: return EllipticArc_CurveVertices();
                    default: throw new InvalidOperationException("Unrecognized type.");
                }
            }
        }

        public override string ToString()
        {
            switch (Type)
            {
                case CurveType.Line: return Line_ToString();
                case CurveType.QuadraticBezier: return QuadraticBezier_ToString();
                case CurveType.CubicBezier: return CubicBezier_ToString();
                case CurveType.EllipticArc: return EllipticArc_ToString();
                default: throw new InvalidOperationException("Unrecognized type.");
            }
        }

        public string PathRepresentation() => PathCommand.MoveTo(At(0)).ToString() + " " + PathCommandFrom().ToString();
    }
}
