using System;
using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab
{
    public enum FillRule { Nonzero, Evenodd }
    public enum StrokeLineCap { Butt, Round, Square }
    public enum StrokeLineJoin { Bevel, Miter, MiterClip, Round, Arcs }

    public class PathCompiler
    {
        public static int FindOrAddPoint(List<Double2> vertices, Double2 vertex)
        {
            int ind = vertices.FindIndex(x => DoubleUtils.RoughlyEquals(x, vertex));
            if (ind == -1)
            {
                ind = vertices.Count;
                vertices.Add(vertex);
            }
            return ind;
        }

        /// <summary>
        /// Determine the filled simple segments of a path, splitting lines and curves appropriately.
        /// </summary>
        /// <param name="path">The path that is supposed to be compiled.</param>
        /// <param name="fillRule">The fill rule used to determine the filled components</param>
        /// <returns>The set of simple path components.</returns>
        public static CompiledDrawing CompileFill(Path path, FillRule fillRule = FillRule.Evenodd)
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

        internal static CompiledDrawing CompileCurves(List<Curve> curves, FillRule fillRule)
        {
            Console.WriteLine("Current curve list:");
            {
                int i = 0;
                foreach (var curve in curves)
                    Console.WriteLine($"-- [{i++}] {curve}");
            }
            Console.WriteLine("Path command: ");
            Console.WriteLine(string.Join(" ", curves.Select(c => c.PathRepresentation())));

            // Reunite all intersections to subdivide the curves
            var curveRootSets = new List<double>[curves.Count];
            for (int i = 0; i < curveRootSets.Length; i++)
                curveRootSets[i] = new List<double>() { 1f };

            // Get all intersections
            for (int i = 0; i < curves.Count; i++)
                for (int j = i + 1; j < curves.Count; j++)
                    foreach (var pair in Curve.Intersections(curves[i], curves[j]))
                    {
                        var a = curves[i].At(pair.A);
                        var b = curves[j].At(pair.B);
                        if (!DoubleUtils.RoughlyEquals(a, b))
                        {
                            Console.WriteLine(string.Join(" ", Curve.Intersections(curves[i], curves[j]).Select(c => $"({c.A} {c.B})")));
                            throw new Exception("Problem here...");
                        }

                        curveRootSets[i].Add(pair.A);
                        curveRootSets[j].Add(pair.B);
                    }

            // Sort and eliminate all weird results
            foreach (var set in curveRootSets)
            {
                set.RemoveAll(r => !GeometricUtils.Inside01(r));
                set.RemoveDuplicatedValues();
            }

            // Finally, we can start building the DCEL
            var dcel = new DCEL.DCEL();

            for (int i = 0; i < curves.Count; i++)
            {
                double v = 0f;
                foreach (var l in curveRootSets[i])
                {
                    var curve = curves[i].Subcurve(v, l);

                    foreach (var c in curve.Simplify())
                    {
                        dcel.AddCurve(c);
                        //Console.WriteLine(dcel);
                        //Console.ReadLine();
                    }

                    v = l;
                }
            }

            // Now, we remove wedges and assign the fill numbers
            dcel.RemoveWedges();
            //Console.WriteLine(dcel);
            //Console.ReadLine();
            dcel.AssignFillNumbers();
            //Console.WriteLine(dcel);
            //Console.ReadLine();

            // Pick the appropriate predicate for the fill rule
            Func<DCEL.Face, bool> facePredicate;
            if (fillRule == FillRule.Evenodd) facePredicate = f => f.FillNumber % 2 != 0;
            else facePredicate = f => f.FillNumber != 0;

            // Simplify the faces
            dcel.SimplifyFaces(facePredicate);
            Console.WriteLine(dcel);
            //Console.ReadLine();

            // Generate the filled faces
            var fills = dcel.Faces.Where(facePredicate).Select(face =>
                new FillFace(face.Contours.Select(contour =>
                contour.CyclicalSequence.Select(e => e.Curve).ToArray()).ToArray()));

            // Generace the filled faces
            return CompiledDrawing.ConcatMany(fills.Select(CompiledDrawing.FromFace));
        }

        /// <summary>
        /// Compiles the stroke to the necessary triangles to draw it.
        /// </summary>
        /// <param name="path">The path to be compiled.</param>
        /// <param name="width">The width of the stroke.</param>
        /// <param name="lineCap">The line cap method for the stroke's ends.</param>
        /// <param name="lineJoin">The line join method for the stroke's midpoints</param>
        /// <param name="miterLimit">The miter limit value.</param>
        public static CompiledDrawing CompileStroke(Path path, double width,
            StrokeLineCap lineCap = StrokeLineCap.Butt,
            StrokeLineJoin lineJoin = StrokeLineJoin.Bevel,
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
            return CompileCurves(curves, FillRule.Nonzero);
        }
    }
}