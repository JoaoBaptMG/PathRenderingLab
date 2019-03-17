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
                if (ca.IsConvex == cb.IsConvex) return false;

                return true;
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
            var curves = new LinkedList<LinkedListNode<Curve>>();
            foreach (var contour in contourLists)
                for (var cnode = contour.First; cnode != null; cnode = cnode.Next)
                    curves.AddLast(cnode);

            bool subdivide = true;
            // Iterate for each convex-concave pair until no subdivisions are needed anymore
            while (subdivide)
            {
                subdivide = false;

                for (var node1 = curves.First; node1 != null; node1 = node1.Next)
                {
                    // Skip degenerate curves
                    if (node1.Value.Value.IsDegenerate) continue;

                    for (var node2 = node1.Next; node2 != null; node2 = node2.Next)
                    {
                        // Skip degenerate curves
                        if (node2.Value.Value.IsDegenerate) continue;

                        // Skip curves that are eligible to double curve
                        if (AreCurvesFusable(node1.Value.Value, node2.Value.Value)) continue;

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
                            var intersectionInfo = GetIntersectionInfoFromCurves(node1.Value.Value, node2.Value.Value);

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
                                SubdivideCurveInHalf(node1);
                                SubdivideCurveInHalf(node2);
                            }
                            else if (l1 && !k1) SubdivideCurveInHalf(node1);
                            else if (l2 && !k2) SubdivideCurveInHalf(node2);
                            else if (k1) SubdivideCurveInHalf(node2);
                            else if (k2) SubdivideCurveInHalf(node1);
                            else Debug.Assert(false, "This should not happen!");

                            // If we generate degenerate curves, bail out too
                            if (node1.Value.Value.IsDegenerate || node2.Value.Value.IsDegenerate) break;
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
