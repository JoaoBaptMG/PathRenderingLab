using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PathRenderingLab.PathCompiler
{
    class CurveIntersectionInfo
    {
        public Double2[] InteriorPoints1, InteriorPoints2;
        public bool CurveIntersects1, CurveIntersects2;

        public CurveIntersectionInfo()
        {
            CurveIntersects1 = false;
            CurveIntersects2 = false;
        }
    }

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

                // 2) The tangents on that endpoint must be similar
                if (ca.ExitTangent.Dot(cb.EntryTangent) >= -0.99) return false;

                // 3) Both must not have the same convexity
                return ca.IsConvex != cb.IsConvex;
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

                TestListSubdivision(convexCurves, concaveCurves);
                TestListSubdivision(convexCurves, convexCurves);

                void TestListSubdivision(LinkedList<LinkedListNode<Curve>> list1, LinkedList<LinkedListNode<Curve>> list2)
                {
                    for (var onode = list1.First; onode != null; onode = onode.Next)
                    {
                        // Skip degenerate curves
                        if (onode.Value.Value.IsDegenerate) continue;

                        for (var inode = list2.First; inode != null; inode = inode.Next)
                        {
                            // Skip equal curves
                            if (inode.Value.Value == onode.Value.Value) continue;

                            // Skip degenerate curves
                            if (inode.Value.Value.IsDegenerate) continue;

                            // A function for subdividing the curves
                            void SubdivideCurveInHalf(LinkedListNode<LinkedListNode<Curve>> node)
                            {
                                var curve = node.Value.Value;
                                node.List.AddAfter(node, node.Value.List.AddAfter(node.Value, curve.Subcurve(0.5, 1)));
                                node.Value.Value = curve.Subcurve(0, 0.5);
                            }

                            while (true)
                            {
                                // Get the curve intersection info for the pair
                                var intersectionInfo = GetIntersectionInfoFromCurves(onode.Value.Value, inode.Value.Value);

                                // If there is no intersection, bail out
                                if (intersectionInfo == null) break;
                                subdivide = true;

                                // Get the case switches for it
                                var interiorPoints1 = intersectionInfo.InteriorPoints1;
                                var interiorPoints2 = intersectionInfo.InteriorPoints2;
                                var l1 = interiorPoints1.Length > 0;          // Polygon 1 has points from polygon 2 in its interior
                                var l2 = interiorPoints2.Length > 0;          // Polygon 2 has points from polygon 1 in its interior
                                var k1 = intersectionInfo.CurveIntersects1;   // Curve 1 intersects polygon 2
                                var k2 = intersectionInfo.CurveIntersects2;   // Curve 2 intersects polygon 1

                                if ((!l1 && !l2 && !k1 && !k2) || (l1 && l2) || (k1 && k2))
                                {
                                    SubdivideCurveInHalf(onode);
                                    SubdivideCurveInHalf(inode);
                                }
                                else if (l1 && !k1) SubdivideCurveInHalf(onode);
                                else if (l2 && !k2) SubdivideCurveInHalf(inode);
                                else if (k1) SubdivideCurveInHalf(inode);
                                else if (k2) SubdivideCurveInHalf(onode);
                                else Debug.Assert(false, "This should not happen!");

                                // If we generate degenerate curves, bail out too
                                if (onode.Value.Value.IsDegenerate || inode.Value.Value.IsDegenerate) break;
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

            // When all subdivisions are done, we can return the result, filtering out the degenerate curves
            return new FillFace(contourLists.Select(list => list.Where(c => !c.IsDegenerate).ToArray()).ToArray());
        }

        static double CombinedWindings(Curve convex, Curve concave)
            => convex.Winding + convex.At(1).Cross(concave.At(0)) + concave.Winding + concave.At(1).Cross(convex.At(0));

        static CurveIntersectionInfo GetIntersectionInfoFromCurves(Curve c1, Curve c2)
        {
            // Utility function to test for intersection from convex polygon
            bool StrictlyInsideConvexPolygon(Double2[] poly, Double2 pt)
            {
                var length = poly.Length;
                for (int i = 0; i < length; i++)
                {
                    var ik = (i + 1) % length;
                    if ((poly[ik] - poly[i]).Cross(pt - poly[i]) <= 0)
                        return false;
                }
                return true;
            }

            // Utility function to test for intersection with curve
            bool CurveIntersects(Double2[] poly, Curve curve)
            {
                var length = poly.Length;
                for (int i = 0; i < length; i++)
                {
                    var ik = (i + 1) % length;
                    if (Curve.Intersections(curve, Curve.Line(poly[i], poly[ik])).Any())
                        return true;
                }
                return false;
            }

            // Get the curves' enclosing polygons
            var p1 = GeometricUtils.EnsureCounterclockwise(c1.EnclosingPolygon);
            var p2 = GeometricUtils.EnsureCounterclockwise(c2.EnclosingPolygon);

            // If there is no intersection, no info
            if (!GeometricUtils.PolygonsOverlap(p1, p2, true)) return null;

            return new CurveIntersectionInfo
            {
                InteriorPoints1 = p2.Where(p => StrictlyInsideConvexPolygon(p1, p)).ToArray(),
                InteriorPoints2 = p1.Where(p => StrictlyInsideConvexPolygon(p2, p)).ToArray(),
                CurveIntersects1 = CurveIntersects(p2, c1),
                CurveIntersects2 = CurveIntersects(p1, c2)
            };
        }
    }
}
