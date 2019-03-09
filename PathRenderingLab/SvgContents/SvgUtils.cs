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
            => (node.Attributes[attr, Svg.XmlNamespace] ?? node.Attributes[attr, ""])?.Value ?? "";
    }
}
