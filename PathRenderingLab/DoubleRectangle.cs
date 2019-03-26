using System;

namespace PathRenderingLab
{
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

        public bool StrictlyIntersects(DoubleRectangle o) =>
            !(X >= o.X + o.Width || o.X >= X + Width || Y >= o.Y + o.Height || o.Y >= Y + Height);

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

        public static DoubleRectangle operator *(double s, DoubleRectangle r)
            => new DoubleRectangle(s * r.X, s * r.Y, s * r.Width, s * r.Height);

        public static DoubleRectangle operator *(DoubleRectangle r, double s)
            => new DoubleRectangle(r.X * s, r.Y * s, r.Width * s, r.Height * s);

        public static DoubleRectangle operator /(DoubleRectangle r, double s)
            => new DoubleRectangle(r.X / s, r.Y / s, r.Width / s, r.Height / s);

        public override string ToString() => $"{X} {Y} {Width} {Height}";
    }
}