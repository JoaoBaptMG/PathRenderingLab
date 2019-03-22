using System;
using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab.PathCompiler
{
    public enum CurveType { Line, QuadraticBezier, CubicBezier, EllipticArc }

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

        public double WindingAtMidpoint => WindingRelativeTo((At(0) + At(1)) / 2);

        public bool IsConvex => WindingAtMidpoint > 0;

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

        Double2[] cachedEnclosingPolygon;

        public Double2[] EnclosingPolygon
        {
            get
            {
                if (cachedEnclosingPolygon == null)
                {
                    switch (Type)
                    {
                        case CurveType.Line: cachedEnclosingPolygon = Line_EnclosingPolygon; break;
                        case CurveType.QuadraticBezier: cachedEnclosingPolygon = QuadraticBezier_EnclosingPolygon; break;
                        case CurveType.CubicBezier: cachedEnclosingPolygon = CubicBezier_EnclosingPolygon; break;
                        case CurveType.EllipticArc: cachedEnclosingPolygon = EllipticArc_EnclosingPolygon; break;
                        default: throw new InvalidOperationException("Unrecognized type.");
                    }
                }

                return cachedEnclosingPolygon;
            }
        }

        public CurveVertex[] CurveVertices
        {
            get
            {
                switch (Type)
                {
                    case CurveType.Line: return new CurveVertex[0];
                    case CurveType.QuadraticBezier: return QuadraticBezier_CurveVertices;
                    case CurveType.CubicBezier: return CubicBezier_CurveVertices;
                    case CurveType.EllipticArc: return EllipticArc_CurveVertices;
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
                    case CurveType.QuadraticBezier: return QuadraticBezier_EntryCurvature;
                    case CurveType.CubicBezier: return CubicBezier_EntryCurvature;
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
                    case CurveType.QuadraticBezier: return QuadraticBezier_ExitCurvature;
                    case CurveType.CubicBezier: return CubicBezier_ExitCurvature;
                    case CurveType.EllipticArc: return EllipticArc_Curvature(1);
                    default: throw new InvalidOperationException("Unrecognized type!");
                }
            }
        }

        public double[] IntersectionsWithVerticalLine(double x)
        {
            switch (Type)
            {
                case CurveType.Line: return Line_IntersectionsWithVerticalLine(x);
                case CurveType.QuadraticBezier: return QuadraticBezier_IntersectionsWithVerticalLine(x);
                case CurveType.CubicBezier: return CubicBezier_IntersectionsWithVerticalLine(x);
                case CurveType.EllipticArc: return EllipticArc_IntersectionsWithVerticalLine(x);
                default: throw new InvalidOperationException("Unrecognized type!");
            }
        }

        public double[] IntersectionsWithHorizontalLine(double y)
        {
            switch (Type)
            {
                case CurveType.Line: return Line_IntersectionsWithHorizontalLine(y);
                case CurveType.QuadraticBezier: return QuadraticBezier_IntersectionsWithHorizontalLine(y);
                case CurveType.CubicBezier: return CubicBezier_IntersectionsWithHorizontalLine(y);
                case CurveType.EllipticArc: return EllipticArc_IntersectionsWithHorizontalLine(y);
                default: throw new InvalidOperationException("Unrecognized type!");
            }
        }

        public double[] IntersectionsWithSegment(Double2 v1, Double2 v2)
        {
            switch (Type)
            {
                case CurveType.Line: return Line_IntersectionsWithSegment(v1, v2);
                case CurveType.QuadraticBezier: return QuadraticBezier_IntersectionsWithSegment(v1, v2);
                case CurveType.CubicBezier: return CubicBezier_IntersectionsWithSegment(v1, v2);
                case CurveType.EllipticArc: return EllipticArc_IntersectionsWithSegment(v1, v2);
                default: throw new InvalidOperationException("Unrecognized type!");
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
                default: return "<<invalid>>";
            }
        }

        public string PathRepresentation() => PathCommand.MoveTo(At(0)).ToString() + " " + PathCommandFrom().ToString();
    }
}
