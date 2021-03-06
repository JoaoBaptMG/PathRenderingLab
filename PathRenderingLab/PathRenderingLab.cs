﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PathRenderingLab.PaintServers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PathRenderingLab
{
    public static class HelpText
    {
        public const string HelpTextKeyboard = "On a keyboard: arrow keys to move, Q to zoom in, A to zoom out, " +
            "Z to toggle wireframe, R/F to choose a drawing, X to toggle the selected drawing, space to speed.";
        public const string HelpTextGamepad = "On a gamepad: left thumbstick to move, RT to zoom in, LT to zoom out, " +
            "Y to toggle wireframe, d-pad up/down to choose a drawing, B to toggle the selected drawing, A to speed.";
    }

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class PathRenderingLab : Game
    {
        public Color BackgroundColor;
        public IPaintServer[] DrawingPaintServers;

        public Vector2[] AllDrawingVertices;
        public int NumDrawings;

        public Matrix[] DrawingTransforms;
        public Matrix[] DrawingColorTransforms;
        public int[] DrawingIndices;
        public VertexPositionCurve[] DrawingCurveVertices;
        public VertexPositionDoubleCurve[] DrawingDoubleCurveVertices;

        public int[] DrawingIndicesStartingIds;
        public int[] DrawingCurveVerticesStartingIds;
        public int[] DrawingDoubleCurveVerticesStartingIds;

        public Matrix Projection;
        public float LogZoom;
        public Vector2 Position;
        public float Amount;

        Dictionary<string, Effect> effects;

        VertexBuffer triangleVertexBuffer;
        VertexBuffer curveVertexBuffer;
        VertexBuffer doubleCurveVertexBuffer;
        IndexBuffer indexBuffer;
        SpriteBatch spriteBatch;
        SpriteFont font;

        bool wireFrame;
        bool lastY, lastUp, lastDown, lastB;
        bool[] hiddenDrawings;
        int curDrawingSelected;

        public PathRenderingLab()
        {
            var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;

            new GraphicsDeviceManager(this)
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

            wireFrame = false;
            lastY = false;
            lastUp = false;
            lastDown = false;
            lastB = false;
            curDrawingSelected = 0;
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
            var vertices = AllDrawingVertices.Select(v => new VertexPosition(new Vector3(v, 0))).ToArray();
            if (AllDrawingVertices.Length > 0)
            {
                triangleVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPosition.VertexDeclaration,
                    AllDrawingVertices.Length, BufferUsage.WriteOnly);
                triangleVertexBuffer.SetData(vertices);
            }

            if (DrawingCurveVertices.Length > 0)
            {
                curveVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionCurve.VertexDeclaration,
                    DrawingCurveVertices.Length, BufferUsage.WriteOnly);
                curveVertexBuffer.SetData(DrawingCurveVertices);
            }

            if (DrawingDoubleCurveVertices.Length > 0)
            {
                doubleCurveVertexBuffer = new VertexBuffer(GraphicsDevice, VertexPositionDoubleCurve.VertexDeclaration,
                    DrawingDoubleCurveVertices.Length, BufferUsage.WriteOnly);
                doubleCurveVertexBuffer.SetData(DrawingDoubleCurveVertices);
            }

            // Create the index buffer
            if (DrawingIndices.Length > 0)
            {
                indexBuffer = new IndexBuffer(GraphicsDevice, IndexElementSize.ThirtyTwoBits,
                    DrawingIndices.Length, BufferUsage.WriteOnly);
                indexBuffer.SetData(DrawingIndices);
            }

            // Create the effects and set up the parameters
            effects = new Dictionary<string, Effect>();
            foreach (var ps in DrawingPaintServers)
            {
                ps.PrepareOutsideResources(Content);

                if (!effects.ContainsKey(ps.EffectName))
                    effects.Add(ps.EffectName, Content.Load<Effect>(ps.EffectName));
            }

            // Compute the projection matrix
            var bbox = DeriveBoundingBox();
            Amount = Math.Min(bbox.Width, bbox.Height) / 4;
            Projection = Matrix.CreateOrthographic(bbox.Width, bbox.Height, -1, 100);
            Projection *= Matrix.CreateScale(new Vector3(1, -1, 1));
            Position = new Vector2(bbox.X + bbox.Width / 2, bbox.Y + bbox.Height / 2);

            Console.WriteLine(GraphicsDevice.Adapter.Description);
            //superSampler = new SuperSampler(this, 4, 4);

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("mplus");

            hiddenDrawings = new bool[NumDrawings];
        }

        private FloatRectangle DeriveBoundingBox()
        {
            // Find the curve's bounding box
            float minx = float.PositiveInfinity;
            float maxx = float.NegativeInfinity;
            float miny = float.PositiveInfinity;
            float maxy = float.NegativeInfinity;

            for (int i = 0; i < NumDrawings; i++)
            {
                for (int j = DrawingIndicesStartingIds[i]; j < DrawingIndicesStartingIds[i + 1]; j++)
                {
                    var v = Vector4.Transform(AllDrawingVertices[DrawingIndices[j]], DrawingTransforms[i]);

                    minx = Math.Min(minx, v.X);
                    maxx = Math.Max(maxx, v.X);
                    miny = Math.Min(miny, v.Y);
                    maxy = Math.Max(maxy, v.Y);
                }

                for (int j = DrawingCurveVerticesStartingIds[i]; j < DrawingCurveVerticesStartingIds[i + 1]; j++)
                {
                    var v = Vector4.Transform(DrawingCurveVertices[j].Position, DrawingTransforms[i]);

                    minx = Math.Min(minx, v.X);
                    maxx = Math.Max(maxx, v.X);
                    miny = Math.Min(miny, v.Y);
                    maxy = Math.Max(maxy, v.Y);
                }

                for (int j = DrawingDoubleCurveVerticesStartingIds[i]; j < DrawingDoubleCurveVerticesStartingIds[i + 1]; j++)
                {
                    var v = Vector4.Transform(DrawingDoubleCurveVertices[j].Position, DrawingTransforms[i]);

                    minx = Math.Min(minx, v.X);
                    maxx = Math.Max(maxx, v.X);
                    miny = Math.Min(miny, v.Y);
                    maxy = Math.Max(maxy, v.Y);
                }
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

            bool ButtonOrKeyDown(Buttons button, Keys key) => keyboardState.IsKeyDown(key) || gamepadState.IsButtonDown(button);
            bool ButtonOrKeyHit(ref bool last, Buttons button, Keys key) => EventJustOccurred(ref last, ButtonOrKeyDown(button, key));

            if (ButtonOrKeyDown(Buttons.Start, Keys.Escape)) Exit();
            if (ButtonOrKeyHit(ref lastY, Buttons.Y, Keys.Z)) wireFrame = !wireFrame;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var logZoomAmountGamepad = gamepadState.Triggers.Right - gamepadState.Triggers.Left;
            var logZoomAmountKeyboard = (keyboardState.IsKeyDown(Keys.Q) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.A) ? 1 : 0);
            var logZoomAmount = logZoomAmountGamepad + logZoomAmountKeyboard;

            var oldLogZoom = LogZoom;
            LogZoom += 0.8f * logZoomAmount * dt;
            Position *= (float)Math.Exp(LogZoom - oldLogZoom);

            var displacementGamepad = gamepadState.ThumbSticks.Left;
            var displacementKeyboard = new Vector2(
                (keyboardState.IsKeyDown(Keys.Right) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.Left) ? 1 : 0),
                (keyboardState.IsKeyDown(Keys.Up) ? 1 : 0) - (keyboardState.IsKeyDown(Keys.Down) ? 1 : 0));

            if (keyboardState.IsKeyDown(Keys.Space)) displacementKeyboard *= 4f;
            if (gamepadState.IsButtonDown(Buttons.A)) displacementGamepad *= 4f;

            var displacement = displacementGamepad + displacementKeyboard;
            displacement.Y = -displacement.Y;

            Position += displacement * Amount * dt;

            if (ButtonOrKeyHit(ref lastUp, Buttons.DPadUp, Keys.R))
                curDrawingSelected = (curDrawingSelected + NumDrawings - 1) % NumDrawings;
            else if (ButtonOrKeyHit(ref lastDown, Buttons.DPadDown, Keys.F))
                curDrawingSelected = (curDrawingSelected + 1) % NumDrawings;

            if (ButtonOrKeyHit(ref lastB, Buttons.B, Keys.X))
                hiddenDrawings[curDrawingSelected] = !hiddenDrawings[curDrawingSelected];

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

            var view = Matrix.CreateScale((float)Math.Exp(LogZoom)) * Matrix.CreateTranslation(new Vector3(-Position, 0));

            // Draw the triangles
            GraphicsDevice.RasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                FillMode = wireFrame ? FillMode.WireFrame : FillMode.Solid
            };

            GraphicsDevice.BlendState = BlendState.AlphaBlend;

            for (int i = 0; i < NumDrawings; i++)
            {
                if (hiddenDrawings[i]) continue;

                var effect = effects[DrawingPaintServers[i].EffectName];
                DrawingPaintServers[i].SetEffectParameters(effect);
                effect.Parameters["WorldViewProjection"].SetValue(DrawingTransforms[i] * view * Projection);
                effect.Parameters["ColorTransform"].SetValue(DrawingColorTransforms[i]);

                effect.CurrentTechnique = effect.Techniques["Triangle"];

                // Fill triangles
                if (DrawingIndicesStartingIds[i] < DrawingIndicesStartingIds[i + 1])
                {
                    GraphicsDevice.SetVertexBuffer(triangleVertexBuffer);
                    GraphicsDevice.Indices = indexBuffer;

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, DrawingIndicesStartingIds[i],
                            (DrawingIndicesStartingIds[i + 1] - DrawingIndicesStartingIds[i]) / 3);
                    }
                }

                // Fill curve triangles
                if (DrawingCurveVerticesStartingIds[i] < DrawingCurveVerticesStartingIds[i + 1])
                {
                    if (!wireFrame) effect.CurrentTechnique = effect.Techniques["Curve"];
                    GraphicsDevice.SetVertexBuffer(curveVertexBuffer);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, DrawingCurveVerticesStartingIds[i],
                            (DrawingCurveVerticesStartingIds[i + 1] - DrawingCurveVerticesStartingIds[i]) / 3);
                    }
                }

                // Fill double curve triangles
                if (DrawingDoubleCurveVerticesStartingIds[i] < DrawingDoubleCurveVerticesStartingIds[i + 1])
                {
                    if (!wireFrame) effect.CurrentTechnique = effect.Techniques["DoubleCurve"];
                    GraphicsDevice.SetVertexBuffer(doubleCurveVertexBuffer);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
                        GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, DrawingDoubleCurveVerticesStartingIds[i],
                            (DrawingDoubleCurveVerticesStartingIds[i + 1] - DrawingDoubleCurveVerticesStartingIds[i]) / 3);
                    }
                }
            }

            //superSampler.DrawSupersampledTarget();
            var text1 = WrapText(font, CommandText(), GraphicsDevice.PresentationParameters.BackBufferWidth);
            var text2 = WrapText(font, PathDebugText(), GraphicsDevice.PresentationParameters.BackBufferWidth);

            var pos = new Vector2(0, GraphicsDevice.PresentationParameters.BackBufferHeight - font.MeasureString(text2).Y);

            var textColor = Color.Black;

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.Default, RasterizerState.CullNone);
            spriteBatch.DrawString(font, text1, Vector2.Zero, textColor);
            spriteBatch.DrawString(font, text2, pos, textColor);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        public static string CommandText() => HelpText.HelpTextKeyboard + '\n' + HelpText.HelpTextGamepad;

        public string PathDebugText()
        {
            var sb = new StringBuilder();

            void AppendDrawing(string name, int numIndices, int numCurveVertices, int numDoubleCurveVertices)
                => sb.AppendLine($"{name}: {(numIndices + numCurveVertices + numDoubleCurveVertices) / 3} triangles " +
                    $"({numIndices / 3} filled, {numCurveVertices / 3} curves and {numDoubleCurveVertices / 3} double curves)");

            for (int i = 0; i < NumDrawings; i++)
                AppendDrawing($"{(i == curDrawingSelected ? "@" : "O")} drawing {i+1}{(hiddenDrawings[i] ? " (hidden)" : "")}",
                    DrawingIndicesStartingIds[i + 1] - DrawingIndicesStartingIds[i],
                    DrawingCurveVerticesStartingIds[i + 1] - DrawingCurveVerticesStartingIds[i],
                    DrawingDoubleCurveVerticesStartingIds[i + 1] - DrawingDoubleCurveVerticesStartingIds[i]);

            return sb.ToString().Trim();
        }

        public static string WrapText(SpriteFont spriteFont, string text, float maxLineWidth)
        {
            var strs = text.Split('\n').Select(s => WrapTextLine(spriteFont, s, maxLineWidth));
            return string.Join("\n", strs);
        }

        // Taken from https://stackoverflow.com/a/15987581
        public static string WrapTextLine(SpriteFont spriteFont, string text, float maxLineWidth)
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
