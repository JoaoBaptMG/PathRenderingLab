#if false

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.GeometricUtils;

namespace PathRenderingLab.DCEL
{

    // Stored face data: the vertices (in edge order) that form the face and the sub-DCEL it has
    public class Face
    {
        public Edge PrimaryEdge;
        public DCEL SubDCEL;
        public int FillNumber;
        public HashSet<Face> Cluster;

        public Face()
        {
            PrimaryEdge = null;
            Cluster = null;
            SubDCEL = new DCEL();
            FillNumber = 0;
        }

        /// <summary>
        /// Enumerate through all the edges (starting in its primary edge)
        /// </summary>
        public IEnumerable<Edge> Edges => PrimaryEdge?.CyclicalSequence ?? Enumerable.Empty<Edge>();

        /// <summary>
        /// Computes the face winding, which is double the signed area of the face
        /// </summary>
        public double Winding => Edges.Sum(e => e.Winding);

        /// <summary>
        /// A vertex inside the face, used to
        /// </summary>
        public Vertex ReferenceVertex => PrimaryEdge?.E1;

        /// <summary>
        /// Checks if the face contains the specified vertex.
        /// </summary>
        /// <param name="v">The vertex to be tested</param>
        /// <returns></returns>
        public bool ContainsVertex(Double2 v)
        {
            int numRoots = 0;
            foreach (var edge in Edges)
            {
                var bbox = edge.Curve.BoundingBox;
                var lspur = Curve.Line(v, v.WithX(Math.Max(bbox.X + bbox.Width, v.X) + 1f));

                // Ensure we get only one of the endpoints if necessary
                numRoots = unchecked(numRoots + Curve.Intersections(lspur, edge.Curve).Count(p => p.B < 1));
            }
            return numRoots % 2 == 1;
        }

        public string PathCommands
        {
            get
            {
                var list = new List<PathCommand>();
                list.Add(PathCommand.MoveTo(ReferenceVertex.Position));
                list.AddRange(Edges.Select(e => e.PathCommandFrom()));
                list.Add(PathCommand.ClosePath());

                return string.Join(" ", list);
            }
        }
    }
}

#endif