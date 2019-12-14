using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathRenderingLab.PaintServers
{
    internal class SolidPaintServer : IPaintServer
    {
        readonly Color color;

        public SolidPaintServer(Color c)
        {
            color = c;
        }

        public string EffectName => "CurveEffectsSolid";

        public void SetEffectParameters(Effect effect)
        {
            effect.Parameters["Color"].SetValue(color.ToVector4());
        }

        public void PrepareOutsideResources(ContentManager content) { }
    }
}
