using System;

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

        public bool Intersects(FloatRectangle o) => !(X > o.X + o.Width || o.X > X + Width || Y > o.Y + o.Height || o.Y > Y + Height);

        public bool StrictlyIntersects(FloatRectangle o) =>
            !(X >= o.X + o.Width || o.X >= X + Width || Y >= o.Y + o.Height || o.Y >= Y + Height);

        public FloatRectangle Intersection(FloatRectangle o)
        {
            if (!Intersects(o)) return new FloatRectangle(float.NaN, float.NaN, float.NaN, float.NaN);

            var x1 = (float)Math.Max(X, o.X);
            var x2 = (float)Math.Min(X + Width, o.X + o.Width);
            var y1 = (float)Math.Max(Y, o.Y);
            var y2 = (float)Math.Min(Y + Height, o.Y + o.Height);

            return new FloatRectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public bool ContainsCompletely(FloatRectangle o)
            => X <= o.X && Y <= o.Y && X + Width >= o.X + o.Width && Y + Height >= o.Y + o.Height;

        public bool ContainsPoint(Double2 v) => X <= v.X && Y <= v.Y && X + Width >= v.X && Y + Height >= v.Y;

        public static FloatRectangle operator *(float s, FloatRectangle r)
            => new FloatRectangle(s * r.X, s * r.Y, s * r.Width, s * r.Height);

        public static FloatRectangle operator *(FloatRectangle r, float s)
            => new FloatRectangle(r.X * s, r.Y * s, r.Width * s, r.Height * s);

        public static FloatRectangle operator /(FloatRectangle r, float s)
            => new FloatRectangle(r.X / s, r.Y / s, r.Width / s, r.Height / s);

        public override string ToString() => $"{X} {Y} {Width} {Height}";
    }
}