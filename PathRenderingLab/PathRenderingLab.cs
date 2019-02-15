using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;
using System.Text;

namespace PathRenderingLab
{
    public static class HelpText
    {
        public const string HelpTextKeyboard = "On a keyboard: arrow keys to move, Q to zoom in, A to zoom out, " +
            "Z to toggle wireframe, X to toggle fill, C to toggle stroke, space to speed.";
        public const string HelpTextGamepad = "On a gamepad: left thumbstick to move, RT to zoom in, LT to zoom out, " +
            "Y to toggle wireframe, B to toggle fill, X to toggle stroke, A to speed.";
    }

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class PathRenderingLab : Game
    {
        GraphicsDeviceManager graphics;

        public Color BackgroundColor;
        public Color FillColor;
        public Color StrokeColor;

        public Vector2[] FillVertices;
        public int[] FillIndices;
        public VertexPositionCurve[] FillCurveVertices;
        public VertexPositionDoubleCurve[] FillDoubleCurveVertices;

        public Vector2[] StrokeVertices;
        public int[] StrokeIndices;
        public VertexPositionCurve[] StrokeCurveVertices;
        public VertexPositionDoubleCurve[] StrokeDoubleCurveVertices;

        public float StrokeHalfWidth;

        public bool InvertY, ShowFill, ShowStroke;

        public Matrix Projection;
        public float LogZoom;
        public Vector2 Position;
        public float Amount;

        public string PathString;

        private SuperSampler superSampler;

        Effect effect;

        VertexBuffer triangleVertexBuffer;
        VertexBuffer curveVertexBuffer;
        VertexBuffer doubleCurveVertexBuffer;
        IndexBuffer indexBuffer;
        SpriteBatch spriteBatch;
        SpriteFont font;

        bool WireFrame;
        bool lastY, lastX, lastB;

        public PathRenderingLab()
        {
            var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            graphics = new GraphicsDeviceManager(this)
            {
                PreferredBackBufferWidth = displayMode.Width,
                PreferredBackBufferHeight = displayMode.Height,
                PreferredBackBufferFormat = displayMode.Format,
                HardwareModeSwitch = false,
                IsFullScreen = true,
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferMultiSampling = true
            };

            Position = Vector2.Zero;
            LogZoom = 0;

            Content.RootDirectory = "Content";

            WireFrame = false;
            lastY = false;
            lastX = false;
            lastB = false;

            ShowFill = true;
            ShowStroke = true;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create the vertex buffer
            var vertices = FillVertices.Concat(StrokeVertices).Select(v => new VertexPosition(new Vector3(v, 0))).ToArray();
            if (vertices.Length > 0)
            {
                triangleVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPosition.VertexDeclaration,
                    vertices.Length, BufferUsage.WriteOnly);
                triangleVertexBuffer.SetData(vertices);
            }

            var curveVertices = FillCurveVertices.Concat(StrokeCurveVertices).ToArray();
            if (curveVertices.Length > 0)
            {
                curveVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionCurve.VertexDeclaration,
                    curveVertices.Length, BufferUsage.WriteOnly);
                curveVertexBuffer.SetData(curveVertices);
            }

            var doubleCurveVertices = FillDoubleCurveVertices.Concat(StrokeDoubleCurveVertices).ToArray();
            if (doubleCurveVertices.Length > 0)
            {
                doubleCurveVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionDoubleCurve.VertexDeclaration,
                    doubleCurveVertices.Length, BufferUsage.WriteOnly);
                doubleCurveVertexBuffer.SetData(doubleCurveVertices);
            }

            // Create the index buffer
            var indices = FillIndices.Concat(StrokeIndices).ToArray();
            if (indices.Length > 0)
            {
                indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits, indices.Length, BufferUsage.WriteOnly);
                indexBuffer.SetData(indices);
            }

            // Create the basic effect
            effect = Content.Load<Effect>("CurveEffects");

            // Compute the projection matrix
            var bbox = DeriveBoundingBox();
            Amount = Math.Min(bbox.Width, bbox.Height) / 4;
            Projection = Matrix.CreateOrthographic(bbox.Width, bbox.Height, -1, 100);
            if (InvertY) Projection *= Matrix.CreateScale(new Vector3(1, -1, 1));
            Position = new Vector2(bbox.X + bbox.Width / 2, bbox.Y + bbox.Height / 2);

            effect.Parameters["ScreenSize"].SetValue(new Vector2(
                GraphicsDevice.PresentationParameters.BackBufferWidth,
                GraphicsDevice.PresentationParameters.BackBufferHeight));

            Console.WriteLine(GraphicsDevice.Adapter.Description);
            //superSampler = new SuperSampler(this, 4, 4);

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("mplus");
        }

        private FloatRectangle DeriveBoundingBox()
        {
            // Find the curve's bounding box
            float minx = float.PositiveInfinity;
            float maxx = float.NegativeInfinity;
            float miny = float.PositiveInfinity;
            float maxy = float.NegativeInfinity;

            foreach (var v in FillVertices.Concat(StrokeVertices))
            {
                minx = Math.Min(minx, v.X);
                maxx = Math.Max(maxx, v.X);
                miny = Math.Min(miny, v.Y);
                maxy = Math.Max(maxy, v.Y);
            }

            foreach (var v in FillCurveVertices.Concat(StrokeCurveVertices))
            {
                minx = Math.Min(minx, v.Position.X);
                maxx = Math.Max(maxx, v.Position.X);
                miny = Math.Min(miny, v.Position.Y);
                maxy = Math.Max(maxy, v.Position.Y);
            }

            foreach (var v in FillDoubleCurveVertices.Concat(StrokeDoubleCurveVertices))
            {
                minx = Math.Min(minx, v.Position.X);
                maxx = Math.Max(maxx, v.Position.X);
                miny = Math.Min(miny, v.Position.Y);
                maxy = Math.Max(maxy, v.Position.Y);
            }

            var rectangle = new FloatRectangle(minx, miny, maxx - minx, maxy - miny);

            // Adjust it to the aspect ratio
            var value = rectangle.Width * 9 - rectangle.Height * 16;
            if (value < 0)
            {
                var oldWidth = rectangle.Width;
                rectangle.Width = rectangle.Height * 16 / 9;
                rectangle.X -= (rectangle.Width - oldWidth) / 2;
            }
            else if (value > 0)
            {
                var oldHeight = rectangle.Height;
                rectangle.Height = rectangle.Width * 9 / 16;
                rectangle.Y -= (rectangle.Height - oldHeight) / 2;
            }

            // Enlarge it a bit
            var owidth = rectangle.Width;
            var oheight = rectangle.Height;
            rectangle.Width = rectangle.Width * 6 / 5;
            rectangle.Height = rectangle.Height * 6 / 5;
            rectangle.X -= (rectangle.Width - owidth) / 2;
            rectangle.Y -= (rectangle.Height - oheight) / 2;

            return rectangle;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        private bool EventJustOccurred(ref bool lastEvent, bool curEvent)
        {
            var result = !lastEvent && curEvent;
            lastEvent = curEvent;
            return result;
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            var gamepadState = GamePad.GetState(PlayerIndex.One);
            var keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape) || gamepadState.IsButtonDown(Buttons.Start))
                Exit();

            if (EventJustOccurred(ref lastY, gamepadState.IsButtonDown(Buttons.Y) || keyboardState.IsKeyDown(Keys.Z)))
                WireFrame = !WireFrame;

            if (EventJustOccurred(ref lastB, gamepadState.IsButtonDown(Buttons.B) || keyboardState.IsKeyDown(Keys.X)))
                ShowFill = !ShowFill;

            if (EventJustOccurred(ref lastX, gamepadState.IsButtonDown(Buttons.X) || keyboardState.IsKeyDown(Keys.C)))
                ShowStroke = !ShowStroke;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var logZoomAmountGamepad = gamepadState.Triggers.Right - gamepadState.Triggers.Left;
            var logZoomAmountKeyboard = (keyboardState.IsKeyDown(Keys.Q) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.A) ? 1 : 0);
            var logZoomAmount = logZoomAmountGamepad + logZoomAmountKeyboard;

            var oldLogZoom = LogZoom;
            LogZoom += 0.8f * logZoomAmount * dt;
            Position = Position * (float)Math.Exp(LogZoom - oldLogZoom);

            var displacementGamepad = gamepadState.ThumbSticks.Left;
            var displacementKeyboard = new Vector2(
                (keyboardState.IsKeyDown(Keys.Right) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.Left) ? 1 : 0),
                (keyboardState.IsKeyDown(Keys.Up) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.Down) ? 1 : 0));

            if (gamepadState.IsButtonDown(Buttons.A)) displacementGamepad *= 4f;
            if (keyboardState.IsKeyDown(Keys.Space)) displacementKeyboard *= 4f;

            var displacement = displacementGamepad + displacementKeyboard;
            if (InvertY) displacement.Y = -displacement.Y;

            Position += displacement * Amount * dt;

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            //superSampler.SetSupersamplingTarget();
            GraphicsDevice.Clear(BackgroundColor);

            var modelView = Matrix.CreateScale((float)Math.Exp(LogZoom)) * Matrix.CreateTranslation(new Vector3(-Position, 0));
            var modelViewProjection = modelView * Projection;


            effect.Parameters["WorldViewProjection"].SetValue(modelViewProjection);

            // Draw the triangles
            GraphicsDevice.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                FillMode = WireFrame ? FillMode.WireFrame : FillMode.Solid
            };

            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            if (ShowFill)
            {
                effect.Parameters["Color"].SetValue(FillColor.ToVector4());
                effect.CurrentTechnique = effect.Techniques["BasicColorDrawing"];

                // Fill triangles
                if (FillIndices.Length > 0)
                {
                    GraphicsDevice.SetVertexBuffer(triangleVertexBuffer);
                    GraphicsDevice.Indices = indexBuffer;

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, FillIndices.Length / 3);
                    }
                }

                // Fill curve triangles
                if (FillCurveVertices.Length > 0)
                {
                    if (!WireFrame) effect.CurrentTechnique = effect.Techniques["CurveDrawing"];
                    GraphicsDevice.SetVertexBuffer(curveVertexBuffer);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, FillCurveVertices.Length / 3);
                    }
                }

                // Fill double curve triangles
                if (FillDoubleCurveVertices.Length > 0)
                {
                    if (!WireFrame) effect.CurrentTechnique = effect.Techniques["DoubleCurveDrawing"];
                    GraphicsDevice.SetVertexBuffer(doubleCurveVertexBuffer);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, FillDoubleCurveVertices.Length / 3);
                    }
                }
            }

            if (ShowStroke)
            {
                effect.Parameters["Color"].SetValue(StrokeColor.ToVector4());
                effect.CurrentTechnique = effect.Techniques["BasicColorDrawing"];

                // Stroke triangles
                if (StrokeIndices.Length > 0)
                {
                    GraphicsDevice.SetVertexBuffer(triangleVertexBuffer);
                    GraphicsDevice.Indices = indexBuffer;

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, FillVertices.Length, 
                            FillIndices.Length, StrokeIndices.Length / 3);
                    }
                }

                // Stroke curve triangles
                if (StrokeCurveVertices.Length > 0)
                {
                    if (!WireFrame) effect.CurrentTechnique = effect.Techniques["CurveDrawing"];
                    GraphicsDevice.SetVertexBuffer(curveVertexBuffer);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, FillCurveVertices.Length,
                            StrokeCurveVertices.Length / 3);
                    }
                }

                // Stroke double curve triangles
                if (StrokeDoubleCurveVertices.Length > 0)
                {
                    if (!WireFrame) effect.CurrentTechnique = effect.Techniques["DoubleCurveDrawing"];
                    GraphicsDevice.SetVertexBuffer(doubleCurveVertexBuffer);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, FillDoubleCurveVertices.Length,
                            StrokeDoubleCurveVertices.Length / 3);
                    }
                }
            }

            //superSampler.DrawSupersampledTarget();

            var text1 = WrapText(font, CommandText(), GraphicsDevice.PresentationParameters.BackBufferWidth);
            var text2 = WrapText(font, PathString, GraphicsDevice.PresentationParameters.BackBufferWidth);

            var pos = new Vector2(0, GraphicsDevice.PresentationParameters.BackBufferHeight - font.MeasureString(text2).Y);

            var textColor = StrokeColor.A > 0 ? StrokeColor : Color.Black;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.Default, RasterizerState.CullNone);
            spriteBatch.DrawString(font, text1, Vector2.Zero, textColor);
            spriteBatch.DrawString(font, text2, pos, textColor);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        public string CommandText()
        {
            var sb = new StringBuilder();
            sb.AppendLine(HelpText.HelpTextKeyboard);
            sb.AppendLine(HelpText.HelpTextGamepad);

            void AppendDrawing(string name, int numIndices, int numCurveVertices, int numDoubleCurveVertices)
            {
                if (numIndices + numCurveVertices + numDoubleCurveVertices > 0)
                    sb.AppendLine($"{(numIndices + numCurveVertices + numDoubleCurveVertices) / 3} {name} triangles " +
                        $"({numIndices / 3} filled, {numCurveVertices / 3} curves and {numDoubleCurveVertices / 3} double curves)");
            }

            AppendDrawing("fill", FillIndices.Length, FillCurveVertices.Length, FillDoubleCurveVertices.Length);
            AppendDrawing("stroke", StrokeIndices.Length, StrokeCurveVertices.Length, StrokeDoubleCurveVertices.Length);

            return sb.ToString();
        }

        public string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
        {
            var strs = text.Split('\n').Select(s => WrapTextLine(spriteFont, s, maxLineWidth));
            return string.Join("\n", strs);
        }

        // Taken from https://stackoverflow.com/a/15987581
        public string WrapTextLine(SpriteFont spriteFont, string text, float maxLineWidth)
        {
            string[] words = text.Split(' ');
            StringBuilder sb = new StringBuilder();
            float lineWidth = 0f;
            float spaceWidth = spriteFont.MeasureString(" ").X;

            foreach (string word in words)
            {
                Vector2 size = spriteFont.MeasureString(word);

                if (lineWidth + size.X < maxLineWidth)
                {
                    sb.Append(word + " ");
                    lineWidth += size.X + spaceWidth;
                }
                else
                {
                    sb.Append("\n" + word + " ");
                    lineWidth = size.X + spaceWidth;
                }
            }

            return sb.ToString();
        }
    }
}
