using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;

namespace PathRenderingLab
{
    internal class Equations
    {
        static bool FinelyZero(double x) => Math.Abs(x) <= 1E-12;
        static bool FinelyEquals(double x, double y) => FinelyZero(x - y);

        /// <summary>
        /// Solves a quadratic equation.
        /// </summary>
        /// <param name="a">The quadratic term</param>
        /// <param name="b">The linear term</param>
        /// <param name="c">The constant term</param>
        /// <param name="mult">Whether the root list should include multiple roots multiple times or not</param>
        /// <returns>The real roots of the equation.</returns>
        internal static double[] SolveQuadratic(double a, double b, double c)
        {
            if (FinelyZero(a)) return FinelyZero(b) ? new double[0] : new[] { -c / b };

            double delta = b * b - 4 * a * c;

            if (delta <= double.Epsilon && delta >= -double.Epsilon) return new[] { -b / (2 * a), -b / (2 * a) };
            else if (delta < -double.Epsilon) return new double[] { };
            else
            {
                var dsr = b >= 0 ? Math.Sqrt(delta) : -Math.Sqrt(delta);
                var q = -0.5 * (b + dsr);

                var x1 = q / a;
                var x2 = c / q;

                if (x1 < x2) return new[] { x1, x2 };
                else return new[] { x2, x1 };
            }
        }

        internal static double Cbrt(double x) => x < 0 ? -Math.Pow(-x, 1.0 / 3.0) : Math.Pow(x, 1.0 / 3.0);

        /// <summary>
        /// Solves a cubic equation.
        /// </summary>
        /// <param name="a">The cubic term</param>
        /// <param name="b">The quadratic term</param>
        /// <param name="c">The linear term</param>
        /// <param name="d">The constant term</param>
        /// <param name="mult">Whether the root list should include multiple roots multiple times or not</param>
        /// <returns>The real roots of the equation.</returns>
        internal static double[] SolveCubic(double c3, double c2, double c1, double c0)
        {
            if (FinelyZero(c3)) return SolveQuadratic(c2, c1, c0);

            // Ah, Cardano's long-winded method... let's go
            var a = c2 / c3;
            var b = c1 / c3;
            var c = c0 / c3;

            // First, compute the depressing terms
            double p = b - a * a / 3;
            double q = 2 * a * a * a / 27 - a * b / 3 + c;

            // Discriminant
            double D = q * q / 4 + p * p * p / 27;

            // first case: D is positive
            if (D > double.Epsilon)
            {
                // Calculate u0 and v0, very similarly to a quadratic
                double dsr = q >= 0 ? Math.Sqrt(D) : -Math.Sqrt(D);

                double u0 = -Cbrt(q / 2 + dsr);
                double v0 = -p / (3 * u0);

                // The only real root is u0+v0-a/3
                return new[] { u0 + v0 - a / 3 };
            }
            // second case: D = 0
            else if (D >= -double.Epsilon)
            {
                double uv0 = Cbrt(-q / 2);

                // Two solutions (one double root)
                return new[] { -uv0 - a / 3, -uv0 - a / 3, 2 * uv0 - a / 3 };
            }
            // third case: D is negative
            else
            {
                // radius and angle of the complex roots
                double r = Math.Sqrt(-p / 3);
                double alpha = Math.Atan2(2 * Math.Sqrt(-D), -q);

                // Three solutions
                return new[]
                {
                    r * (Math.Cos(alpha / 3) + Math.Cos((6 * Pi - alpha) / 3)) - a / 3,
                    r * (Math.Cos((2 * Pi + alpha) / 3) + Math.Cos((4 * Pi - alpha) / 3)) - a / 3,
                    r * (Math.Cos((4 * Pi + alpha) / 3) + Math.Cos((2 * Pi - alpha) / 3)) - a / 3
                };
            }
        }

        /// <summary>
        /// Finds a root of a function through the bisection method.
        /// </summary>
        /// <param name="f">The function to be tested. Must be continuous.</param>
        /// <param name="tl">The left element of the interval.</param>
        /// <param name="tr">The right element of the interval.</param>
        /// <returns>The root found, or the element supposedly nearest the root.</returns>
        internal static double Bisection(Func<double, double> f, double tl, double tr)
        {
            if (f(tl) > 0 && f(tr) > 0)
                return f(tl) > f(tr) ? tr : tl;
            else if (f(tl) < 0 && f(tr) < 0)
                return f(tl) > f(tr) ? tl : tr;

            while (!FinelyEquals(tl, tr))
            {
                var tm = tl + (tr - tl) / 2;
                if (f(tm) == 0) return tm;

                if (f(tm) > 0)
                {
                    if (f(tl) > 0) tl = tm;
                    else tr = tm;
                }
                else
                {
                    if (f(tr) > 0) tl = tm;
                    else tr = tm;
                }
            }

            return tl + (tr - tl) / 2;
        }

        internal static double[] PolynomialDerivative(double[] a)
        {
            int l = a.Length;
            var b = new double[l - 1];
            for (int i = 0; i < l - 1; i++) b[i] = (l - i - 1) * a[i];
            return b;
        }

        internal static double[] SolvePolynomial(params double[] a)
        {
            // Shortcuts
            if (a.Length == 1) return a[0] == 0 ? new double[] { 0 } : new double[0];
            if (a.Length == 2) return a[0] == 0 ? new double[0] : new[] { -a[1] / a[0] };
            if (a.Length == 3) return SolveQuadratic(a[0], a[1], a[2]);
            if (a.Length == 4) return SolveCubic(a[0], a[1], a[2], a[3]);
            if (a[0] == 0) return SolvePolynomial(a.Skip(1).ToArray());

            int l = a.Length;

            // Make the leading coefficient of the polynomial positive, if necessary
            if (a[0] < 0) for (int i = 0; i < l; i++) a[i] = -a[i];

            // Polynomial evaluation
            double Evaluate(double t)
            {
                double r = a[0];
                for (int i = 1; i < l; i++)
                    r = r * t + a[i];
                return r;
            }

            // Find the derivative's roots to calculate the maxima and minima and sort them
            var extrema = SolvePolynomial(PolynomialDerivative(a)).ToList();
            extrema.Sort();

            // If there are no extrema, add a dummy one
            if (extrema.Count == 0) extrema.Add(0);

            // Add possible test points for the binary search interval
            // To the left
            if (l % 2 == 0 ? Evaluate(extrema[0]) >= 0 : Evaluate(extrema[0]) <= 0)
            {
                double k = 1;
                double guess = extrema[0];

                while (l % 2 == 0 ? Evaluate(guess) >= 0 : Evaluate(guess) <= 0)
                {
                    guess -= k;
                    k *= 2;
                }

                extrema.Insert(0, guess);
            }

            // And to the right
            if (Evaluate(extrema[extrema.Count - 1]) <= 0)
            {
                double k = 1;
                double guess = extrema[extrema.Count - 1];

                while (Evaluate(guess) <= 0)
                {
                    guess += k;
                    k *= 2;
                }

                extrema.Add(guess);
            }

            // Finally, find the roots
            var roots = new List<double>(extrema.Count - 1);
            for (int i = 1; i < extrema.Count; i++)
            {
                var p0 = Evaluate(extrema[i - 1]);
                var p1 = Evaluate(extrema[i]);

                // Account for roots on the extrema (this needs to be implemented like this to correctly handle multiple roots)
                if (Math.Abs(p1) < 1E-12) roots.Add(extrema[i]);
                else if (Math.Abs(p0) < 1E-12) roots.Add(extrema[i - 1]);
                // Otherwise, they must have the opposite signal
                else if ((p0 > 0 && p1 < 0) || (p0 < 0 && p1 > 0))
                    roots.Add(Bisection(Evaluate, extrema[i - 1], extrema[i]));
            }

            return roots.ToArray();
        }

        internal struct LinearSystemData
        {
            public readonly double[] ConstantTerm;
            public readonly double[][] LinearTerms;

            public LinearSystemData(double[] c, double[][] l)
            {
                ConstantTerm = c;
                LinearTerms = l;
            }
        }

        internal static LinearSystemData? SolveLinearSystem(double[][] equations, double[] constants)
        {
            // First, compare the lengths
            if (equations.Length != constants.Length)
                throw new InvalidOperationException("There must be the same number of equations and constants!");

            // Now, guarantee at least one equation
            if (equations.Length == 0)
                throw new InvalidOperationException("There must be at least one equation!");

            // Finally, guarantee all the equations have the same size
            var unknownNumber = equations[0].Length;
            if (!equations.All(e => e.Length == unknownNumber))
                throw new InvalidOperationException("All equations must have the same size!");

            var equationNumber = equations.Length;

            // Create a copy of both arrays in order to preserve the coefficients
            var equationsC = new double[equationNumber][];
            for (int i = 0; i < equationNumber; i++)
            {
                equationsC[i] = new double[unknownNumber];
                equations[i].CopyTo(equationsC[i], 0);
            }
            var constantsC = new double[equationNumber];
            constants.CopyTo(constantsC, 0);

            // Track the pivot and non-pivot columns
            var pivots = new List<int>();
            var nonPivots = new List<int>();

            // Convert the coefficient matrix to row-echelon form
            int h, k;
            for (h = 0, k = 0; h < equationNumber && k < unknownNumber; k++)
            {
                // Find the maximum pivot of the current column
                int im = h;
                for (int i = h + 1; i < equationNumber; i++)
                    if (Math.Abs(equationsC[i][k]) > Math.Abs(equationsC[im][k])) im = i;

                // If it is zero, skip to the next column
                if (equationsC[im][k] == 0)
                {
                    nonPivots.Add(k);
                    continue;
                }

                // Swap the current row with the pivot row
                Swap(ref equationsC[im], ref equationsC[h]);
                Swap(ref constantsC[im], ref constantsC[h]);

                // Reduce the pivot row, first
                var pivot = equationsC[h][k];
                for (int j = k; j < unknownNumber; j++)
                    equationsC[h][j] /= pivot;
                constantsC[h] /= pivot;

                // Now, apply row redution for every row under the current row
                for (int i = h + 1; i < equationNumber; i++)
                {
                    // The pivot is guaranteed to be 1
                    var f = equationsC[i][k];
                    equationsC[i][k] = 0;

                    for (int j = k + 1; j < unknownNumber; j++)
                        equationsC[i][j] -= f * equationsC[h][j];

                    constantsC[i] -= f * constantsC[h];
                }

                pivots.Add(k);
                // Now, work with the next row
                h++;
            }
            nonPivots.AddRange(Enumerable.Range(k, unknownNumber - k));

            // Now, do the reverse to convert the matrix to reduced row echelon form
            for (h--; h >= 0; h--)
            {
                // Find the pivot
                for (k = 0; k < unknownNumber; k++)
                    if (equationsC[h][k] != 0) break;

                // Multiply every row above to eliminate the upper coefficients
                for (int i = 0; i < h; i++)
                {
                    var f = equationsC[i][k];
                    for (int j = 0; j < unknownNumber; j++)
                        equationsC[i][j] -= f * equationsC[h][j];

                    constantsC[i] -= f * constantsC[h];
                }
            }

            // Check if the system is impossible. This happens if any row has equations zero and constant nonzero
            for (int i = 0; i < equationNumber; i++)
                if (equationsC[i].All(x => x == 0) && constantsC[i] != 0)
                    return null;

            // The matrix being in reduced row echelon form, create the resolution data
            var cterm = new double[unknownNumber];
            for (int i = 0; i < pivots.Count; i++)
                cterm[pivots[i]] = constantsC[i];

            var lterms = new List<double[]>();
            foreach (var j in nonPivots)
            {
                var lterm = new double[unknownNumber];
                lterm[j] = -1;

                for (int i = 0; i < pivots.Count; i++)
                    lterm[pivots[i]] = equationsC[i][j];

                lterms.Add(lterm);
            }

            return new LinearSystemData(cterm, lterms.ToArray());
        }

        public static void Swap<T>(ref T lhs, ref T rhs)
        {
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }
    }
}