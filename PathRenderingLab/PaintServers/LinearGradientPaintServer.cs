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
    public class LinearGradientPaintServer : GradientPaintServer, IPaintServer
    {
        Vector2 origin, direction;

        public LinearGradientPaintServer(float x1, float y1, float x2, float y2, GradientSpreadMethod spreadMethod)
            : base(spreadMethod)
        {
            origin = new Vector2(x1, y1);
            direction = new Vector2(x2, y2) - origin;
        }

        public string EffectName => "CurveEffectsLinear";

        public new void SetEffectParameters(Effect effect)
        {
            base.SetEffectParameters(effect);

            effect.Parameters["Origin"].SetValue(origin);
            effect.Parameters["Direction"].SetValue(direction);
        }

        public new void PrepareOutsideResources(ContentManager content)
        {
            base.PrepareOutsideResources(content);
        }
    }
}
