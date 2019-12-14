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
    public class RadialGradientPaintServer : GradientPaintServer, IPaintServer
    {
        readonly Vector2 focus, cf;
        readonly float a, rfr, fr2;

        public RadialGradientPaintServer(float cx, float cy, float r, float fx, float fy, float fr, GradientSpreadMethod spreadMethod)
            : base(spreadMethod)
        {
            // Input the constants
            focus = new Vector2(fx, fy);
            cf = new Vector2(cx, cy) - focus;
            r -= fr;
            a = cf.LengthSquared() - r * r;
            rfr = r * fr;
            fr2 = fr * fr;
        }

        public string EffectName => "CurveEffectsRadial";

        public new void SetEffectParameters(Effect effect)
        {
            base.SetEffectParameters(effect);

            effect.Parameters["Focus"].SetValue(focus);
            effect.Parameters["CF"].SetValue(cf);
            effect.Parameters["a"].SetValue(a);
            effect.Parameters["rfr"].SetValue(rfr);
            effect.Parameters["fr2"].SetValue(fr2);
        }

        public new void PrepareOutsideResources(ContentManager content)
        {
            base.PrepareOutsideResources(content);
        }
    }
}
