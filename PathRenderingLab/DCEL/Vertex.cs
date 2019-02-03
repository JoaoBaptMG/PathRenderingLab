using Bitlush;

namespace PathRenderingLab.DCEL
{

    // Stored vertex data: outgoing vertices stored by angle order (counter-clockwise order)
    public class Vertex
    {
        public Double2 Position;
        public AvlTree<Edge, Edge> OutgoingEdges;

        public Vertex(Double2 pos)
        {
            Position = pos;
            OutgoingEdges = new AvlTree<Edge, Edge>(Edge.CCWComparer);
        }

        public bool SearchOutgoingEdges(Edge e, out Edge eli, out Edge eri)
        {
            if (OutgoingEdges.SearchLeftRight(e, out eli, out eri))
                return true;

            // Try to mimic a cyclical edge list
            if (eli == null) eli = OutgoingEdges.Last;
            if (eri == null) eri = OutgoingEdges.First;

            return false;
        }
    }
}