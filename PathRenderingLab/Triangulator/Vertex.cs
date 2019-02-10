using System;
using Bitlush;

namespace PathRenderingLab.Triangulator
{

    /// <summary>
    /// Denotes the type of the vertex, used on the triangulation routine.
    /// </summary>
    public enum VertexType { Start, End, Split, Merge, RegularLeft, RegularRight };

    /// <summary>
    /// A vertex on the polygon. It has a type, and it stores its current, previous and next vectors
    /// </summary>
    public class Vertex : IComparable<Vertex>
    {
        public readonly VertexType Type;
        public readonly Double2 Current;

        public AvlTree<Edge, Edge> OutgoingEdges, IncomingEdges;
        public Edge NextEdge, PreviousEdge;

        public Vertex(Double2 prev, Double2 cur, Double2 next)
        {
            Current = cur;
            NextEdge = new Edge(cur, next);

            OutgoingEdges = new AvlTree<Edge, Edge>(Edge.AngleComparer);
            OutgoingEdges.Insert(NextEdge, NextEdge);

            IncomingEdges = new AvlTree<Edge, Edge>(Edge.AngleComparer);

            // Comparison variables
            int comparisonCP = GeneralizedComparer.Default.Compare(cur, prev);
            int comparisonCN = GeneralizedComparer.Default.Compare(cur, next);
            bool reflex = (prev - cur).AngleBetween(next - cur) > 0;

            // Compute the type of the vertex
            // Both lie below:
            if (comparisonCP > 0 && comparisonCN > 0)
                Type = reflex ? VertexType.Split : VertexType.Start;
            // Both lie above:
            else if (comparisonCP < 0 && comparisonCN < 0)
                Type = reflex ? VertexType.Merge : VertexType.End;
            // We have to take account if the vertex is on the outer or the inner contour
            else Type = comparisonCN > 0 ? VertexType.RegularLeft : VertexType.RegularRight;
        }

        public int CompareTo(Vertex other) => GeneralizedComparer.Default.Compare(Current, other.Current);

        public override string ToString() => $"{Current}, {Type}";

        public bool SearchOutgoingEdges(Edge e, out Edge eli, out Edge eri)
        {
            if (OutgoingEdges.SearchLeftRight(e, out eli, out eri))
                return true;

            // Try to mimic a cyclical edge list
            if (eli == null) eli = OutgoingEdges.Last;
            if (eri == null) eri = OutgoingEdges.First;

            return false;
        }

        public bool SearchIncomingEdges(Edge e, out Edge eli, out Edge eri)
        {
            if (IncomingEdges.SearchLeftRight(e, out eli, out eri))
                return true;

            // Try to mimic a cyclical edge list
            if (eli == null) eli = IncomingEdges.Last;
            if (eri == null) eri = IncomingEdges.First;

            return false;
        }
    }

    public struct ChainVertex : IComparable<ChainVertex>
    {
        public readonly Double2 Position;
        public readonly VertexType Type;

        public ChainVertex(Double2 pos, VertexType type)
        {
            Position = pos;
            Type = type;
        }

        public int CompareTo(ChainVertex other) => GeneralizedComparer.Default.Compare(Position, other.Position);

        public override string ToString() => $"{Position} {Type}";
    }
}