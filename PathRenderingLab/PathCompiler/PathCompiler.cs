using System;
using System.Collections.Generic;
using System.Linq;
using Svg;
using Svg.Pathing;

namespace PathRenderingLab.PathCompiler
{
    public static class PathCompilerMethods
    {

        /// <summary>
        /// Determine the filled simple segments of a path, splitting lines and curves appropriately.
        /// </summary>
        /// <param name="path">The path that is supposed to be compiled.</param>
        /// <param name="fillRule">The fill rule used to determine the filled components</param>
        /// <returns>The set of simple path components.</returns>
        public static CompiledDrawing CompileFill(SvgPathSegmentList path, SvgFillRule fillRule = SvgFillRule.EvenOdd)
        {
            var curveData = path.SplitCurves();
            var curves = new List<Curve>();

            foreach (var data in curveData)
            {
                // Add all open curves
                curves.AddRange(data.Curves);

                // Force close the open curves
                var p0 = data.Curves[0].At(0);
                var p1 = data.Curves[data.Curves.Length - 1].At(1);
                if (!DoubleUtils.RoughlyEquals(p0, p1)) curves.Add(Curve.Line(p1, p0));
            }

            return CompileCurves(curves, fillRule);
        }

        /// <summary>
        /// Compiles the stroke to the necessary triangles to draw it.
        /// </summary>
        /// <param name="path">The path to be compiled.</param>
        /// <param name="width">The width of the stroke.</param>
        /// <param name="lineCap">The line cap method for the stroke's ends.</param>
        /// <param name="lineJoin">The line join method for the stroke's midpoints</param>
        /// <param name="miterLimit">The miter limit value.</param>
        public static CompiledDrawing CompileStroke(SvgPathSegmentList path, double width,
            SvgStrokeLineCap lineCap = SvgStrokeLineCap.Butt,
            SvgStrokeLineJoin lineJoin = SvgStrokeLineJoin.Bevel,
            double miterLimit = double.PositiveInfinity)
        {
            // Return empty if stroke width == 0
            if (width == 0) return CompiledDrawing.Empty;

            // Divide the width by 2 to cope with the SVG documentation
            var halfWidth = width / 2;

            var curves = new List<Curve>();
            // Convert each split path to a fill
            foreach (var data in path.SplitCurves())
                curves.AddRange(StrokeUtils.ConvertToFill(data, halfWidth, lineCap, lineJoin, miterLimit));

            // And compile
            return CompileCurves(curves, SvgFillRule.NonZero);
        }

        internal static CompiledDrawing CompileCurves(List<Curve> curves, SvgFillRule fillRule)
        {
            // Reunite all intersections to subdivide the curves
            var curveRootSets = new SortedDictionary<double, Double2>[curves.Count];
            for (int i = 0; i < curveRootSets.Length; i++)
                curveRootSets[i] = new SortedDictionary<double, Double2>() { [0] = curves[i].At(0), [1] = curves[i].At(1) };

            // Get all intersections
            for (int i = 0; i < curves.Count; i++)
                for (int j = i + 1; j < curves.Count; j++)
                    foreach (var pair in Curve.Intersections(curves[i], curves[j]))
                    {
                        if (!GeometricUtils.Inside01(pair.A) || !GeometricUtils.Inside01(pair.B)) continue;

                        curveRootSets[i][pair.A] = curves[i].At(pair.A);
                        curveRootSets[j][pair.B] = curves[j].At(pair.B);
                    }

            // Cluster the intersections
            var curveRootClusters = DerivePointClustersFromRootSets(curveRootSets);

            // Finally, we can start building the DCEL
            var dcel = new DCEL.DCEL();

            for (int i = 0; i < curves.Count; i++)
            {
                var prevPair = new KeyValuePair<double, int>(double.NaN, 0);
                foreach (var curPair in curveRootClusters[i])
                {
                    if (!double.IsNaN(prevPair.Key))
                    {
                        Curve curve;
                        if (prevPair.Key == 0 && curPair.Key == 1) curve = curves[i];
                        else curve = curves[i].Subcurve(prevPair.Key, curPair.Key);

                        foreach (var c in curve.Simplify())
                        {
                            // Skip degenerate curves
                            if (c.IsDegenerate) continue;
                            dcel.AddCurve(c, prevPair.Value, curPair.Value);
                            //Console.WriteLine(dcel);
                            //Console.ReadLine();
                        }
                    }

                    prevPair = curPair;
                }
            }

            //Console.WriteLine(dcel);
            //Console.ReadLine();

            // Now, we remove wedges and assign the fill numbers

            dcel.RemoveWedges();
            //Console.WriteLine(dcel);
            //Console.ReadLine();
            dcel.AssignFillNumbers();
            //Console.WriteLine(dcel);
            //Console.ReadLine();

            // Pick the appropriate predicate for the fill rule
            Func<DCEL.Face, bool> facePredicate;
            if (fillRule == SvgFillRule.EvenOdd) facePredicate = f => f.FillNumber % 2 != 0;
            else facePredicate = f => f.FillNumber != 0;

            // Simplify the faces
            dcel.SimplifyFaces(facePredicate);
            //Console.WriteLine(dcel);
            //Console.ReadLine();

            // Generate the filled faces
            var fills = dcel.Faces.Where(facePredicate).Select(face =>
                new FillFace(face.Contours.Select(contour =>
                contour.CyclicalSequence.Select(e => e.Curve).ToArray()).ToArray()));

            // Generace the filled faces
            return CompiledDrawing.ConcatMany(fills.Select(CompiledDrawing.FromFace));
        }

        // Use disjoint sets to create the clusters
        private static SortedDictionary<double, int>[] DerivePointClustersFromRootSets(SortedDictionary<double, Double2>[] curveRootSets)
        {
            // First, gather all points and create the disjoint sets data structure
            var allPoints = curveRootSets.SelectMany(set => set.Values).ToArray();
            var disjointSets = new DisjointSets(allPoints.Length);

            // Now, reunite the clusters
            for (int i = 0; i < allPoints.Length; i++)
                for (int j = i + 1; j < allPoints.Length; j++)
                    if (DoubleUtils.RoughlyEquals(allPoints[i], allPoints[j]))
                        disjointSets.UnionSets(i, j);

            // Finally, attribute the clusters to the original curves
            int length = curveRootSets.Length;
            var clusters = new SortedDictionary<double, int>[length];
            int k = 0;

            for (int i = 0; i < length; i++)
            {
                clusters[i] = new SortedDictionary<double, int>();
                foreach (var kvp in curveRootSets[i])
                    clusters[i][kvp.Key] = disjointSets.FindParentOfSets(k++);
            }

            return clusters;
        }
    }
}