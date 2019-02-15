using System.Collections.Generic;
using System.Linq;

namespace PathRenderingLab
{
    /// <summary>
    /// Represents a face to be filled with a specific fill mode.
    /// It has an outer contor (counterclockwise) and inner contours (clockwise).
    /// It still does not represent the triangularization needed to do it.
    /// </summary>
    public class FillFace
    {
        /// <summary>
        /// The face's contours. The outer contour is arranged in counterclockwise order and the inner contours are arranged
        /// in clockwise order.
        /// </summary>
        public readonly Curve[][] Contours;

        public FillFace(Curve[][] contours)
        {
            Contours = contours;
        }

        /// <summary>
        /// The path resulting from this face.
        /// </summary>
        public Path Path
        {
            get
            {
                var list = new List<PathCommand>();

                foreach (var contour in Contours)
                {
                    list.Add(PathCommand.MoveTo(contour[0].At(0)));
                    list.AddRange(contour.Select(e => e.PathCommandFrom()));
                    list.Add(PathCommand.ClosePath());
                }

                return new Path { PathCommands = list.ToArray() };
            }
        }

        // Check if two curves are eligible for double curve promotion
        public static bool AreCurvesFusable(Curve c1, Curve c2)
        {
            bool Eligible(Curve ca, Curve cb)
            {
                // 1) The curves must have a common endpoint
                if (!DoubleUtils.RoughlyEquals(ca.At(1), cb.At(0))) return false;

                // 2) The tangents on that endpoind must be similar
                if (ca.ExitTangent.Dot(cb.EntryTangent) >= -0.95) return false;

                // 3) Both must not have the same convexity
                if (ca.IsConvex == cb.IsConvex) return false;

                // 4) They must describe a positive winding on the plane
                var pth = (ca.At(0) + cb.At(1)) / 2;
                return ca.WindingRelativeTo(pth) + cb.WindingRelativeTo(pth) > 0;
            }

            // If any combination is eligible, return true
            return Eligible(c1, c2) || Eligible(c2, c1);
        }

        /// <summary>
        /// Subdivide overlapping non-line curves on this face in order to ease for
        /// the path triangulator
        /// </summary>
        /// <returns></returns>
        public FillFace SubdivideOverlapping()
        {
            // Linked lists to store the curves
            var contourLists = Contours.Select(c => new LinkedList<Curve>(c)).ToArray();

            // Divide the curves into convex and concave curves
            var convexCurves = new LinkedList<LinkedListNode<Curve>>();
            var concaveCurves = new LinkedList<LinkedListNode<Curve>>();
            foreach (var contour in contourLists)
                for (var cnode = contour.First; cnode != null; cnode = cnode.Next)
                {
                    var curve = cnode.Value;

                    // For the sake of this test, lines are considered concave curves
                    if (curve.Type != CurveType.Line && curve.IsConvex) convexCurves.AddLast(cnode);
                    else concaveCurves.AddLast(cnode);
                }

            bool subdivide = true;
            // Iterate for each convex-concave pair until no subdivisions are needed anymore
            while (subdivide)
            {
                subdivide = false;

                for (var onode = convexCurves.First; onode != null; onode = onode.Next)
                {
                    // The curves and their enclosing polygons
                    var convex = onode.Value.Value;
                    var cvPoly = convex.EnclosingPolygon;
                    var cvstr = Triangulator.YMonotone.PolygonRepresentation(cvPoly);

                    // Skip degenerate curves
                    if (convex.IsDegenerate) continue;

                    for (var inode = concaveCurves.First; inode != null; inode = inode.Next)
                    {
                        retry: // Return-to label if the concave curve needs to be subdivided further
                        var concave = inode.Value.Value;
                        var ccPoly = concave.EnclosingPolygon;
                        var ccstr = Triangulator.YMonotone.PolygonRepresentation(ccPoly);

                        // Skip degenerate curves
                        if (concave.IsDegenerate) continue;

                        // Do not try to check eligible curves in subdivision
                        if (AreCurvesFusable(convex, concave)) continue;

                        // Check each of the concave polygon's vertices for overlap
                        foreach (var v in ccPoly.Reverse())
                        {
                            // Get the segment that joins the point to the nearest point on the curve
                            double wk = concave.NearestPointTo(v);
                            var w = concave.At(wk);

                            // If the segment intersects the polygon, do the subdivision
                            if (GeometricUtils.PolygonSegmentIntersect(cvPoly, v, w))
                            {
                                double t = convex.NearestPointTo(v);
                                var p = convex.At(t);

                                // The only way for the nearest point to be one of the convex curve's endpoints
                                // is if it is equal to one of the concave curve's endpoints too, skip this
                                if (DoubleUtils.RoughlyZero(t) || DoubleUtils.RoughlyEquals(t, 1)) continue;

                                subdivide = true;

                                // If the nearest point is below the convex curve, subdivide the convex curve
                                if ((p - v).Cross(convex.Derivative.At(t)) >= 0)
                                {
                                    var c1 = convex.Subcurve(0, t);
                                    var c2 = convex.Subcurve(t, 1);

                                    // Update the linked list accordingly
                                    onode.List.AddBefore(onode, onode.Value.List.AddBefore(onode.Value, c1));
                                    onode.Value.Value = c2;

                                    // Update the locally present values
                                    convex = c2;
                                    cvPoly = convex.EnclosingPolygon;
                                    cvstr = Triangulator.YMonotone.PolygonRepresentation(cvPoly);
                                }
                                else // Subdivide the concave curve
                                {
                                    double u = concave.NearestPointTo(p);

                                    var k1 = concave.Subcurve(0, u);
                                    var k2 = concave.Subcurve(u, 1);

                                    // Update the linked list
                                    inode.Value.Value = k1;
                                    inode.List.AddAfter(inode, inode.Value.List.AddAfter(inode.Value, k2));

                                    // Retry this iteration with the newly-divided curve
                                    goto retry;
                                }
                            }
                        }
                    }
                }
            }

            // Now, we are going to compare concave with line curves
            // Remove line curves from the concave curves and populate a list with the line curves
            var lineCurves = new LinkedList<LinkedListNode<Curve>>();
            for (var inode = concaveCurves.First; inode != null;)
            {
                var next = inode.Next;

                if (inode.Value.Value.Type == CurveType.Line)
                {
                    inode.List.Remove(inode.Value);
                    lineCurves.AddLast(inode.Value);
                }

                inode = next;
            }

            subdivide = true;
            // Iterate for each line-concave pair until no subdivisions are needed anymore
            while (subdivide)
            {
                subdivide = false;

                for (var lnode = lineCurves.First; lnode != null; lnode = lnode.Next)
                {
                    var line = lnode.Value.Value;
                    var a = line.A;
                    var b = line.B;

                    // Skip degenerate curves
                    if (line.IsDegenerate) continue;

                    for (var inode = concaveCurves.First; inode != null; inode = inode.Next)
                    {
                        retry: // Return-to label if the concave curve needs to be subdivided further 
                        var concave = inode.Value.Value;
                        var ccPoly = concave.EnclosingPolygon;

                        // Skip degenerate curves
                        if (concave.IsDegenerate) continue;

                        // Check first if the line's endpoints don't coincide with the curve's
                        // If they are, there is nothing left to do
                        if (DoubleUtils.RoughlyEquals(concave.At(0), b) ||
                            DoubleUtils.RoughlyEquals(concave.At(1), a)) continue;

                        // Now, for each vertex, check if is ouside the line
                        foreach (var v in ccPoly)
                        {
                            // Get the nearest point
                            double t = concave.NearestPointTo(v);
                            var p = concave.At(t);

                            // If at the endpoints, no need to subdivide it
                            if (DoubleUtils.RoughlyZero(t) || DoubleUtils.RoughlyEquals(t, 1)) continue;

                            // If the line connecting both intersects the original line, subdivide right there
                            if (GeometricUtils.SegmentsIntersect(a, b, p, v))
                            {
                                subdivide = true;

                                var c1 = concave.Subcurve(0, t);
                                var c2 = concave.Subcurve(t, 1);

                                // Update the linked list
                                inode.List.AddBefore(inode, inode.Value.List.AddBefore(inode.Value, c1));
                                inode.Value.Value = c2;

                                // Retry this iteration with the newly-divided curve
                                goto retry;
                            }
                        }

                        // Finnaly, we check if any point is inside the polygon
                        foreach (var v in new[] { a, b })
                            if (GeometricUtils.PolygonContainsPoint(ccPoly, v))
                            {
                                // If the nearest point is one of the endpoints, no need to subdivide it
                                double t = concave.NearestPointTo(v);
                                if (DoubleUtils.RoughlyZero(t) || DoubleUtils.RoughlyEquals(t, 1)) continue;

                                subdivide = true;

                                var c1 = concave.Subcurve(0, t);
                                var c2 = concave.Subcurve(t, 1);

                                // Update the linked list
                                inode.List.AddBefore(inode, inode.Value.List.AddBefore(inode.Value, c1));
                                inode.Value.Value = c2;

                                // Retry this iteration with the newly-divided curve
                                goto retry;
                            }
                    }
                }
            }

            foreach (var contour in contourLists)
            {
                // If the contour has less than 3 curves, no problem
                if (contour.Count < 3) continue;

                // We are going to check whether there are triples of double curves,
                // because we must subdivide them
                for (var node = contour.First; node != null; node = node.Next)
                {
                    // Get the previous and next node
                    var prev = node.Previous ?? contour.Last;
                    var next = node.Next ?? contour.First;

                    // If the triple is an eligible triple for fusion, subdivide the middle curve
                    if (AreCurvesFusable(prev.Value, node.Value) && AreCurvesFusable(node.Value, next.Value))
                    {
                        var c1 = node.Value.Subcurve(0, 0.5);
                        var c2 = node.Value.Subcurve(0.5, 1);

                        node.Value = c1;
                        contour.AddAfter(node, c2);
                    }
                }
            }

            // When all subdivisions are done, we can return the result
            return new FillFace(contourLists.Select(list => list.ToArray()).ToArray());
        }
    }
}
