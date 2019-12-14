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
    public enum GradientSpreadMethod { Pad, Reflect, Repeat }

    public class GradientPaintServer
    {
        protected readonly GradientSpreadMethod SpreadMethod;
        protected readonly SortedDictionary<float, Color> Stops;
        protected Texture2D ColorTexture { get; private set; }

        protected GradientPaintServer(GradientSpreadMethod method)
        {
            Stops = new SortedDictionary<float, Color>();
            SpreadMethod = method;
        }

        public void AddStop(float offset, Color color) => Stops.Add(offset / 100, color);

        protected void PrepareOutsideResources(ContentManager content)
        {
            if (ColorTexture != null) return;

            GraphicsDevice graphicsDevice = content.GetGraphicsDevice();
            var renderTarget = new RenderTarget2D(graphicsDevice, 1024, 1, true, SurfaceFormat.Color, DepthFormat.None);

            // Generate the line strip
            graphicsDevice.SetRenderTarget(renderTarget);
            var vertices = Stops.Select(stop => new VertexPositionColor(new Vector3(stop.Key * 2 - 1, 0, 0), stop.Value)).ToArray();

            // Using the direct draw effect
            var effect = content.Load<Effect>("DirectEffect");
            effect.CurrentTechnique = effect.Techniques["DirectDrawing"];

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply();
                graphicsDevice.DrawUserPrimitives(PrimitiveType.LineStrip, vertices, 0, vertices.Length - 1);
            }
            graphicsDevice.SetRenderTarget(null);

            ColorTexture = renderTarget;
        }

        public void SetEffectParameters(Effect effect)
        {
            TextureAddressMode mode = TextureAddressMode.Clamp;
            switch (SpreadMethod)
            {
                case GradientSpreadMethod.Reflect: mode = TextureAddressMode.Mirror; break;
                case GradientSpreadMethod.Repeat: mode = TextureAddressMode.Wrap; break;
            }

            effect.Parameters["ColorRamp"].SetValue(ColorTexture);
            effect.GraphicsDevice.SamplerStates[0] = new SamplerState
            {
                AddressU = mode,
                Filter = TextureFilter.Linear,
                FilterMode = TextureFilterMode.Default
            };
        }
    }
}
