using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace PathRenderingLab.DCEL
{
    [Flags]
    public enum Show { Vertices = 1, Edges = 2, Faces = 4, All = Vertices | Edges | Faces }

    /// <summary>
    /// Manages a doubly-connected edge list, in order to build the simple graphs.
    /// It should be guaranteed that no two edges will intersect each other. This is very important in order
    /// for the algorithms to work.
    /// </summary>
    public class DCEL
    {
        private List<Vertex> vertices;
        private List<Edge> edges;
        private List<Face> faces;

        public ReadOnlyCollection<Vertex> Vertices => vertices.AsReadOnly();
        public ReadOnlyCollection<Edge> Edges => edges.AsReadOnly();
        public ReadOnlyCollection<Face> Faces => faces.AsReadOnly();

        /// <summary>
        /// Initializes the DCEL and constructs everything
        /// </summary>
        public DCEL()
        {
            vertices = new List<Vertex>();
            edges = new List<Edge>();
            faces = new List<Face>
            {
                // Initializes the first face, the outer face
                new Face(true)
            };
        }

        // Values necessary to "align" the curves in the right places
        const double TruncateVal = 2048;
        static double Truncate(double v) => Math.Round(v * TruncateVal) / TruncateVal;
        static Double2 Truncate(Double2 v) => new Double2(Truncate(v.X), Truncate(v.Y));

        /// <summary>
        /// Adds a vertex to the DCEL
        /// </summary>
        /// <param name="v">The vertex to be added</param>
        /// <returns>The vertex added</returns>
        public Vertex AddVertex(Double2 v)
        {
            var vertex = new Vertex(Truncate(v));
            vertices.Add(vertex);
            return vertex;
        }

        /// <summary>
        /// Finds if a vertex is on the DCEL
        /// </summary>
        /// <param name="v">The position to find</param>
        /// <param name="vertex"></param>
        /// <returns></returns>
        public bool FindVertex(Double2 v, out Vertex vertex)
        {
            v = Truncate(v);
            vertex = vertices.FirstOrDefault(vt => vt.Position == v);
            return vertex != null;
        }

        /// <summary>
        /// Adds a curve to the DCEL.
        /// </summary>
        /// <param name="curve">The curve to be added</param>
        public void AddCurve(Curve curve)
        {
            var vert1 = curve.At(0);
            var vert2 = curve.At(1);

            void PairOfEdges(Vertex v1, Vertex v2, out Edge e1, out Edge e2, bool reverse = false)
            {
                var rev = curve.Reverse;
                e1 = new Edge(v1, v2, reverse ? rev : curve);
                e2 = new Edge(v2, v1, reverse ? curve : rev);

                e1.Twin = e2;
                e2.Twin = e1;
            }

            // There are four main cases:
            // 1) The vertices are both new: we find which face they pertain and add them to the contour list
            // 2) The vertices are both existing and they connect two different contour of a face: we join those contours
            // 3) The vertices are both existing and they connect a contour of a face to itself: here, we close the face
            // 4) One of the vertices is new: here, there is not a lot of preprocessing to do

            // Check if the vertices were added to the cache first
            bool found1 = FindVertex(vert1, out var vertex1);
            bool found2 = FindVertex(vert2, out var vertex2);

            // If none are found, add them individually and add a contour to the face
            if (!found1 && !found2)
            {
                var face = GetFaceFromVertex(vert1);

                vertex1 = AddVertex(vert1);
                vertex2 = AddVertex(vert2);

                PairOfEdges(vertex1, vertex2, out var e1, out var e2);

                e1.Canonicity++;
                e1.Next = e1.Previous = e2;
                e2.Next = e2.Previous = e1;
                e1.Face = e2.Face = face;

                AddEdgePair(vertex1, vertex2, e1, e2);

                // Add the edge to the contours
                face.Contours.Add(e1);
            }
            // If both of them are found, we create the edge and find out which shapes they are
            else if (found1 && found2)
            {
                PairOfEdges(vertex1, vertex2, out var e1, out var e2);

                // If a matching edge is found, we're done here
                if (vertex1.SearchOutgoingEdges(e1, out var e1lo, out var e1ro))
                {
                    // Just up the canonicity of the edge, so we can track it
                    e1lo.Canonicity++;
                    return;
                }

                e1.Canonicity++;

                // The other matching edge is guaranteed to be found
                vertex2.SearchOutgoingEdges(e2, out var e2lo, out var e2ro);

                // Check whether the new edge will connect to different contours
                var differentContours = !e1lo.CyclicalSequence.Contains(e2lo);

                // WARNING: the operation above CANNOT be commuted with this below - leave the variable there
                // Correctly create the edge links (I won't try to draw a diagram here, sorry... my ASCII art is horrible)
                e1ro.Twin.Next = e2lo.Previous = e1;
                e2ro.Twin.Next = e1lo.Previous = e2;
                e1.Next = e2lo;
                e1.Previous = e1ro.Twin;
                e2.Next = e1lo;
                e2.Previous = e2ro.Twin;

                // Add the edges to the list
                AddEdgePair(vertex1, vertex2, e1, e2);

                // If the new edges were connected to different contours, fuse the contours
                if (differentContours)
                {
                    var face = e1lo.Face;
                    e1.Face = e2.Face = face;

                    // Create a hashset to agilize things
                    var edges = new HashSet<Edge>(e1.CyclicalSequence, ReferenceEqualityComparer.Default);

                    // Remove the previous edge links and add a reference edge
                    face.Contours.RemoveAll(e => edges.Contains(e));
                    face.Contours.Add(e1);
                }
                else
                {
                    // The case where the edges connected the same contour is trickier
                    // First, create a face
                    var newFace = new Face();
                    var oldFace = e1lo.Face;

                    // Remove the contours that pertained to the old edges
                    var edges = new HashSet<Edge>(e1.CyclicalSequence.Concat(e2.CyclicalSequence), ReferenceEqualityComparer.Default);
                    oldFace.Contours.RemoveAll(e => edges.Contains(e));

                    // Pick the edge that forms a counterclockwise sequence
                    var edge = e1.CyclicalSequence.Sum(e => e.Winding) > 0 ? e1 : e2;

                    // And add it to the new face
                    newFace.Contours.Add(edge);
                    AssignFace(newFace, edge);

                    // Now, pluck all the old contours that should pertain to the new face
                    var contours = oldFace.Contours.ExtractAll(e => newFace.ContainsVertex(e.E1.Position));

                    // Add them to the new face
                    newFace.Contours.AddRange(contours);
                    foreach (var c in contours) AssignFace(newFace, c);

                    // Put the counterclockwise edge's twin in the old face
                    oldFace.Contours.Add(edge.Twin);
                    AssignFace(oldFace, edge.Twin);

                    // Add the new face to the list
                    faces.Add(newFace);
                }
            }
            // If only one of them is found, the case is very simple
            else
            {
                // Old cached vertex and new vertex, so we can run the first algorithms
                var oldVertex = found1 ? vertex1 : vertex2;
                var epo = found1 ? vert1 : vert2;
                var epn = found1 ? vert2 : vert1;
                var newVertex = AddVertex(epn);

                // Create the new pair of edges and set the right canonicity
                PairOfEdges(oldVertex, newVertex, out var e1, out var e2, found2);
                (found1 ? e1 : e2).Canonicity++;

                // Search for the adjacent edges of the new vertex
                oldVertex.SearchOutgoingEdges(e1, out var e1lo, out var e1ro);

                // Set the vertices correctly
                e1.Previous = e1ro.Twin;
                e1.Next = e2;
                e2.Previous = e1;
                e2.Next = e1lo;
                e1ro.Twin.Next = e1;
                e1lo.Previous = e2;

                // The face is the outmost face
                e1.Face = e2.Face = e1lo.Face;

                // Add the edges to the list
                AddEdgePair(oldVertex, newVertex, e1, e2);
            }

            // FIN
        }

        /// <summary>
        /// Use a raycasting technique to select the face this vertex belongs
        /// </summary>
        /// <param name="vert">The vertex to be tested</param>
        /// <returns>The face to which the vertex belongs, or null if none</returns>
        public Face GetFaceFromVertex(Double2 vert) => faces.FirstOrDefault(f => f.ContainsVertex(vert));

        private void AddEdgePair(Vertex vertex1, Vertex vertex2, Edge e1, Edge e2)
        {
            edges.Add(e1);
            edges.Add(e2);

            vertex1.OutgoingEdges.Insert(e1, e1);
            vertex2.OutgoingEdges.Insert(e2, e2);

            CheckProblematicCycle(e1);
            CheckProblematicCycle(e2);
        }

        private void CheckProblematicCycle(Edge edge)
        {
            var set = new HashSet<Edge>(ReferenceEqualityComparer.Default);

            foreach (var e in edge.CyclicalSequence)
                if (!set.Add(e)) throw new Exception("Detected cycle outside of starting edge!");
        }

        private void AssignFace(Face face, Edge edge)
        {
            foreach (var e in edge.CyclicalSequence) e.Face = face;
        }

        /// <summary>
        /// Removes the wedges from the DCEL. Wedges are chains of edges that go "back and forth" without forming
        /// a line, and thus has no participation on face formation
        /// </summary>
        public void RemoveWedges()
        {
            // The test to see if an edge is (part of) a wedge
            // Its happen if the edge's twin is on the same face and all the edges in the sequence between
            // those two also happen to have the same feature
            bool IsWedge(Edge edge) => edge.CyclicalSequence.TakeWhile(e => e != edge.Twin).All(e => e.Face == e.Twin.Face);

            // Go through all the faces and all the contours for this
            foreach (var face in faces)
            {
                // An array of indices of contours to purge, if necessary
                var indices = new List<int>();

                // Go through each contour in order
                for (int i = 0; i < face.Contours.Count; i++)
                {
                    // We'll manually iterate the sequence here
                    var set = new HashSet<Edge>(ReferenceEqualityComparer.Default);

                    // Guarantee that we will not break the cycle
                    for (var e = face.Contours[i]; set.Add(e); e = e.Next)
                    {
                        // If the edge is a wedge
                        if (IsWedge(e))
                        {
                            // Try to find the start of the wedge
                            while (e.Face == e.Twin.Face)
                            {
                                // If if the previous edge is also the twin edge, we
                                // find that the entire contour is a wedge, so we remove it
                                if (e.Previous == e.Twin)
                                {
                                    indices.Add(i);
                                    goto breaktwo;
                                }

                                e = e.Previous;
                            }

                            // Now, e points to the last segment before the wedge. Fix the links.
                            var en = e.Next.Twin.Next;
                            e.Next = en;
                            en.Previous = e;

                            // Set the new contour beginning
                            face.Contours[i] = e;
                        }
                    }

                    breaktwo:;
                }

                // Pluck the contours which are only wedges
                face.Contours.ExtractIndices(indices.ToArray());
            }
        }

        /// <summary>
        /// Assign the fill numbers to the faces
        /// </summary>
        public void AssignFillNumbers()
        {
            // Create the iteration queue
            var alreadyAssignedFaces = new HashSet<Face>(ReferenceEqualityComparer.Default);
            var iterationQueue = new Queue<Face>();

            // Add the outer face first
            iterationQueue.Enqueue(faces.First(f => f.IsOuterFace));

            // Assign fill numbers to every face
            while (iterationQueue.Count > 0)
            {
                var face = iterationQueue.Dequeue();

                // Pass through every face and assign fill numbers
                foreach (var edge in face.Edges)
                {
                    var twinFace = edge.Twin.Face;

                    // Ignore faces already assigned
                    if (alreadyAssignedFaces.Contains(twinFace)) continue;

                    // Assign the fill number to the face
                    twinFace.FillNumber = face.FillNumber - edge.Canonicity + edge.Twin.Canonicity;
                    alreadyAssignedFaces.Add(twinFace);
                    iterationQueue.Enqueue(twinFace);
                }
            }
        }

        /// <summary>
        /// Converts the DCEL to a string
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString(Show.All);

        public string ToString(Show showOptions)
        {
            if (vertices.Count == 0) return "<<empty>>";

            var output = new StringBuilder();

            if ((showOptions & Show.Vertices) != 0)
            {
                output.AppendLine($"Vertices:");

                int i = 0;
                foreach (var vertex in vertices)
                    output.AppendLine($"-- [{i++}] ({vertex.Position}) " +
                        $"o=[{string.Join(" ", vertex.OutgoingEdges.Select(e => edges.IndexOf(e.Value)))}] " +
                        $"i=[{string.Join(" ", vertex.OutgoingEdges.Select(e => edges.IndexOf(e.Value.Twin)))}]");
            }

            if ((showOptions & Show.Edges) != 0)
            {
                output.AppendLine($"Edges: ");

                int i = 0;
                foreach (var edge in edges)
                    output.AppendLine($"-- [{i++}] {edge.Curve.ToString()} " +
                        $"c={edge.Canonicity} " +
                        $"face={faces.IndexOf(edge.Face)} " +
                        $"angle={edge.OuterAngles} " +
                        $"prev={edges.IndexOf(edge.Previous)} " +
                        $"next={edges.IndexOf(edge.Next)} " +
                        $"twin={edges.IndexOf(edge.Twin)}");
            }

            if ((showOptions & Show.Faces) != 0)
            {
                output.AppendLine($"Faces: ");

                int i = 0;
                foreach (var face in faces)
                {
                    var contours = face.Contours.Select(c => $"[{string.Join(" ", c.CyclicalSequence.Select(e => edges.IndexOf(e)))}]");
                    var outer = face.IsOuterFace ? " outer" : "";
                    output.AppendLine($"-- [{i++}] fill={face.FillNumber} contours=[{string.Join(" ", contours)}]{outer}");
                }
            }

            return output.ToString();
        }
    }
}
