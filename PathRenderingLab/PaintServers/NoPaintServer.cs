using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PathRenderingLab.PaintServers
{
    internal class NoPaintServer : IPaintServer
    {
        public string EffectName => null;

        public void SetEffectParameters(Effect effect) { }
    }
}
