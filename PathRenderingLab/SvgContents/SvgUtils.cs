using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace PathRenderingLab.SvgContents
{
    public static class SvgUtils
    {
        public static string SvgAttribute(this XmlNode node, string attr)
            => (node.Attributes[attr, Svg.Namespace] ?? node.Attributes[attr, ""])?.Value ?? "";

        public static IEnumerable<XmlElement> ChildElements(this XmlNode node)
            => node.ChildNodes.Cast<XmlNode>().Where(child => child.NodeType == XmlNodeType.Element).Cast<XmlElement>();
    }
}
