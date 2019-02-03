using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace PathRenderingLab
{
    /// <summary>
    /// This class is responsible for managing the super-sampling render target and them
    /// rendering copies of it in order to leverage the alpha-blending mode
    /// </summary>
    /// 
    public sealed class SuperSampler : IDisposable
    {
        GraphicsDevice GraphicsDevice;
        Effect supersamplingEffect;
        RenderTarget2D supersampledRenderTarget;
        RenderTarget2D highAccumulationRenderTarget;
        VertexBuffer quadsBuffer;
        readonly float weight;
        int vertexCount;

        public SuperSampler(Game game, int sx, int sy)
        {
            // Compute the alpha weight of the textures
            weight = 1f / (sx * sy);

            // Load the effect
            supersamplingEffect = game.Content.Load<Effect>("Supersampling");

            GraphicsDevice = game.GraphicsDevice;

            // Create the resources
            CreateResources(sx, sy);
        }

        private void CreateResources(int sx, int sy)
        {
            // Check if the resources "already" existed
            if (supersampledRenderTarget != null) supersampledRenderTarget.Dispose();
            if (highAccumulationRenderTarget != null) highAccumulationRenderTarget.Dispose();
            if (quadsBuffer != null) quadsBuffer.Dispose();

            // Create the render target
            int width = GraphicsDevice.PresentationParameters.BackBufferWidth;
            int height = GraphicsDevice.PresentationParameters.BackBufferHeight;
            supersampledRenderTarget = new RenderTarget2D(GraphicsDevice, width * sx, height * sy);
            highAccumulationRenderTarget = new RenderTarget2D(GraphicsDevice, width, height,
                false, SurfaceFormat.Vector4, DepthFormat.None);

            // Create the vertices
            var vertices = new VertexPositionTexture[6 * sx * sy];

            // Populate the vertices
            vertexCount = 0;
            for (int j = 0; j < sy; j++)
                for (int i = 0; i < sx; i++)
                {
                    float dx = 2f * i / (width * sx);
                    float dy = 2f * j / (height * sy);

                    var v0 = new VertexPositionTexture(new Vector3(-1 - dx, -1 - dy, 0f), new Vector2(0f, 1f));
                    var v1 = new VertexPositionTexture(new Vector3(+1 - dx, -1 - dy, 0f), new Vector2(1f, 1f));
                    var v2 = new VertexPositionTexture(new Vector3(+1 - dx, +1 - dy, 0f), new Vector2(1f, 0f));
                    var v3 = new VertexPositionTexture(new Vector3(-1 - dx, +1 - dy, 0f), new Vector2(0f, 0f));

                    vertices[vertexCount++] = v0;
                    vertices[vertexCount++] = v1;
                    vertices[vertexCount++] = v2;
                    vertices[vertexCount++] = v0;
                    vertices[vertexCount++] = v2;
                    vertices[vertexCount++] = v3;
                }

            // Now, create the buffer
            quadsBuffer = new VertexBuffer(GraphicsDevice, VertexPositionTexture.VertexDeclaration, vertexCount, BufferUsage.None);
            quadsBuffer.SetData(vertices);
        }

        public void SetSupersamplingTarget() => GraphicsDevice.SetRenderTarget(supersampledRenderTarget);

        public static readonly BlendState SuperSampleBlendState = new BlendState()
        {
            ColorSourceBlend = Blend.One,
            ColorDestinationBlend = Blend.One,
            ColorBlendFunction = BlendFunction.Add,

            AlphaSourceBlend = Blend.One,
            AlphaDestinationBlend = Blend.One,
            AlphaBlendFunction = BlendFunction.Add,
        };

        public void DrawSupersampledTarget(RenderTarget2D newTarget = null)
        {
            // Set the high-resolution accumulation target
            GraphicsDevice.SetRenderTarget(highAccumulationRenderTarget);
            GraphicsDevice.Clear(Color.Black);

            // Set the state
            GraphicsDevice.SetVertexBuffer(quadsBuffer);
            GraphicsDevice.BlendState = SuperSampleBlendState;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            // Draw the effect
            supersamplingEffect.Parameters["Texture"].SetValue(supersampledRenderTarget);
            supersamplingEffect.Parameters["Weight"].SetValue(weight);
            supersamplingEffect.CurrentTechnique = supersamplingEffect.Techniques["Supersampling"];

            // Draw the list
            foreach (var pass in supersamplingEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexCount);
            }

            // Set the original target
            GraphicsDevice.SetRenderTarget(newTarget);

            // Set the state
            GraphicsDevice.SetVertexBuffer(quadsBuffer);
            GraphicsDevice.BlendState = BlendState.Opaque;

            // Draw the effect
            supersamplingEffect.Parameters["Texture"].SetValue(highAccumulationRenderTarget);
            supersamplingEffect.Parameters["Weight"].SetValue(1f);
            supersamplingEffect.CurrentTechnique = supersamplingEffect.Techniques["Supersampling"];

            // Draw the list
            foreach (var pass in supersamplingEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, 6);
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public void Dispose()
        {
            if (!disposedValue)
            {
                supersampledRenderTarget.Dispose();
                supersampledRenderTarget = null;

                quadsBuffer.Dispose();
                quadsBuffer = null;

                highAccumulationRenderTarget.Dispose();
                highAccumulationRenderTarget = null;

                disposedValue = true;
            }
        }
        #endregion


    }
}
