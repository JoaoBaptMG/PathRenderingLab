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
        /// The color used to fill the path (or none)
        /// </summary>
        public Color? FillColor;

        /// <summary>
        /// The fill rule used (even-odd or nonzero)
        /// </summary>
        public FillRule FillRule;

        /// <summary>
        /// The color used to stroke the path (or none)
        /// </summary>
        public Color? StrokeColor;

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

        public PathStyle(Dictionary<string, string> properties, PathStyle? parent)
        {
            // Fill in the default values according to the default values on https://svgwg.org/svg-next/painting.htm
            // Special handling for paint servers (which now only accept colors)
            FillColor = CSSColor.Parse(properties.GetOrDefault("fill")) ?? (parent.HasValue ? parent.Value.FillColor : Color.Black);
            StrokeColor = CSSColor.Parse(properties.GetOrDefault("stroke")) ?? parent?.StrokeColor;

            FillRule = CSSEnumPicker<FillRule>.Get(properties.GetOrDefault("fill-rule")) ?? parent?.FillRule ?? FillRule.Nonzero;
            StrokeWidth = DoubleUtils.TryParse(properties.GetOrDefault("stroke-width")) ?? parent?.StrokeWidth ?? 1;
            StrokeLineCap = CSSEnumPicker<StrokeLineCap>.Get(properties.GetOrDefault("stroke-linecap")) ??
                parent?.StrokeLineCap ?? StrokeLineCap.Butt;
            StrokeLineJoin = CSSEnumPicker<StrokeLineJoin>.Get(properties.GetOrDefault("stroke-linejoin")) ??
                parent?.StrokeLineJoin ?? StrokeLineJoin.Miter;
            MiterLimit = DoubleUtils.TryParse(properties.GetOrDefault("stroke-miterlimit")) ?? parent?.MiterLimit ?? 4;
        }
    }
}
