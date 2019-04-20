using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents
{
    /// <summary>
    /// The class that represents an SVG group (aka the g node, but also the svg node)
    /// </summary>
    public class SvgGroup : SvgNode
    {
        /// <summary>
        /// Internal list of children
        /// </summary>
        List<SvgNode> children;

        public ReadOnlyCollection<SvgNode> Children => children.AsReadOnly();
        public readonly bool Renderable;

        public SvgGroup(XmlNode node, SvgNode parent, Svg svg, bool renderable = true) : base(node, parent, svg)
        {
            children = new List<SvgNode>();
            Renderable = renderable;
            ProcessChildren(node);
        }

        protected virtual void ProcessChildren(XmlNode node)
        {
            // Add all the children present
            foreach (var child in node.ChildElements())
                ProcessChildNode(child);
        }

        protected void ProcessChildNode(XmlNode child, SvgUse overrideNode = null)
        {
            // Check the name of the node to find the right type of SVG node to create
            // First, treat everything which isn't on the SVG namespace as unknown
            if (child.NamespaceURI != Svg.Namespace)
                children.Add(new SvgGroup(child, this, svg));
            // Now, check the names
            else switch (child.LocalName)
                {
                    // Path
                    case "path": children.Add(new SvgPath(child, this, svg)); break;

                    // Basic shapes
                    case "rect": children.Add(new BasicShapes.SvgRect(child, this, svg)); break;
                    case "circle": children.Add(new BasicShapes.SvgCircle(child, this, svg)); break;
                    case "ellipse": children.Add(new BasicShapes.SvgEllipse(child, this, svg)); break;
                    case "line": children.Add(new BasicShapes.SvgLine(child, this, svg)); break;
                    case "polyline": children.Add(new BasicShapes.SvgPolyline(child, this, svg)); break;
                    case "polygon": children.Add(new BasicShapes.SvgPolygon(child, this, svg)); break;

                    // "Definitions container"
                    case "defs": children.Add(new SvgGroup(child, this, svg, false)); break;

                    // Symbols and SVGs
                    case "svg": children.Add(new SvgSizedGroup(child, this, svg, true, overrideNode)); break;
                    case "symbol": children.Add(new SvgSizedGroup(child, this, svg, false, overrideNode)); break;

                    // Use (aka inject data into the code)
                    case "use": children.Add(new SvgUse(child, this, svg)); break;

                    // Skip metadata
                    case "metadata": break; // Skip

                    // Anything else
                    default: children.Add(new SvgGroup(child, this, svg)); break;
                }
        }
    }
}
