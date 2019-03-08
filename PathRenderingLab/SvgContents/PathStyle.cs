using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;

namespace PathRenderingLab.SvgContents
{
    public struct PathStyle
    {
        /// <summary>
        /// The color used to fill the path
        /// </summary>
        public Color FillColor;

        /// <summary>
        /// The fill rule used (even-odd or nonzero)
        /// </summary>
        public FillRule FillRule;

        /// <summary>
        /// The color used to stroke the path
        /// </summary>
        public Color StrokeColor;

        /// <summary>
        /// The stroke width
        /// </summary>
        public double StrokeWidth;

        /// <summary>
        /// The line cap used at the endpoints of the stroke
        /// </summary>
        public StrokeLineCap StrokeLineCap;

        /// <summary>
        /// The line join used at consecutive curves of the stroke
        /// </summary>
        public StrokeLineJoin StrokeLineJoin;

        /// <summary>
        /// The miter limit for the line join
        /// </summary>
        public double MiterLimit;
    }
}
