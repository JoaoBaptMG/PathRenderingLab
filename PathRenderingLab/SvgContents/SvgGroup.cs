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

        public SvgGroup(XmlNode node, SvgNode parent, bool renderable = true) : base(node, parent)
        {
            children = new List<SvgNode>();

            Renderable = renderable;

            // Add all the children present
            foreach (var child in node.ChildElements())
                ProcessChildNode(child);
        }

        private void ProcessChildNode(XmlNode child)
        {
            // Check the name of the node to find the right type of SVG node to create
            // First, treat everything which isn't on the SVG namespace as unknown
            if (child.NamespaceURI != Svg.Namespace)
                children.Add(new SvgGroup(child, this));
            // Now, check the names
            else switch (child.LocalName)
                {
                    case "path": children.Add(new SvgPath(child, this)); break;
                    case "rect": children.Add(new BasicShapes.SvgRect(child, this)); break;
                    case "circle": children.Add(new BasicShapes.SvgCircle(child, this)); break;
                    case "ellipse": children.Add(new BasicShapes.SvgEllipse(child, this)); break;
                    case "line": children.Add(new BasicShapes.SvgLine(child, this)); break;
                    case "polyline": children.Add(new BasicShapes.SvgPolyline(child, this)); break;
                    case "polygon": children.Add(new BasicShapes.SvgPolygon(child, this)); break;
                    case "defs": children.Add(new SvgGroup(child, this, false)); break;
                    case "metadata": break; // Skip
                    default: children.Add(new SvgGroup(child, this)); break;
                }
        }
    }
}
