using System;
using System.Collections.Generic;

namespace PathRenderingLab
{
    public static class DoubleUtils
    {
        public const double Epsilon = 1d / 32768;
        public const double Epsilon2 = Epsilon * Epsilon;

        public static bool RoughlyZero(double a) => a > -Epsilon && a < Epsilon;
        public static bool RoughlyZeroSquared(double a) => a > -Epsilon2 && a < Epsilon2;
        public static bool RoughlyZero(Double2 a) => RoughlyZeroSquared(a.LengthSquared);

        public static bool RoughlyEquals(double a, double b) => a - b < Epsilon && b - a < Epsilon;
        public static bool RoughlyEqualsSquared(double a, double b) => a - b < Epsilon2 && b - a < Epsilon2;
        public static bool RoughlyEquals(Double2 a, Double2 b) => RoughlyZeroSquared((a - b).LengthSquared);

        public class RoughComparerInst : Comparer<double>
        {
            public override int Compare(double x, double y) => RoughlyEquals(x, y) ? 0 : x.CompareTo(y);
        }

        public static readonly RoughComparerInst RoughComparer = new RoughComparerInst();

        public class RoughComparerSquaredInst : Comparer<double>
        {
            public override int Compare(double x, double y) => RoughlyEqualsSquared(x, y) ? 0 : x.CompareTo(y);
        }

        public static readonly RoughComparerSquaredInst RoughComparerSquared = new RoughComparerSquaredInst();

        public const double Pi = Math.PI;
        public const double TwoPi = 2 * Pi;
        public const double Pi_2 = Pi / 2;
        public const double Pi_4 = Pi / 4;

        public static double WrapAngle(this double a) => a + TwoPi * Math.Round(-a / TwoPi);
        public static double WrapAngle360(this double a) => a - TwoPi * Math.Ceiling(a / TwoPi) + TwoPi;
        public static double WrapAngle360(this double v, bool ccw) => (ccw ? 1 : -1) * WrapAngle360(ccw ? v : -v);
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

        public static void RemoveDuplicatedValues(this List<double> list)
        {
            list.Sort(RoughComparer);
            var temp = new List<double>() { list[0] };

            for (int i = 1; i < list.Count; i++)
                if (RoughComparer.Compare(list[i - 1], list[i]) != 0) temp.Add(list[i]);
                else temp[temp.Count - 1] = (temp[temp.Count - 1] + list[i]) / 2;

            list.Clear();
            list.AddRange(temp);
        }

        public static double Truncate(this double x) => Epsilon * Math.Round(x / Epsilon);

        public static double? TryParse(string str)
            => double.TryParse(str, out var result) ? result : new double?();

        public static double NextPowerOfTwo(double x)
        {
            // Bail out if zero
            if (x == 0) return 1;

            // If it's NaN or negative, throw
            if (x < 0 || double.IsNaN(x)) throw new ArgumentException("NaNs and negative numbers are not allowed!", nameof(x));
            // If it's infinity, return itself
            if (double.IsInfinity(x)) return x;

            // Get the double's bits
            var bits = BitConverter.DoubleToInt64Bits(x);

            // If the exponent is negative, return 1
            if (bits < (1023L << 52)) return 1;

            // If the mantissa is not clear, augment the exponent
            if ((bits & 0xFFFFFFFFFFFFF) != 0) bits += 1L << 52;

            // Clear the mantissa and return it
            bits &= ~0xFFFFFFFFFFFFF;

            return BitConverter.Int64BitsToDouble(bits);
        }
    }
}
