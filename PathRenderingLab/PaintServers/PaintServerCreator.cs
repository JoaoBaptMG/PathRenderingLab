using Svg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathRenderingLab.PaintServers
{
    /// <summary>
    /// Converts a SvgPaintServer to a IPaintServer
    /// </summary>
    public class PaintServerCreator
    {
        public static Microsoft.Xna.Framework.Color Convert(System.Drawing.Color c)
            => new Microsoft.Xna.Framework.Color(c.R, c.G, c.B, c.A);

        public IPaintServer GetFromSvg(SvgPaintServer server, out bool userSpaceOnUse)
        {
            userSpaceOnUse = false;

            // Go through each paint server type
            if (server is SvgColourServer clr)
            {
                // A color paint server that has alpha = 0 is equal to no paint server
                if (clr.Colour.A == 0) return new NoPaintServer();
                return new SolidPaintServer(Convert(clr.Colour));
            }

            return new NoPaintServer();
        }
    }
}
