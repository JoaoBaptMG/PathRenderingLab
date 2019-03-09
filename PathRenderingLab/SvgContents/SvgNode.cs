using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PathRenderingLab.Parsers;

namespace PathRenderingLab.SvgContents
{
    public class SvgNode
    {
        /// <summary>
        /// The ID of the current node
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// The node's parent, of null if it is the root element
        /// </summary>
        public SvgNode Parent { get; private set; }

        /// <summary>
        /// The transform this node is submitted to
        /// </summary>
        public TransformFunction[] Transform { get; private set; }

        /// <summary>
        /// The path style applied to its node
        /// </summary>
        public PathStyle PathStyle { get; private set; }

        // The most general constructor
        protected SvgNode(XmlNode node, SvgNode parent, string transformAttributeName = "transform")
        {
            // Assign the ID for everyone
            Parent = parent;
            Id = node.SvgAttribute("id");

            // Build the property list
            var properties = new Dictionary<string, string>();

            // Go through all attributes
            foreach (XmlAttribute attr in node.Attributes)
            {
                // Skip if the attribute doesn't belong to the SVG namespace or no namespace
                if (!string.IsNullOrEmpty(attr.NamespaceURI) && attr.NamespaceURI != Svg.XmlNamespace) continue;

                // Skip if the attribute is style or id
                if (attr.LocalName == "style" || attr.LocalName == "id") continue;

                // Attach it to the properties
                if (attr.LocalName == transformAttributeName)
                    properties["transform"] = attr.Value;
                else properties[attr.LocalName] = attr.Value;
            }

            // Parse the CSS property list in style
            var styleProperties = node.SvgAttribute("style").ToPropertyList();

            foreach (var kvp in styleProperties)
                properties[kvp.Key] = kvp.Value;

            // Now, parse
            Parse(properties);
        }

        protected virtual void Parse(Dictionary<string, string> properties)
        {
            // Parse the transform
            Transform = TransformFunction.ParseCollectionFromString(properties.GetOrDefault("transform", "none"));
            if (Parent != null) Transform = Parent.Transform.Concat(Transform);

            // Parse the most general arguments: fill, fill-rule, stroke, stroke-width, stroke-linejoin, stroke-linecap
            PathStyle = new PathStyle(properties, Parent?.PathStyle);
        }
    }
}
