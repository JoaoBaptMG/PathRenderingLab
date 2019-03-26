using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents
{
    public class Svg
    {
        /// <summary>
        /// The XML Namespace for the SVG specification
        /// </summary>
        public static readonly string Namespace = "http://www.w3.org/2000/svg";

        public SvgSizedGroup Root { get; private set; }

        /// <summary>
        /// An associative list of all the ID'ed SVG nodes
        /// </summary>
        public Dictionary<string, SvgNode> NodesById;

        /// <summary>
        /// An associative list of all the ID'ed XML nodes that generate the ID
        /// </summary>
        public Dictionary<string, XmlNode> XmlNodesById;

        /// <summary>
        /// Constructs an SVG file representation from an XML node
        /// </summary>
        /// <param name="node">The node to be parsed for SVG</param>
        public Svg(XmlNode node)
        {
            Parse(node);
        }

        public Svg(XmlDocument document)
        {
            Parse(document.ChildElements().Single());
        }

        /// <summary>
        /// Constructs an SVG file representation from a stream
        /// </summary>
        /// <param name="inStream">The stream from which the XML file will be pulled</param>
        public Svg(Stream inStream)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.Load(inStream);
            Parse(document.ChildElements().Single());
        }

        /// <summary>
        /// Constructs an SVG file representation from a file
        /// </summary>
        /// <param name="filename">The filename from which the XML file will be pulled</param>
        public Svg(string filename)
        {
            var document = new XmlDocument { XmlResolver = null };
            document.Load(filename);
            Parse(document.ChildElements().Single());
        }

        // Actual parsing is done here
        private void Parse(XmlNode node)
        {
            // First, check if it iself is on the SVG namespace and if it is "svg"
            if (node.NamespaceURI != Namespace)
                throw new InvalidDataException("The SVG root element must be in the SVG namespace!");
            if (node.LocalName != "svg")
                throw new InvalidDataException("The SVG root element must be svg!");

            // Now, parse the root element
            Root = new SvgSizedGroup(node, null, this);

            // Clear the "XML nodes by ID"
            XmlNodesById.Clear();
        }
    }
}
