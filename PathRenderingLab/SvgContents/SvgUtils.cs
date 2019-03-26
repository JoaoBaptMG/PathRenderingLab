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

        public static void InheritFrom(this Dictionary<string, string> properties, Dictionary<string, string> parentProperties)
        {
            var propertiesCopy = new Dictionary<string, string>();

            // First, handle all explicit cases (inherit, initial)
            foreach (var kvp in properties)
            {
                if (kvp.Value == "initial") propertiesCopy[kvp.Key] = "";
                else if (kvp.Value == "inherit") propertiesCopy[kvp.Key] = parentProperties[kvp.Key];
                else propertiesCopy[kvp.Key] = kvp.Value;
            }

            // Now, copy over the parent properties
            foreach (var kvp in parentProperties)
                if (!propertiesCopy.ContainsKey(kvp.Key))
                    propertiesCopy[kvp.Key] = kvp.Value;

            // Finally, copy over
            properties.Clear();
            foreach (var kvp in propertiesCopy)
                properties[kvp.Key] = kvp.Value;
        }
    }
}
