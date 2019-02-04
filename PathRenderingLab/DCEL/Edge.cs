using System.Collections.Generic;

namespace PathRenderingLab.DCEL
{
    // An edge with cached vertices - we'll come back here later
    public class Edge
    {
        // Cached curve and outer angles
        public readonly Curve Curve;
        public readonly OuterAngles OuterAngles;

        // Cached endpoints
        public readonly Vertex E1, E2;

        // This edge's twin, next and previous edges in the graph
        public Edge Twin, Next, Previous;

        // The face this edge is incident
        public Face Face;

        // Canonicity of the edge (how many times the edge appears on the original path)
        public int Canonicity;

        public Edge(Vertex e1, Vertex e2, Curve curve)
        {
            E1 = e1;
            E2 = e2;
            Curve = curve;
            OuterAngles = curve.OuterAngles;
            Canonicity = 0;
        }

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

        /// <summary>
        /// The particular edge winding - a measure of how much counterclockwise it is.
        /// It is basically the signed area betwen the curve and two lines that go from the origin to the endpoints.
        /// </summary>
        public double Winding => Curve.Winding;

        public override string ToString() => $"{Curve.ToString()} / {OuterAngles.ToString()}";

        public PathCommand PathCommandFrom() => Curve.PathCommandFrom();
    }
}