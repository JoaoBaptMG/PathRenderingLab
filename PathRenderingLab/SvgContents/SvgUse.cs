using System;
using System.Collections.Generic;
using System.Xml;

namespace PathRenderingLab.SvgContents
{
    public class SvgUse : SvgGroup, ISvgPostResolve
    {
        public double Width { get; private set; }

        public double Height { get; private set; }

        public string Href { get; private set; }

        public SvgUse(XmlNode node, SvgNode parent, Svg svg) : base(node, parent, svg)
        {

        }

        // Disable children processing
        protected override void ProcessChildren(XmlNode node) { }

        protected override void Parse(XmlNode node, Dictionary<string, string> properties)
        {
            base.Parse(node, properties);

            // Parse the "x" and "y" used to translate to a new point
            var x = ParseLengthX(properties.GetOrDefault("x")) ?? 0;
            var y = ParseLengthY(properties.GetOrDefault("y")) ?? 0;

            // Append the translation
            var transform = Transform;
            Array.Resize(ref transform, transform.Length + 1);
            transform[transform.Length - 1] = TransformFunction.Translate(x, y);
            Transform = transform;

            // Parse the "width" and "height"
            Width = ParseLengthX(properties.GetOrDefault("width")) ?? 0;
            Height = ParseLengthY(properties.GetOrDefault("height")) ?? 0;

            // Parse "href"
            Href = SvgUtils.ParseHref(properties.GetOrDefault("href"));
        }

        // Expand the "children" by resolving the reference
        public void PostResolve()
        {
            // Just ignore if the ID doesn't exist
            if (!string.IsNullOrWhiteSpace(Href) && svg.XmlNodesById.ContainsKey(Href.Trim()))
                ProcessChildNode(svg.XmlNodesById[Href.Trim()], this);
        }
    }
}