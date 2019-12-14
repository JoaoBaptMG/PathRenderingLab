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

        Dictionary<string, IPaintServer> paintServerCache;
        Dictionary<string, bool> userSpaceOnUseCache;
        Dictionary<string, DoubleMatrix> transformCache;

        public PaintServerCreator()
        {
            paintServerCache = new Dictionary<string, IPaintServer>();
            userSpaceOnUseCache = new Dictionary<string, bool>();
            transformCache = new Dictionary<string, DoubleMatrix>();
        }

        public IPaintServer GetFromSvg(SvgPaintServer server, SvgElement element, out bool userSpaceOnUse, out DoubleMatrix transform)
        {
            transform = DoubleMatrix.Identity;
            server = SvgDeferredPaintServer.TryGet<SvgPaintServer>(server, element);
            userSpaceOnUse = false;

            // Go through each paint server type
            if (server is SvgColourServer clr)
            {
                // A color paint server that has alpha = 0 is equal to no paint server
                if (clr.Colour.A == 0) return new NoPaintServer();
                return new SolidPaintServer(Convert(clr.Colour));
            }
            // For all those, see if they are already cached
            else if (paintServerCache.ContainsKey(server.ID))
            {
                userSpaceOnUse = userSpaceOnUseCache[server.ID];
                transform = transformCache[server.ID];
                return paintServerCache[server.ID];
            }
            // Linear gradient
            else if (server is SvgGradientServer grad)
            {
                GradientPaintServer paintServer;

                if (server is SvgLinearGradientServer lgrad)
                    paintServer = new LinearGradientPaintServer(lgrad.X1, lgrad.Y1, lgrad.X2, lgrad.Y2,
                        SpreadMethodFrom(grad.SpreadMethod));
                else if (server is SvgRadialGradientServer rgrad)
                    paintServer = new RadialGradientPaintServer(rgrad.CenterX, rgrad.CenterY, rgrad.Radius,
                        rgrad.FocalX, rgrad.FocalY, rgrad.FocalRadius, SpreadMethodFrom(rgrad.SpreadMethod));
                else throw new Exception("Gradient server that is neither linear or radial???");

                foreach (var stop in grad.Stops)
                    paintServer.AddStop(stop.Offset, Convert(stop.GetColor(element)));
                paintServerCache.Add(grad.ID, (IPaintServer)paintServer);
                userSpaceOnUse = userSpaceOnUseCache[server.ID] = grad.GradientUnits == SvgCoordinateUnits.UserSpaceOnUse;
                transform = transformCache[server.ID] = grad.GradientTransform?.GetMatrix().ToDoubleMatrix() ?? DoubleMatrix.Identity;
                return (IPaintServer)paintServer;
            }

            return new NoPaintServer();

            GradientSpreadMethod SpreadMethodFrom(SvgGradientSpreadMethod method)
            {
                switch (method)
                {
                    case SvgGradientSpreadMethod.Reflect: return GradientSpreadMethod.Reflect;
                    case SvgGradientSpreadMethod.Repeat: return GradientSpreadMethod.Repeat;
                    default: return GradientSpreadMethod.Pad;
                }

            }
        }
    }
}
