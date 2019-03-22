using System;
using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab.PathCompiler.DCEL
{

    // Stored face data: the vertices (in edge order) that form the face and the sub-DCEL it has
    public class Face
    {
        public List<Edge> Contours;
        public int FillNumber;
        public readonly bool IsOuterFace;

        public Face(bool outer = false)
        {
            Contours = new List<Edge>();
            FillNumber = 0;
            IsOuterFace = outer;
        }

        /// <summary>
        /// Enumerate through all the edges (starting in its primary edge)
        /// </summary>
        public IEnumerable<Edge> Edges => Contours.SelectMany(e => e.CyclicalSequence);

        public IEnumerable<Edge> GetContour(int id) => Contours[id].CyclicalSequence;

        /// <summary>
        /// Computes the face winding, which is double the signed area of the face
        /// </summary>
        public double Winding => Edges.Sum(e => e.Winding);

        /// <summary>
        /// Checks if the face contains the specified vertex.
        /// </summary>
        /// <param name="v">The vertex to be tested</param>
        /// <returns></returns>
        public bool ContainsVertex(Double2 v)
        {
            int numRoots = IsOuterFace ? 1 : 0;

            foreach (var edge in Edges)
            {
                // Ignore edges which interface on "blank" contours
                if (edge.Face == edge.Twin.Face) continue;

                // Ensure we get only one of the endpoints if necessary
                bool IsValidRoot(double t) => t >= 0 && t < 1 && edge.Curve.At(t).X >= v.X;
                numRoots = unchecked(numRoots + edge.Curve.IntersectionsWithHorizontalLine(v.Y).Count(IsValidRoot));
            }
            return numRoots % 2 == 1;
        }

        public string PathCommands
        {
            get
            {
                var list = new List<PathCommand>();

                foreach (var edge in Contours)
                {
                    list.Add(PathCommand.MoveTo(edge.Curve.At(0)));
                    list.AddRange(edge.CyclicalSequence.Select(e => e.PathCommandFrom()));
                    list.Add(PathCommand.ClosePath());
                }

                return string.Join(" ", list);
            }
        }
    }
}