using System;
using System.Collections.Generic;

namespace PathRenderingLab.PathCompiler.Triangulator
{
    /// <summary>
    /// An edge on the polygon. Its comparison method checks if one edge is "to the left" of another
    /// </summary>
    public class Edge : IComparable<Edge>
    {
        public readonly Double2 A, B;
        public Vertex Helper;
        public Edge Previous, Next, Twin;

        public Edge(Double2 a, Double2 b)
        {
            A = a;
            B = b;
        }

        public int CompareTo(Edge other)
        {
            // First check for equal edges
            if (A == other.A && B == other.B) return 0;
            if (A == other.B && B == other.A) return 0;
            
            // Build an upwards-facing edge
            int comp = CanonicalComparer.Default.Compare(A, B);

            // If the edge is degenerate (i. e. if it is a vertex)
            if (comp == 0) return -other.CompareTo(this);

            var max = comp > 0 ? A : B;
            var min = comp > 0 ? B : A;

            // The edge is to the left if the points of the other edge have a
            // positive cross product with the other
            // If both points disagree, use the other comparison
            var sign1 = Math.Sign((max - min).Cross(other.A - min));
            var sign2 = Math.Sign((max - min).Cross(other.B - min));
            if (sign1 == 0) return sign2;
            if (sign2 == 0) return sign1;
            if (sign1 != sign2) return -other.CompareTo(this);
            return sign1;
        }

        public override string ToString() => $"{A} / {B}, ({Helper})";

        /// <summary>
        /// Iterates throught the edge's sequence formed by its "next" links
        /// </summary>
        public IEnumerable<Edge> CyclicalSequence
        {
            get
            {
                yield return this;
                for (var edge = Next; edge != this; edge = edge.Next)
                    yield return edge;
            }
        }

        public class EdgeAngleComparer : Comparer<Edge>
        {
            public override int Compare(Edge x, Edge y) => (x.B - x.A).Angle.CompareTo((y.B - y.A).Angle);
        }

        public readonly static EdgeAngleComparer AngleComparer = new EdgeAngleComparer();
    }
}