using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using static PathRenderingLab.DoubleUtils;
using static PathRenderingLab.GeometricUtils;

namespace PathRenderingLab.PathCompiler
{
    public partial class Curve
    {
        public static IEnumerable<RootPair> Intersections(Curve c1, Curve c2)
        {
            if (c1.Type == CurveType.Line && c2.Type == CurveType.Line)
                return LineLineIntersections(c1, c2);
            return GeneralCurveIntersections(c1, c2, 0, 1, 0, 1);
        }

        static IEnumerable<RootPair> LineLineIntersections(Curve l1, Curve l2)
        {
            // Check if both lines subdivide
            var p = l1.A;
            var q = l2.A;
            var r = l1.B - l1.A;
            var s = l2.B - l2.A;

            var rr = r.Normalized;
            var ss = s.Normalized;

            // Intersection measure
            var k = r.Cross(s);
            var kk = rr.Cross(ss);

            // If they have the same direction
            if (RoughlyZeroSquared(kk))
            {
                var rs = rr.Dot(ss) > 0 ? (rr + ss).Normalized : (rr - ss).Normalized;

                // Check if they are collinear
                if (RoughlyZeroSquared((q - p).Cross(rs)))
                {
                    var tab0 = (q - p).Dot(r) / r.LengthSquared;
                    var tab1 = tab0 + s.Dot(r) / r.LengthSquared;

                    var tba0 = (p - q).Dot(s) / s.LengthSquared;
                    var tba1 = tba0 + r.Dot(s) / s.LengthSquared;

                    // If there is no intersection
                    if (1 <= Math.Min(tab0, tab1) || 0 >= Math.Max(tab0, tab1)) return Enumerable.Empty<RootPair>();
                    // Assemble the lines accordingly (tedious cases...)
                    else
                    {
                        if (0 <= tab0 && tab0 <= 1) // l1.A -- l2.A -- l1.B, with l2.B elsewhere
                        {
                            if (tab1 > 1) // l2.B to the right of l1
                                return new[] { new RootPair(tab0, 0f), new RootPair(1f, tba1) };
                            else if (tab1 < 0) // l2.B to the left of l1
                                return new[] { new RootPair(tab0, 0f), new RootPair(0f, tba0) };
                            else // l2 inside l1
                                return new[] { new RootPair(tab0, 0f), new RootPair(tab1, 1f) };
                        }
                        else if (0 <= tab1 && tab1 <= 1) // l1.A -- l2.B -- l1.B, with l2.A elsewhere
                        {
                            if (tab0 < 0) // l2.A to the left of l1
                                return new[] { new RootPair(0f, tba0), new RootPair(tab1, 1f) };
                            else // l2.A to the right of l1
                                return new[] { new RootPair(1f, tba1), new RootPair(tab1, 1f) };
                        }
                        else // l1 inside l2
                            return new[] { new RootPair(0f, tba0), new RootPair(1f, tba1) };
                    }
                }
                else return new RootPair[0];
            }
            // If they don't, calculate t and u
            else
            {
                var t = (q - p).Cross(s) / k;
                var u = (q - p).Cross(r) / k;

                // If both lie at the interval [0, 1], then:
                if (Inside01(t) && Inside01(u)) return new[] { new RootPair(t, u) };
                // Else, no intersection
                else return new RootPair[0];
            }
        }

        static string RectanglePath(DoubleRectangle r) => $"M {r.X},{r.Y} h{r.Width} v{r.Height} h{-r.Width} Z";

        static IEnumerable<RootPair> GeneralCurveIntersections(Curve c1, Curve c2, double t1l, double t1r, double t2l, double t2r)
        {
            // Treat endpoints, since they are not counted on strict intersection
            if (c1.At(t1l) == c2.At(t2l)) yield return new RootPair(t1l, t2l);
            if (c1.At(t1l) == c2.At(t2r)) yield return new RootPair(t1l, t2r);
            if (c1.At(t1r) == c2.At(t2l)) yield return new RootPair(t1r, t2l);
            if (c1.At(t1r) == c2.At(t2r)) yield return new RootPair(t1r, t2r);

            // Take the subcurves for the current iteration
            var bb1s = c1.Subcurve(t1l, t1r).BoundingBox;
            var bb2s = c2.Subcurve(t2l, t2r).BoundingBox;

            // If the bounding boxes don't intersect, neither do the curves
            if (!bb1s.StrictlyIntersects(bb2s)) yield break;

            // Pick the midpoints
            double t1m = (t1l + t1r) / 2;
            double t2m = (t2l + t2r) / 2;

            // Check the bounding box's measures to check whether we subdivide more the curves or not
            var r1 = IsRectangleNegligible(bb1s);
            var r2 = IsRectangleNegligible(bb2s);

            bool IsRectangleNegligible(DoubleRectangle r) => r.Width <= Epsilon / 8 && r.Height <= Epsilon / 8;

            // If both are negligible, just return the midpoints
            if (r1 && r2) foreach (var r in FindRoot()) yield return r;
            // Else, pick the right curves based on the decisions
            else if (r1)
            {
                foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1r, t2l, t2m)) yield return r;
                foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1r, t2m, t2r)) yield return r;
            }
            else if (r2)
            {
                foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1m, t2l, t2r)) yield return r;
                foreach (var r in GeneralCurveIntersections(c1, c2, t1m, t1r, t2l, t2r)) yield return r;
            }
            else
            {
                foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1m, t2l, t2m)) yield return r;
                foreach (var r in GeneralCurveIntersections(c1, c2, t1l, t1m, t2m, t2r)) yield return r;
                foreach (var r in GeneralCurveIntersections(c1, c2, t1m, t1r, t2l, t2m)) yield return r;
                foreach (var r in GeneralCurveIntersections(c1, c2, t1m, t1r, t2m, t2r)) yield return r;
            }

            // Find the roots
            IEnumerable<RootPair> FindRoot()
            {
                var bb = bb1s.Intersection(bb2s);

                if (bb.ContainsPoint(c1.At(t1l)) && bb.ContainsPoint(c2.At(t2l))) yield return new RootPair(t1l, t2l);
                if (bb.ContainsPoint(c1.At(t1l)) && bb.ContainsPoint(c2.At(t2r))) yield return new RootPair(t1l, t2r);
                if (bb.ContainsPoint(c1.At(t1r)) && bb.ContainsPoint(c2.At(t2l))) yield return new RootPair(t1r, t2l);
                if (bb.ContainsPoint(c1.At(t1r)) && bb.ContainsPoint(c2.At(t2r))) yield return new RootPair(t1r, t2r);

                var roots1 = c1.IntersectionsWithVerticalLine(bb.X)
                    .Concat(c1.IntersectionsWithVerticalLine(bb.X + bb.Width))
                    .Concat(c1.IntersectionsWithHorizontalLine(bb.Y))
                    .Concat(c1.IntersectionsWithHorizontalLine(bb.Y + bb.Height))
                    .Where(t => t >= t1l && t <= t1r);

                var roots2 = c2.IntersectionsWithVerticalLine(bb.X)
                    .Concat(c2.IntersectionsWithVerticalLine(bb.X + bb.Width))
                    .Concat(c2.IntersectionsWithHorizontalLine(bb.Y))
                    .Concat(c2.IntersectionsWithHorizontalLine(bb.Y + bb.Height))
                    .Where(t => t >= t2l && t <= t2r);

                if (roots1.Any() && roots2.Any())
                    yield return new RootPair(roots1.Average(), roots2.Average());
            }
        }

        public static void RemoveSimilarRoots(SortedDictionary<double, Double2> rootSet)
        {
            // Identify similar clusters on the rootSet
            var clusters = new List<SortedDictionary<double, Double2>>();
            var curCluster = new SortedDictionary<double, Double2>();

            var prevPair = new KeyValuePair<double, Double2>(double.NaN, new Double2());
            foreach (var kvp in rootSet)
            {
                if (!double.IsNaN(prevPair.Key))
                    // Compare the values
                    if (!RoughlyEquals(prevPair.Value, kvp.Value))
                    {
                        clusters.Add(curCluster);
                        curCluster = new SortedDictionary<double, Double2>();
                    }

                curCluster.Add(kvp.Key, kvp.Value);
                prevPair = kvp;
            }

            // Add the last cluster
            clusters.Add(curCluster);

            // Now, clear the set and, for each cluster, add a single root
            rootSet.Clear();

            foreach (var cluster in clusters)
            {
                // If the cluster has 0 or 1, add the 0 or 1
                if (cluster.ContainsKey(0)) rootSet[0] = cluster[0];
                else if (cluster.ContainsKey(1)) rootSet[1] = cluster[1];

                // Else, pick the average
                else
                {
                    int n = cluster.Count;
                    var value = new Double2(0, 0);
                    double key = 0;

                    foreach (var kvp in cluster)
                    {
                        key += kvp.Key;
                        value += kvp.Value;
                    }

                    rootSet[key / n] = (value / n).Truncate();
                }
            }
        }

        static string RectangleRepresentation(DoubleRectangle r)
            => $"Polygon(({r.X},{r.Y}),({r.X + r.Width},{r.Y}),({r.X + r.Width},{r.Y + r.Height}),({r.X},{r.Y + r.Height}))";
    }
}
