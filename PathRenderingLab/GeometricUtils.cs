using System;
using System.Collections.Generic;
using System.Linq;
using static PathRenderingLab.DoubleUtils;

namespace PathRenderingLab
{
    public struct FloatRectangle
    {
        public float X, Y, Width, Height;
        public FloatRectangle(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public bool Intersects(FloatRectangle o) => !(X > o.X + o.Width || o.X > X + Width || Y > o.Y + o.Height || o.Y > Y + Height);

        public override string ToString() => $"{X} {Y} {Width} {Height}";
    }

    public struct DoubleRectangle
    {
        public double X, Y, Width, Height;
        public DoubleRectangle(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;

            if (Width < 0)
            {
                X += Width;
                Width = -Width;
            }

            if (Height < 0)
            {
                Y += Height;
                Height = -Height;
            }
        }

        public bool Intersects(DoubleRectangle o) => !(X > o.X + o.Width || o.X > X + Width || Y > o.Y + o.Height || o.Y > Y + Height);

        public DoubleRectangle Intersection(DoubleRectangle o)
        {
            if (!Intersects(o)) return new DoubleRectangle(double.NaN, double.NaN, double.NaN, double.NaN);

            var x1 = Math.Max(X, o.X);
            var x2 = Math.Min(X + Width, o.X + o.Width);
            var y1 = Math.Max(Y, o.Y);
            var y2 = Math.Min(Y + Height, o.Y + o.Height);

            return new DoubleRectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public bool ContainsCompletely(DoubleRectangle o)
            => X <= o.X && Y <= o.Y && X + Width >= o.X + o.Width && Y + Height >= o.Y + o.Height;

        public bool ContainsPoint(Double2 v) => X <= v.X && Y <= v.Y && X + Width >= v.X && Y + Height >= v.Y;

        public DoubleRectangle Truncate()
        {
            var x1 = X.Truncate();
            var y1 = Y.Truncate();
            var x2 = (X + Width).TruncateCeiling();
            var y2 = (Y + Height).TruncateCeiling();

            return new DoubleRectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public override string ToString() => $"{X} {Y} {Width} {Height}";
    }

    public struct RootPair
    {
        public double A, B;
        public RootPair(double a, double b) { A = a; B = b; }

        public RootPair Flip() => new RootPair(B, A);
        public override string ToString() => $"{A} {B}";

        public bool Inside01() => GeometricUtils.Inside01(A) && GeometricUtils.Inside01(B);
    }

    /// <summary>
    /// A generalized comparer for vectors, used in geometric routines
    /// </summary>
    public class CanonicalComparer : Comparer<Double2>
    {
        public override int Compare(Double2 a, Double2 b) => a.Y == b.Y ? b.X.CompareTo(a.X) : a.Y.CompareTo(b.Y);
        public new readonly static CanonicalComparer Default = new CanonicalComparer();
    }

    public static class GeometricUtils
    {
        public static bool Inside01(double t) => t >= 0 && t <= 1;

        // Check if segments are inside interval
        public static bool InsideSegmentCollinear(Double2 x0, Double2 x1, Double2 y, bool strict)
        {
            var d = (x1 - x0).Dot(y - x0);
            return strict ? d > 0 && d < (x1 - x0).LengthSquared
                : d >= 0 && d <= (x1 - x0).LengthSquared;
        }

        public static bool SegmentsIntersect(Double2 p0, Double2 p1, Double2 q0, Double2 q1, bool strict)
        {
            // The cross products
            double crossq0 = (p1 - p0).Cross(q0 - p0);
            double crossq1 = (p1 - p0).Cross(q1 - p0);
            double crossp0 = (q1 - q0).Cross(p0 - q0);
            double crossp1 = (q1 - q0).Cross(p1 - q0);

            // If two points are equal, we have only containment (not considered in strict case)
            if (RoughlyEquals(p0, p1))
                return !strict && RoughlyZeroSquared(crossp0) && InsideSegmentCollinear(q0, q1, p0, strict);
            if (RoughlyEquals(q0, q1))
                return !strict && RoughlyZeroSquared(crossq0) && InsideSegmentCollinear(p0, p1, q0, strict);

            // Point coincidence is considered a false result on strict mode
            if (strict && (RoughlyEquals(p0, q0) || RoughlyEquals(p0, q1) || RoughlyEquals(q0, p0) || RoughlyEquals(q0, p1)))
                return false;

            // Containment (not considered on strict mode)
            if (RoughlyZeroSquared(crossq0)) return !strict && InsideSegmentCollinear(p0, p1, q0, strict);
            if (RoughlyZeroSquared(crossq1)) return !strict && InsideSegmentCollinear(p0, p1, q1, strict);
            if (RoughlyZeroSquared(crossp0)) return !strict && InsideSegmentCollinear(q0, q1, p0, strict);
            if (RoughlyZeroSquared(crossp1)) return !strict && InsideSegmentCollinear(q0, q1, p1, strict);

            // Check if everything is on one side
            if (crossq0 < 0 && crossq1 < 0) return false;
            if (crossq0 > 0 && crossq1 > 0) return false;
            if (crossp0 < 0 && crossp1 < 0) return false;
            if (crossp0 > 0 && crossp1 > 0) return false;

            // Otherwise...
            return true;
        }

        public static bool PolygonsOverlap(Double2[] poly0, Double2[] poly1, bool strict)
        {
            // Check first for degenerate triangles
            var s0 = SegmentEquivalent(poly0);
            var s1 = SegmentEquivalent(poly1);

            if (s0.Length == 2 && s1.Length == 2)
                return SegmentsIntersect(s0[0], s0[1], s1[0], s1[1], strict);
            else if (s0.Length == 2)
                return PolygonSegmentIntersect(poly1, s0[0], s0[1], strict);
            else if (s1.Length == 2)
                return PolygonSegmentIntersect(poly0, s1[0], s1[1], strict);

            // Check for segments intersection
            for (int j = 0; j < poly0.Length; j++)
                for (int i = 0; i < poly1.Length; i++)
                {
                    var p0 = poly0[j];
                    var q0 = poly1[i];

                    var p1 = poly0[j == 0 ? poly0.Length - 1 : j - 1];
                    var q1 = poly1[i == 0 ? poly1.Length - 1 : i - 1];

                    if (SegmentsIntersect(p0, p1, q0, q1, strict))
                        return true;
                }

            // Check for overlapping of any of the points
            if (poly0.Any(p => PolygonContainsPoint(poly1, p, strict)) || poly1.Any(p => PolygonContainsPoint(poly0, p, strict)))
                return true;

            // Otherwise...
            return false;
        }

        public static bool PolygonSegmentIntersect(Double2[] poly, Double2 a, Double2 b, bool strict)
        {
            // Check for segments intersection
            for (int i = 0; i < poly.Length; i++)
            {
                var p0 = poly[i];
                var p1 = poly[i == 0 ? poly.Length - 1 : i - 1];

                if (SegmentsIntersect(p0, p1, a, b, strict)) return true;
            }

            // Check for overlapping of the segment point
            if (PolygonContainsPoint(poly, a, strict) || PolygonContainsPoint(poly, b, strict))
                return true;

            // Otherwise...
            return false;
        }

        public static double PolygonWinding(Double2[] poly)
        {
            double winding = 0;

            for (int i = 0; i < poly.Length; i++)
                winding += poly[i].Cross(poly[(i + 1) % poly.Length]);

            return winding;
        }

        public static Double2[] SegmentEquivalent(Double2[] poly)
        {
            // If the polygon is already a segment or its winding is non-negligible, just return
            if (poly.Length == 2 || !RoughlyZeroSquared(PolygonWinding(poly))) return poly;

            // Else, build the segment
            var imin = 0;
            var imax = 0;

            for (int i = 1; i < poly.Length; i++)
            {
                if (poly[imin].X > poly[i].X) imin = i;
                if (poly[imax].X < poly[i].X) imax = i;
            }

            return new[] { poly[imin], poly[imax] };
        }

        public static bool PolygonContainsPoint(Double2[] poly, Double2 p, bool strict)
        {
            bool contains = false;

            for (int i = 0; i < poly.Length; i++)
            {
                var p0 = poly[i];
                var p1 = poly[i == 0 ? poly.Length - 1 : i - 1];

                // If the two points are equal, skip
                if (RoughlyEquals(p0, p1)) continue;

                // For strictness, if the line is "inside" the polygon, we have a problem
                if (strict && RoughlyZeroSquared((p1 - p0).Cross(p - p0)) &&
                    InsideSegmentCollinear(p0, p1, p, false)) return false;

                if (p0.X < p.X && p1.X < p.X) continue;
                if (p0.X < p.X) p0 = p1 + (p.X - p1.X) / (p0.X - p1.X) * (p0 - p1);
                if (p1.X < p.X) p1 = p0 + (p.X - p0.X) / (p1.X - p0.X) * (p1 - p0);
                if ((p0.Y >= p.Y) != (p1.Y >= p.Y)) contains = !contains;
            }

            return contains;
        }

        public static Double2[] SimplifyPolygon(Double2[] polygon)
        {
            // Quickly discard degenerate polygons
            if (polygon.Length < 3) return polygon;

            // Check if they follow the same direction
            bool SameDirection(Double2 u, Double2 v) => RoughlyZeroSquared(u.Cross(v)) && u.Dot(v) >= 0;

            // Find a non-collinear polygon first
            int istart;
            int len = polygon.Length;
            for (istart = 0; istart < len; istart++)
            {
                var ik = (istart + 1) % len;
                var ip = (istart + len - 1) % len;

                if (!SameDirection(polygon[ik] - polygon[istart], polygon[ip] - polygon[istart])) break;
            }

            // If there are no polygons non-collinear polygons, just return a line
            if (istart == len)
            {
                var imin = 0;
                var imax = 0;

                for (int i = 1; i < len; i++)
                {
                    if (polygon[imin].X > polygon[i].X) imin = i;
                    if (polygon[imax].X < polygon[i].X) imax = i;
                }

                return new[] { polygon[imin], polygon[imax] };
            }
            else
            {
                // Start with a single point
                var points = new List<Double2>(len) { polygon[istart] };
                Double2 LastAddedPoint() => points[points.Count - 1];

                // Only add the point if it doesn't form a parallel line with the next point on the line
                for (int i = (istart + 1) % len; i != istart; i = (i + 1) % len)
                    if (!SameDirection(polygon[(i + 1) % len] - polygon[i], LastAddedPoint() - polygon[i]))
                        points.Add(polygon[i]);

                // Return the new formed polygon
                return points.ToArray();
            }
        }

        public static Double2[] CircleIntersection(Double2 c1, double r1, Double2 c2, double r2)
        {
            // Firstly, rotate the second circle so it stands on the X-axis
            var rot = (c2 - c1).Normalized;

            // Get the displacement
            var a = (c2 - c1).Dot(rot);
            var x = (r1 * r1 + a * a - r2 * r2) / (2 * a);
            var ys = r1 * r1 - x * x;

            // No intersection if this is negative
            if (ys < 0) return new Double2[0];
            else
            {
                var y = Math.Sqrt(ys);
                var pos = new Double2[] { new Double2(x, y), new Double2(x, -y) };

                // Project back to the new position
                return pos.Select(p => c1 + p.RotScale(rot)).ToArray();
            }
        }

        public static Double2[] CircleLineIntersection(Double2 c1, double r1, Double2 c, Double2 v)
        {
            // Firstly, rotate the second circle so it stands on the X-axis
            var rot = v.Normalized;

            // Get the displacement
            var y = -(c - c1).Cross(rot);
            var xs = r1 * r1 - y * y;

            // No intersection if this is negative
            if (xs < 0) return new Double2[0];
            else
            {
                var x = Math.Sqrt(xs);
                var pos = new Double2[] { new Double2(x, y), new Double2(-x, y) };

                // Project back to the new position
                return pos.Select(p => c1 + p.RotScale(rot)).ToArray();
            }
        }

        public static Double2[] ConvexHull(Double2[] points)
        {
            // Sort the points using the canonical comparer
            Array.Sort(points, CanonicalComparer.Default);
            ArrayExtensions.RemoveDuplicates(ref points);

            var hull = new List<Double2>(points.Length);
            // Work with the points array forwards and backwards
            for (int n = 0; n < 2; n++)
            {
                var hullPart = new List<Double2>(points.Length / 2);

                // Add the first two points
                hullPart.Add(points[0]);
                hullPart.Add(points[1]);

                // Run through the array
                for (int i = 2; i < points.Length; i++)
                {
                    // Rollback the possible vertices
                    while (hullPart.Count > 1 &&
                        (hullPart[hullPart.Count - 1] - hullPart[hullPart.Count - 2])
                        .Cross(points[i] - hullPart[hullPart.Count - 1]) > 0)
                        hullPart.RemoveAt(hullPart.Count - 1);

                    // Add the vertex
                    hullPart.Add(points[i]);
                }

                // Remove the last vertex
                hullPart.RemoveAt(hullPart.Count - 1);
                hull.AddRange(hullPart);
                Array.Reverse(points);
            }

            // Reverse the point orientation
            hull.Reverse();
            return hull.ToArray();
        }

        public static Double2[] EnsureCounterclockwise(Double2[] poly)
        {
            // Calculate the polygon's winding
            double winding = 0;

            for (int i = 0; i < poly.Length; i++)
                winding += poly[i].Cross(poly[(i + 1) % poly.Length]);

            // Reverse the polygon if the winding is clockwise
            if (winding < 0) poly.Reverse().ToArray();
            return poly;
        }
    }
}
