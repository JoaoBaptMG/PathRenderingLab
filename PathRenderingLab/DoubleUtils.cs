using System;
using System.Collections.Generic;

namespace PathRenderingLab
{
    public static class DoubleUtils
    {
        public const double Epsilon = 1d / 16384;
        public const double Epsilon2 = Epsilon * Epsilon;

        public static bool RoughlyZero(double a) => a > -Epsilon && a < Epsilon;
        public static bool RoughlyZero(Double2 a) => a.LengthSquared < Epsilon2;

        public static bool RoughlyEquals(double a, double b) => a - b < Epsilon && b - a < Epsilon;
        public static bool RoughlyEquals(Double2 a, Double2 b) => (a - b).LengthSquared < Epsilon2;

        public class RoughComparer : Comparer<double>
        {
            public override int Compare(double x, double y) => RoughlyEquals(x, y) ? 0 : x.CompareTo(y);
        }

        public static readonly RoughComparer Comparer = new RoughComparer();

        public const double Pi = Math.PI;
        public const double TwoPi = 2 * Pi;
        public const double Pi_2 = Pi / 2;
        public const double Pi_4 = Pi / 4;

        public static double WrapAngle(this double a) => a + TwoPi * Math.Round(-a / TwoPi);
        public static double WrapAngle360(this double a) => a - TwoPi * Math.Ceiling(a / TwoPi) + TwoPi;
        public static double ToDegrees(this double a) => a * 180 / Pi;
        public static double ToRadians(this double a) => a * Pi / 180;

        public static double Clamp(double x, double a, double b) => x <= a ? a : x >= b ? b : x;

        public static double[] MultiplesBetween(this double d, double a, double b)
        {
            int first = (int)Math.Ceiling(a / d);
            int second = (int)Math.Floor(b / d);

            double[] result = new double[second - first + 1];
            for (int i = 0; i < result.Length; i++)
                result[i] = d * (i + first);

            return result;
        }

        public static bool AngleLeftTo(double x, double y) => (x - y).WrapAngle() > 0;
        public static bool AngleRightTo(double x, double y) => (x - y).WrapAngle() < 0;
    }
}
