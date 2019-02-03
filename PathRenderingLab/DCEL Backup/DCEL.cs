#if false

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
            faces = new List<Face>();
        }

        const double TruncateVal = 1024;
        static double Truncate(double v) => Math.Round(v * TruncateVal) / TruncateVal;
        static Double2 Truncate(Double2 v) => new Double2(Truncate(v.X), Truncate(v.Y));

        public void AddCurve(Curve curve)
        {
            var vert1 = curve.At(0);
            var vert2 = curve.At(1);

            void PairOfEdges(Vertex v1, Vertex v2, out Edge e1, out Edge e2)
            {
                e1 = new Edge(v1, v2, curve);
                e2 = new Edge(v2, v1, curve.Reverse);

                e1.Twin = e2;
                e2.Twin = e1;
            }

            // There is a lot of pesky cases. Note that, since we are adding only "whole" edges (i.e.
            // edges do not intersect other edges, we guarantee that a single edge with brand new vertices
            // is inside a single face).

            // Check if the vertices were added to the cache first
            bool found1 = FindVertex(vert1, out var vertex1);
            bool found2 = FindVertex(vert2, out var vertex2);

            // If none are found, add them individually and connect both to the same face
            if (!found1 && !found2)
            {
                // First, check if they actually don't belong to a sub-DCEL
                var face = GetFaceFromVertex(vert1);

                // Add it to the sub-DCEL
                if (face != null) face.SubDCEL.AddCurve(curve);
                else // Add it to the outer face
                {
                    vertex1 = AddVertex(vert1);
                    vertex2 = AddVertex(vert2);

                    PairOfEdges(vertex1, vertex2, out var e1, out var e2);

                    e1.Canonicity++;
                    e1.Next = e1.Previous = e2;
                    e2.Next = e2.Previous = e1;

                    AddEdgePair(vertex1, vertex2, e1, e2);
                }
            }
            // If both of them are found, we "close" the loop, splitting the face in two and transfering the sub-DCEL
            // to one of the faces
            else if (found1 && found2)
            {
                // Discard the equal vertices case
                if (vertex1 == vertex2) return;

                PairOfEdges(vertex1, vertex2, out var e1, out var e2);

                // If a matching edge is found, abort the operation
                if (vertex1.SearchOutgoingEdges(e1, out var e1lo, out var e1ro))
                {
                    // Just up the canonicity of the edge, so we can track it
                    e1lo.Canonicity++;
                    return;
                }

                e1.Canonicity++;

                // Other ones are guaranteed to return false
                vertex2.SearchOutgoingEdges(e2, out var e2lo, out var e2ro);

                // Create the edge links (I won't try to draw a diagram here, sorry... my ASCII art is horrible)
                e1ro.Twin.Next = e2lo.Previous = e1;
                e2ro.Twin.Next = e1lo.Previous = e2;
                e1.Next = e2lo;
                e1.Previous = e1ro.Twin;
                e2.Next = e1lo;
                e2.Previous = e2ro.Twin;

                // Add the edges to the list
                AddEdgePair(vertex1, vertex2, e1, e2);

                // Old face
                var oldface = e2lo.Face;

                // We could be joining two disjoint points of the outer face together. Don't add faces if it is the case
                if (oldface == null && e1.CyclicalSequence.Contains(e2))
                {
                    // Check to see if we need to join clusters
                    var cl1 = e1.CyclicalSequence.FirstOrDefault(e => e.Twin.Face != null)?.Twin.Face.Cluster;
                    var cl2 = e2.CyclicalSequence.FirstOrDefault(e => e.Twin.Face != null)?.Twin.Face.Cluster;

                    // Check if the clusters really exist
                    if (cl1 != null && cl1 != cl2)
                        // Join them
                        foreach (var face in cl2)
                        {
                            cl1.Add(face);
                            face.Cluster = cl1;
                        }

                    return;
                }

                // New face
                var newface = new Face();
                faces.Add(newface);

                // Go through all the new edges and assign them to the new faces, regenerating their vertex caches
                // along the way. The first flag is here to guarantee the loop will at least begin (since the condition
                // would cause the loop not even to start)
                AssignFace(oldface, e1);
                AssignFace(newface, e2);

                // There are two cases: linking an inside face and an outside face
                if (oldface != null) // In case of an inside face
                {
                    // Now, finally move the sub-DCEL (if any) to the destination face
                    var dcel = oldface.SubDCEL;
                    if (dcel.vertices.Count > 0)
                    {
                        // Pick a random point on the sub-DCEL
                        var vtx = dcel.vertices[0].Position; // <-- Ugly, looks like Unity :/
                        // Put the DCEL on the new face
                        if (newface.ContainsVertex(vtx))
                        {
                            oldface.SubDCEL = newface.SubDCEL;
                            newface.SubDCEL = dcel;
                        }
                    }

                    // Attribute the same face cluster to the new face
                    newface.Cluster = oldface.Cluster;
                    newface.Cluster.Add(newface);
                }
                else
                {
                    if (newface.Winding < 0)
                    {
                        // When an outside face, we must be sure that it is the only face that has a clockwise winding
                        AssignFace(newface, e1);
                        AssignFace(oldface, e2);

                        // Swap the edges, so e1 belongs to null and e2 belongs to the newly created face
                        Edge et = e1;
                        e1 = e2;
                        e2 = et;
                    }

                    // Find if the face pertains to a cluster
                    var neighbor = e1.CyclicalSequence.FirstOrDefault(e => e.Twin.Face != null && e.Twin.Face != newface);

                    // If found, add it to the neighbor. If not, create a new one
                    if (neighbor != null) newface.Cluster = neighbor.Twin.Face.Cluster;
                    else newface.Cluster = new HashSet<Face>(ReferenceEqualityComparer.Default);

                    // Add the new face to its cluster
                    newface.Cluster.Add(newface);

                    // Finally, we have to deal with the possible case that the face "closes" on a cluster already in the DCEL
                    while (true)
                    {
                        var nextFace = faces.FirstOrDefault(delegate (Face f)
                        {
                            // Avoid faces that pertain to the "structure" of the vertex being detected
                            if (newface.Cluster.Contains(f)) return false;
                            return newface.ContainsVertex(f.ReferenceVertex.Position);
                        });

                        if (nextFace != null) MoveFaceClusterInwards(newface, nextFace);
                        else break;
                    }
                }
            }
            // If only one of them is found, we need to either add it "open" to the graph or join two DCELs (the worst part)
            else
            {
                // Old cached vertex and new vertex, so we can run the first algorithms
                var oldVertex = found1 ? vertex1 : vertex2;
                var epn = found1 ? vert2 : vert1;

                // Check the face sub-DCEL to check if the new vertex is in it
                var face = GetFaceFromVertex(epn);

                Vertex newVertex = null;
                // Null coalescing and checking for boolean in the same line
                // We will use the hash set for performance
                // We check for != true because it will return null on the outside
                // face or false on inside faces
                if (face?.SubDCEL.FindVertex(epn, out newVertex) != true)
                {
                    // Add the vertex to the list
                    newVertex = AddVertex(epn);

                    // Generate the two pairs of edges
                    PairOfEdges(oldVertex, newVertex, out var e1, out var e2);
                    if (found1) e1.Canonicity++;
                    else e2.Canonicity++;

                    // Search left and right for the edge found
                    oldVertex.SearchOutgoingEdges(e1, out var elo, out var ero);

                    // Assign correctly the linked edges (again, do not ask me to draw this diagram :/)
                    e1.Next = elo.Previous = e2;
                    e2.Previous = ero.Twin.Next = e1;
                    e1.Previous = ero.Twin;
                    e2.Next = elo;

                    // Assign the face to the vertices
                    e1.Face = e2.Face = face;

                    // Add the vertices to the lsit
                    AddEdgePair(oldVertex, newVertex, e1, e2);

                    // Regenerate the vertices of the face
                    AssignFace(face, e1);
                }
                else // The last case... joining two DCELs
                {
                    // First thing... we join  the two DCELs
                    Join(face.SubDCEL);
                    face.SubDCEL.Clear();

                    // Since the precondition is that edges cannot intersect other edges in the graph,
                    // we are guaranteed that this vertex has an edge in the outer face
                    // Generate the two pairs of edges
                    PairOfEdges(oldVertex, newVertex, out var e1, out var e2);
                    if (found1) e1.Canonicity++;
                    else e2.Canonicity++;

                    // Search left and right for the nearby vertices, now it is the same case as for linking
                    // two vertices together, except there is no new face
                    oldVertex.SearchOutgoingEdges(e1, out var e1lo, out var e1ro);
                    newVertex.SearchOutgoingEdges(e2, out var e2lo, out var e2ro);

                    // Create the edge links (...)
                    e1ro.Twin.Next = e2lo.Previous = e1;
                    e2ro.Twin.Next = e1lo.Previous = e2;
                    e1.Next = e2lo;
                    e1.Previous = e1ro.Twin;
                    e2.Next = e1lo;
                    e2.Previous = e2ro.Twin;

                    // Add the edges to the list
                    AddEdgePair(oldVertex, newVertex, e1, e2);

                    // Regenerate the vertices of the face
                    AssignFace(face, e1);
                }
            }

            // All cases are done? I can't believe!
        }

        private void MoveFaceClusterInwards(Face outerFace, Face innerFace)
        {
            // Extracts the entire structure pertaining innerFace and put it on outerFace
            var verticesToExtract = new HashSet<Vertex>(ReferenceEqualityComparer.Default);
            var edgesToExtract = new HashSet<Edge>(ReferenceEqualityComparer.Default);
            var facesToExtract = innerFace.Cluster;

            // Extract all faces
            foreach (var nextFace in facesToExtract)
                foreach (var edge in nextFace.Edges)
                {
                    // Extract both the edge and its twin
                    edgesToExtract.Add(edge);
                    edgesToExtract.Add(edge.Twin);

                    // Extract its endpoint
                    verticesToExtract.Add(edge.E1);
                    verticesToExtract.Add(edge.E2);
                }

            // Extracts the elements
            outerFace.SubDCEL.vertices.AddRange(vertices.ExtractObjects(verticesToExtract.ToArray()));
            outerFace.SubDCEL.edges.AddRange(edges.ExtractObjects(edgesToExtract.ToArray()));
            outerFace.SubDCEL.faces.AddRange(faces.ExtractObjects(facesToExtract.ToArray()));
        }

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
            // Account for the null face
            if (face != null) face.PrimaryEdge = edge;
            foreach (var e in edge.CyclicalSequence)
                e.Face = face;
        }

        /// <summary>
        /// Adds a vertex to the DCEL
        /// </summary>
        /// <param name="vert">The vertex to be added</param>
        /// <returns>The vertex added</returns>
        public Vertex AddVertex(Double2 vert)
        {
            var vertex = new Vertex(Truncate(vert));
            vertices.Add(vertex);
            return vertex;
        }

        public bool FindVertex(Double2 vert, out Vertex vertex)
        {
            vert = Truncate(vert);
            vertex = vertices.FirstOrDefault(vt => vt.Position == vert);
            return vertex != null;
        }

        /// <summary>
        /// Use a raycasting technique to select the face this vertex belongs
        /// </summary>
        /// <param name="vert">The vertex to be tested</param>
        /// <returns>The face to which the vertex belongs, or null if none</returns>
        public Face GetFaceFromVertex(Double2 vert) => faces.FirstOrDefault(f => f.ContainsVertex(vert));

        /// <summary>
        /// Join one doubly connected edge list into another
        /// </summary>
        /// <param name="subDCEL">The DCEL to be joined into this</param>
        public void Join(DCEL dcel)
        {
            // Join the edges
            edges.AddRange(dcel.edges);

            // Join the faces
            faces.AddRange(dcel.faces);

            // Join the vertices
            vertices.AddRange(dcel.vertices);
        }

        /// <summary>
        /// Assign the fill numbers to the faces
        /// </summary>
        public void AssignFillNumbers() => AssignFillNumbers(0);

        private void AssignFillNumbers(int first)
        {
            // Well, Face's default Equals behavior is reference equality, but I prefer to be explicit
            var alreadyAssignedFaces = new HashSet<Face>(ReferenceEqualityComparer.Default);
            var iterationQueue = new Queue<Face>();

            // First, assign fill numbers to all faces adjacent to the outer face, using the edges
            foreach (var edge in edges)
            {
                // We care only about the outer face edges
                if (edge.Face != null) continue;

                var twinFace = edge.Twin.Face;

                // Update the fill number of the face if it wasn't updated already
                if (twinFace == null || alreadyAssignedFaces.Contains(twinFace)) continue;

                twinFace.FillNumber = edge.Twin.Canonicity - edge.Canonicity + first;
                alreadyAssignedFaces.Add(twinFace);
                iterationQueue.Enqueue(twinFace);
            }

            // Now, assign fill numbers to every other face
            while (iterationQueue.Count > 0)
            {
                var face = iterationQueue.Dequeue();

                // Update the face's sub-DCEL first
                face.SubDCEL.AssignFillNumbers(face.FillNumber);

                // Now pass through every edge and assign the fill numbers
                foreach (var edge in face.Edges)
                {
                    var twinFace = edge.Twin.Face;
                    // Ignore the outer face and faces already assigned
                    if (twinFace == null || alreadyAssignedFaces.Contains(twinFace)) continue;

                    // Assign the fill number to the face
                    twinFace.FillNumber = face.FillNumber - edge.Canonicity + edge.Twin.Canonicity;
                    alreadyAssignedFaces.Add(twinFace);
                    iterationQueue.Enqueue(twinFace);
                }
            }
        }

        /// <summary>
        /// Get all the outer face cycles
        /// </summary>
        public IEnumerable<IEnumerable<Edge>> OuterFaceCycles
        {
            get
            {
                var alreadyCycledEdges = new HashSet<Edge>(ReferenceEqualityComparer.Default);

                foreach (var edge in edges)
                {
                    if (edge.Face != null || alreadyCycledEdges.Contains(edge)) continue;
                    foreach (var e in edge.CyclicalSequence) alreadyCycledEdges.Add(e);
                    yield return edge.CyclicalSequence;
                }
            }
        }

        /// <summary>
        /// Clear the DCEL
        /// </summary>
        public void Clear()
        {
            vertices.Clear();
            edges.Clear();
            faces.Clear();
        }

        /// <summary>
        /// Converts the DCEL representation to a string
        /// </summary>
        /// <returns></returns>
        public override string ToString() => ToString(Show.All);

        public string ToString(Show showOptions = Show.All) => ToString(0, showOptions);

        private string ToString(int depth, Show showOptions)
        {
            if (vertices.Count == 0) return "<<empty>>";

            string Rp(Double2 v) => $"({v})";
            var newl = "\n" + Enumerable.Repeat("|  ", depth).Aggregate("", string.Concat);

            var output = new StringBuilder();
            if (depth > 0) output.Append(newl);

            if ((showOptions & Show.Vertices) != 0)
            {
                output.Append($"Vertices: {newl}");

                int i = 0;
                foreach (var vertex in vertices)
                    output.Append($"-- [{i++}] {Rp(vertex.Position)} " +
                        $"o=[{string.Join(" ", vertex.OutgoingEdges.Select(e => edges.IndexOf(e.Value)))}] " +
                        $"i=[{string.Join(" ", vertex.OutgoingEdges.Select(e => edges.IndexOf(e.Value.Twin)))}]{newl}");
            }

            if ((showOptions & Show.Edges) != 0)
            {
                output.Append($"Edges: {newl}");

                int i = 0;
                foreach (var edge in edges)
                    output.Append($"[{i++}] {edge.Curve.ToString()} face={faces.IndexOf(edge.Face)} angle={edge.OuterAngles.ToString()} " +
                        $"prev={edges.IndexOf(edge.Previous)} next={edges.IndexOf(edge.Next)} twin={edges.IndexOf(edge.Twin)}{newl}");
            }

            if ((showOptions & Show.Faces) != 0)
            {
                output.Append($"Faces: {newl}");

                int i = 0;
                foreach (var face in faces)
                {
                    var pad = new string(' ', $"-- [{i}] ".Length);
                    output.Append($"-- [{i++}] fill={face.FillNumber} cluster={face.Cluster.GetHashCode():X} " +
                        $"edges=[{string.Join(" ", face.Edges.Select(e => edges.IndexOf(e)))}]{newl}");
                    output.Append($"{pad}sub-DCEL: {face.SubDCEL.ToString(depth + 1, showOptions)}{newl}");
                }
            }

            return output.ToString();
        }
    }
}

/*
M6.7174531 279.41379 C7.01949820736084 279.00159103589 7.37542305649226 278.766807261266 7.75824932606452 278.662407636306
M7.75827154140951 278.662413856737 C7.71369341985048 279.531830932197 7.69005258006555 280.401303643495 7.63802794538725 281.267732284157
M7.63802794538725 281.267732284157 C7.62930283470485 281.413042017419 7.61977937229157 281.558266130028 7.6092249 281.70339
M7.6092249 281.70339 C7.0667731304989 282.619440308918 6.57052294986301 282.186152637421 6.35810575168721 281.447211700175
M6.35810575168721 281.447211700175 6.34483172154254 281.401035004689 6.33266607248763 281.353664718885 6.32166679319746 281.305355530887
M6.32166679319746 281.305355530887 C6.16626560858738 280.622828535758 6.24368236489234 279.752888181442 6.7174531 279.41379
 */

#endif