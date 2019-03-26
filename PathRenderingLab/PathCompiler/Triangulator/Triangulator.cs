using Bitlush;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PathRenderingLab.PathCompiler.Triangulator
{
    public static class YMonotone
    {
        // The algorithm used here is explained on Chapter 3 on
        // "Computational Geometry: Algorithms and Applications", de Berg et al

        public static Triangle[] Triangulate(Double2[][] contours)
        {
            // Check for ill-formed contours here
            Debug.Assert(!DetectSelfIntersections(contours), "Contours have self intersecting edges!");
            bool state = DetectSelfIntersections(contours);

            // Firstly, simplify the contours
            var simplifiedContours = contours.Select(GeometricUtils.SimplifyPolygon).ToArray();

            // Discard trivial contours right now
            if (simplifiedContours.Length == 0) return new Triangle[0];

            // First, partition the polygon into y-monotone pieces
            var polygons = PartitionToMonotone(simplifiedContours);

            // Now, triangulate each one of them
            return polygons.SelectMany(TriangulateMonotone).Where(t => !t.IsDegenerate).ToArray();
        }

        public static IEnumerable<Double2[]> PartitionToMonotone(Double2[][] contours)
        {
            // Sort all vertices using their default comparison
            var vertexSet = new SortedSet<Vertex>();
            var tempVertices = new List<Vertex>();

            // Add the vertices to the list
            foreach (var poly in contours)
            {
                // Skip "line" contours
                if (poly.Length < 3) continue;

                // To make the circular list
                Vertex first = null, last = null;

                for (int i = 0; i < poly.Length; i++)
                {
                    var prev = poly[(i == 0 ? poly.Length : i) - 1];
                    var cur = poly[i];
                    var next = poly[i == poly.Length - 1 ? 0 : i + 1];

                    var vtx = new Vertex(prev, cur, next);

                    // Build the circular list
                    if (last != null)
                    {
                        vtx.PreviousEdge = last.NextEdge;
                        vtx.IncomingEdges.Insert(vtx.PreviousEdge, vtx.PreviousEdge);

                        vtx.NextEdge.Previous = vtx.PreviousEdge;
                        vtx.PreviousEdge.Next = vtx.NextEdge;
                    }

                    // Add the vertex
                    vertexSet.Add(vtx);
                    tempVertices.Add(vtx);

                    last = vtx;
                    if (first == null) first = last;
                }

                // Close the loop
                first.PreviousEdge = last.NextEdge;
                first.IncomingEdges.Insert(first.PreviousEdge, first.PreviousEdge);

                first.NextEdge.Previous = first.PreviousEdge;
                first.PreviousEdge.Next = first.NextEdge;
            }

            // Put all vertices into an array, and swipe from up to down
            var vertices = vertexSet.ToArray();
            Array.Reverse(vertices);

            // Put the edges here
            var edges = new AvlTree<Edge, Edge>();

            void SplitDiagonal(Vertex v1, Vertex v2)
            {
                var e12 = new Edge(v1.Current, v2.Current);
                var e21 = new Edge(v2.Current, v1.Current);

                v1.SearchOutgoingEdges(e12, out var e1lo, out var e1ro);
                v1.SearchIncomingEdges(e21, out var e1li, out var e1ri);

                v2.SearchOutgoingEdges(e21, out var e2lo, out var e2ro);
                v2.SearchIncomingEdges(e12, out var e2li, out var e2ri);

                e1ri.Next = e12;
                e2lo.Previous = e12;

                e2ri.Next = e21;
                e1lo.Previous = e21;

                e12.Next = e2lo;
                e12.Previous = e1ri;

                e21.Next = e1lo;
                e21.Previous = e2ri;

                v1.OutgoingEdges.Insert(e12, e12);
                v1.IncomingEdges.Insert(e21, e21);

                v2.OutgoingEdges.Insert(e21, e21);
                v2.IncomingEdges.Insert(e12, e12);

                // Sanity check
                var eds = new HashSet<Edge>(ReferenceEqualityComparer.Default);
                foreach (var e in e12.CyclicalSequence)
                    if (!eds.Add(e)) throw new Exception("Problematic edge sequence!");

                eds.Clear();
                foreach (var e in e21.CyclicalSequence)
                    if (!eds.Add(e)) throw new Exception("Problematic edge sequence!");
            }

            Edge SearchEdge(Vertex v)
            {
                edges.SearchLeftRight(new Edge(v.Current, v.Current), out var eleft, out var eright);
                return eleft;
            }

            // Act according with the type of the vertex
            foreach (var v in vertices)
            {
                switch (v.Type)
                {
                    case VertexType.Start:
                    case VertexType.Split:
                        if (v.Type == VertexType.Split)
                        {
                            var edgeLeft = SearchEdge(v);
                            SplitDiagonal(v, edgeLeft.Helper);
                            edgeLeft.Helper = v;
                        }

                        v.NextEdge.Helper = v;
                        edges.Insert(v.NextEdge, v.NextEdge);
                        break;
                    case VertexType.End:
                    case VertexType.Merge:
                        if (v.PreviousEdge.Helper.Type == VertexType.Merge)
                            SplitDiagonal(v, v.PreviousEdge.Helper);
                        edges.Delete(v.PreviousEdge);

                        if (v.Type == VertexType.Merge)
                        {
                            var edgeLeft = SearchEdge(v);
                            if (edgeLeft.Helper.Type == VertexType.Merge)
                                SplitDiagonal(v, edgeLeft.Helper);
                            edgeLeft.Helper = v;
                        }
                        break;
                    case VertexType.RegularLeft:
                        if (v.PreviousEdge.Helper.Type == VertexType.Merge)
                            SplitDiagonal(v, v.PreviousEdge.Helper);
                        edges.Delete(v.PreviousEdge);

                        v.NextEdge.Helper = v;
                        edges.Insert(v.NextEdge, v.NextEdge);
                        break;
                    case VertexType.RegularRight:
                        var eleft = SearchEdge(v);
                        if (eleft.Helper.Type == VertexType.Merge)
                            SplitDiagonal(v, eleft.Helper);
                        eleft.Helper = v;
                        break;
                    default:
                        break;
                }
            }

            // Collect all edges so we can pick the Y-monotone polygons
            var allEdges = new HashSet<Edge>(ReferenceEqualityComparer.Default);
            var primaryEdges = new List<Edge>();

            foreach (var v in vertices)
            {
                var e = v.NextEdge;

                var eds = new HashSet<Edge>(ReferenceEqualityComparer.Default);
                int i = 0;
                foreach (var u in e.CyclicalSequence)
                {
                    if (!eds.Add(u)) throw new Exception("Problematic edge sequence! " + i);
                    i++;
                }

                if (allEdges.Contains(e)) continue;

                primaryEdges.Add(e);
                foreach (var u in e.CyclicalSequence)
                    allEdges.Add(u);
            }

            // Now, collect the formed polygons
            return primaryEdges.Select(edge => edge.CyclicalSequence.Select(e => e.A).ToArray());
        }

        public static IEnumerable<Triangle> TriangulateMonotone(Double2[] polygon)
        {
            int length = polygon.Length;

            // Account for degenerate cases
            if (length == 2) yield break;
            else if (length == 3)
            {
                yield return new Triangle(polygon[0], polygon[1], polygon[2]);
                yield break;
            }

            // Locate the beginning and the end of the chain
            int SpecialPoint(bool bflag)
            {
                for (int i = 0; i < length; i++)
                {
                    var prev = polygon[(i == 0 ? length : i) - 1];
                    var cur = polygon[i];
                    var next = polygon[i == length - 1 ? 0 : i + 1];

                    int comparisonCP = CanonicalComparer.Default.Compare(cur, prev);
                    int comparisonCN = CanonicalComparer.Default.Compare(cur, next);

                    if (bflag ? comparisonCP > 0 && comparisonCN > 0 : comparisonCP < 0 && comparisonCN < 0)
                        return i;
                }

                return 0;
            }

            int begin = SpecialPoint(true), end = SpecialPoint(false);

            // Fill both chains
            var leftChain = new List<ChainVertex>();
            var rightChain = new List<ChainVertex>();

            for (int i = (end + length - 1) % length; i != begin; i = (i + length - 1) % length)
                leftChain.Add(new ChainVertex(polygon[i], VertexType.RegularLeft));

            // DRY people cry here, sorry :/
            for (int i = (end + 1) % length; i != begin; i = (i + 1) % length)
                rightChain.Add(new ChainVertex(polygon[i], VertexType.RegularRight));

            // Merge
            var vertices = ArrayExtensions.Merge(leftChain.ToArray(), rightChain.ToArray());
            Array.Reverse(vertices);

            // Create the stack
            var stack = new Stack<ChainVertex>();
            stack.Push(new ChainVertex(polygon[begin], VertexType.Start));
            stack.Push(vertices[0]);

            // Operate on the vertices
            for (int j = 1; j < vertices.Length; j++)
            {
                var pvert = vertices[j];
                var vert = stack.Pop();

                if (vert.Type != VertexType.Start && pvert.Type != vert.Type)
                {
                    while (stack.Count > 0)
                    {
                        var other = stack.Pop();
                        yield return new Triangle(pvert.Position, vert.Position, other.Position);
                        vert = other;
                    }

                    stack.Push(vertices[j - 1]);
                }
                else
                {
                    bool CanMakeDiagonal(ChainVertex o)
                    {
                        if (vert.Type == VertexType.RegularLeft)
                            return (o.Position - pvert.Position).Cross(vert.Position - pvert.Position) >= 0;
                        return (o.Position - pvert.Position).Cross(vert.Position - pvert.Position) <= 0;
                    }

                    var other = stack.Peek();

                    while (CanMakeDiagonal(other))
                    {
                        yield return new Triangle(pvert.Position, vert.Position, other.Position);

                        stack.Pop();
                        vert = other;
                        if (stack.Count == 0) break;
                        other = stack.Peek();
                    }

                    stack.Push(vert);
                }

                stack.Push(pvert);
            }

            // Push last vertex
            if (stack.Count > 0)
            {
                var pvert = new ChainVertex(polygon[end], VertexType.End);
                var vert = stack.Pop();
                while (stack.Count > 0)
                {
                    var other = stack.Pop();
                    yield return new Triangle(pvert.Position, vert.Position, other.Position);
                    vert = other;
                }
            }
        }

        internal static string PolygonRepresentation(Double2[] polygon)
        {
            var str = string.Join(", ", polygon.Select(p => $"({p.X}, {p.Y})"));
            return $"Polygon({str})";
        }

        internal static bool DetectSelfIntersections(Double2[][] contours)
        {
            var totalLines = contours.Sum(c => c.Length);
            var la = new Double2[totalLines];
            var lb = new Double2[totalLines];

            int k = 0;
            foreach (var contour in contours)
            {
                for (int i = 0; i < contour.Length; i++)
                {
                    la[k + i] = contour[i];
                    lb[k + i] = contour[(i + 1) % contour.Length];
                }
                k += contour.Length;
            }

            for (int j = 0; j < totalLines; j++)
                for (int i = j + 1; i < totalLines; i++)
                {
                    // Check for intersections at the midpoints
                    if (GeometricUtils.SegmentsIntersect(la[i], lb[i], la[j], lb[j], true))
                        return true;
                }

            return false;
        }
    }
}
