using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

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

        // The most general constructor, since every node can have ids, transforms and path styles
        protected SvgNode(XmlNode node, SvgNode parent)
        {

        }
    }
}
