using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PathRenderingLab.PathCompiler;
using System.Diagnostics;
using Svg;

namespace PathRenderingLab
{
#if WINDOWS || LINUX
    /// <summary>
    /// The main class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Utility function to be able to conditionally add an element to a list using the ?. operator
        /// </summary>
        /// <typeparam name="T">The type of the element to be added.</typeparam>
        /// <param name="element">The element to be added.</param>
        /// <param name="list">The list which to add the element to.</param>
        public static void AddTo<T>(this T element, List<T> list) => list.Add(element);

        public static Color Convert(System.Drawing.Color c) => new Color(c.R, c.G, c.B, c.A);

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

            string file;
            if (args.Length > 0) file = args[0];
            else
            {
                Console.Write("Enter path of SVG file: ");
                file = Console.ReadLine();
            }

            var svg = SvgDocument.Open(file);
            var paths = new List<SvgPath>();
            svg.ApplyRecursive(el => (el as SvgPath)?.AddTo(paths));

            int numPaths = 0;
            var triangleIndices = new List<int>();
            var curveVertices = new List<VertexPositionCurve>();
            var doubleCurveVertices = new List<VertexPositionDoubleCurve>();

            var triangleIndicesStartingIds = new List<int>() { 0 };
            var curveVerticesStartingIds = new List<int>() { 0 };
            var doubleCurveVerticesStartingIds = new List<int>() { 0 };

            var colors = new List<Color>();
            var transforms = new List<Matrix>();
            var vertexCache = new Dictionary<Vector2, int>();

            int curTriangleIndicesStartingId = 0;
            int curCurveVerticesStartingId = 0;
            int curDoubleCurveVerticesStartingId = 0;

            int IdForVertex(Vector2 v)
            {
                if (!vertexCache.ContainsKey(v))
                    vertexCache[v] = vertexCache.Count;
                return vertexCache[v];
            }

            var watch = new Stopwatch();
            var totalTimes = new List<TimeSpan>();

            TReturn MeasureTime<TReturn>(Func<TReturn> func, out TimeSpan time)
            {
                watch.Restart();
                var result = func();
                watch.Stop();
                time = watch.Elapsed;
                return result;
            }

            foreach (var path in paths)
            {
#if false
                var path = svgPath.Path;
                var ps = svgPath.PathStyle;

                // Normalize the path
                var normalMatrix = path.NormalizeAndTruncate();
                var matrix = (Matrix)(svgPath.Transform.ToMatrix() * normalMatrix);

                Console.WriteLine($"Parsed path {++pathId}: {svgPath.Path}");
                Console.WriteLine();

                if (ps.FillColor.HasValue && ps.FillColor.Value.A > 0)
                {
                    AddDrawing(MeasureTime(() => PathCompilerMethods.CompileFill(path, ps.FillRule), out var time));
                    colors.Add(ps.FillColor.Value);
                    transforms.Add(matrix);
                    totalTimes.Add(time);
                    numPaths++;
                }

                if (ps.StrokeColor.HasValue && ps.StrokeColor.Value.A > 0)
                {
                    AddDrawing(MeasureTime(() => PathCompilerMethods.CompileStroke(path, ps.StrokeWidth / normalMatrix.A,
                        ps.StrokeLineCap, ps.StrokeLineJoin, ps.MiterLimit), out var time));
                    colors.Add(ps.StrokeColor.Value);
                    transforms.Add(matrix);
                    totalTimes.Add(time);
                    numPaths++;
                }
#endif

                if (path.Fill is SvgColourServer clr && clr.Colour.A > 0)
                {
                    AddDrawing(MeasureTime(() => PathCompilerMethods.CompileFill(path.PathData, path.FillRule), out var time));
                    colors.Add(Convert(clr.Colour));
                    transforms.Add(Matrix.Identity);
                    totalTimes.Add(time);
                    numPaths++;
                }

                if (path.Stroke is SvgColourServer clr2 && clr2.Colour.A > 0)
                {
                    AddDrawing(MeasureTime(() => PathCompilerMethods.CompileStroke(path.PathData, path.StrokeWidth,
                        path.StrokeLineCap, path.StrokeLineJoin, path.StrokeMiterLimit), out var time));
                    colors.Add(Convert(clr2.Colour));
                    transforms.Add(Matrix.Identity);
                    totalTimes.Add(time);
                    numPaths++;
                }

                void AddDrawing(CompiledDrawing drawing)
                {
                    var curTriangleIndices = new List<int>();
                    var curCurveVertices = new List<VertexPositionCurve>();
                    var curDoubleCurveVertices = new List<VertexPositionDoubleCurve>();

                    foreach (var tri in drawing.Triangles)
                    {
                        curTriangleIndices.Add(IdForVertex((Vector2)tri.A));
                        curTriangleIndices.Add(IdForVertex((Vector2)tri.B));
                        curTriangleIndices.Add(IdForVertex((Vector2)tri.C));
                    }

                    foreach (var tri in drawing.CurveTriangles)
                        curCurveVertices.AddRange(new[]
                        {
                            (VertexPositionCurve)tri.A,
                            (VertexPositionCurve)tri.B,
                            (VertexPositionCurve)tri.C
                        });

                    foreach (var tri in drawing.DoubleCurveTriangles)
                        curDoubleCurveVertices.AddRange(new[]
                        {
                            (VertexPositionDoubleCurve)tri.A,
                            (VertexPositionDoubleCurve)tri.B,
                            (VertexPositionDoubleCurve)tri.C
                        });

                    triangleIndices.AddRange(curTriangleIndices);
                    curveVertices.AddRange(curCurveVertices);
                    doubleCurveVertices.AddRange(curDoubleCurveVertices);

                    curTriangleIndicesStartingId += curTriangleIndices.Count;
                    triangleIndicesStartingIds.Add(curTriangleIndicesStartingId);

                    curCurveVerticesStartingId += curCurveVertices.Count;
                    curveVerticesStartingIds.Add(curCurveVerticesStartingId);

                    curDoubleCurveVerticesStartingId += curDoubleCurveVertices.Count;
                    doubleCurveVerticesStartingIds.Add(curDoubleCurveVerticesStartingId);
                }
            }

            int length = vertexCache.Count == 0 ? 0 : vertexCache.Max(p => p.Value) + 1;
            var allVertices = new Vector2[length];
            foreach (var kvp in vertexCache) allVertices[kvp.Value] = kvp.Key;

            Console.WriteLine("Statistics:");

            void WriteStats(string name, int numIndices, int numCurveVertices, int numDoubleCurveVertices, TimeSpan time)
            {
                int numTris = numIndices / 3;
                int numCurveTris = numCurveVertices / 3;
                int numDoubleCurveTris = numDoubleCurveVertices / 3;
                Console.WriteLine($"{name}: {numTris + numCurveTris + numDoubleCurveTris} triangles " +
                    $"({numTris} filled, {numCurveTris} curves and {numDoubleCurveTris} double curves), " +
                    $"parsed in {time.TotalMilliseconds:0.00} ms");
            }

            for (int i = 0; i < numPaths; i++)
                WriteStats($"path {i+1}",
                    triangleIndicesStartingIds[i + 1] - triangleIndicesStartingIds[i],
                    curveVerticesStartingIds[i + 1] - curveVerticesStartingIds[i],
                    doubleCurveVerticesStartingIds[i + 1] - doubleCurveVerticesStartingIds[i], totalTimes[i]);

            Color backgroundColor;
            while (true)
            {
                Console.Write("Select background color: ");
                var color = CSSColor.Parse(Console.ReadLine());
                if (!color.HasValue) Console.WriteLine("Could not parse the color correctly!");
                else
                {
                    backgroundColor = color.Value;
                    break;
                }
            }

            using (var game = new PathRenderingLab())
            {
                game.BackgroundColor = backgroundColor;
                game.AllVertices = allVertices;
                game.DrawingColors = colors.ToArray();
                game.DrawingTransforms = transforms.ToArray();
                game.DrawingIndices = triangleIndices.ToArray();
                game.DrawingCurveVertices = curveVertices.ToArray();
                game.DrawingDoubleCurveVertices = doubleCurveVertices.ToArray();
                game.DrawingIndicesStartingIds = triangleIndicesStartingIds.ToArray();
                game.DrawingCurveVerticesStartingIds = curveVerticesStartingIds.ToArray();
                game.DrawingDoubleCurveVerticesStartingIds = doubleCurveVerticesStartingIds.ToArray();
                game.NumDrawings = numPaths;

                game.Run();
            }
        }
    }
#endif
            }
