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
        /// Constructs an SVG file representation from an XML node
        /// </summary>
        /// <param name="node">The node to be parsed for SVG</param>
        public Svg(XmlNode node)
        {
            Parse(node);
        }

        /// <summary>
        /// Constructs an SVG file representation from a stream
        /// </summary>
        /// <param name="inStream">The stream from which the XML file will be pulled</param>
        public Svg(Stream inStream)
        {
            var document = new XmlDocument();
            document.Load(inStream);
            Parse(document);
        }

        /// <summary>
        /// Constructs an SVG file representation from a file
        /// </summary>
        /// <param name="filename">The filename from which the XML file will be pulled</param>
        public Svg(string filename)
        {
            var document = new XmlDocument();
            document.Load(filename);
            Parse(document);
        }

        // Actual parsing is done here
        private void Parse(XmlNode node)
        {

        }
    }
}
