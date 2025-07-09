using ChessCommon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;

namespace ChessClient
{
    public class ClientGame : Game
    {

        const float SCROLL_SENSITIVITY = 1.0f / 2400.0f;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Effect msaa;

        Texture2D pieceTexture;
        Vector3 cameraPosition = Vector3.Zero;

        Matrix CamMatrix => Matrix.CreateTranslation(cameraPosition * new Vector3(1, 1, 0)) * Matrix.CreateScale(MathF.Pow(10, cameraPosition.Z)) * Matrix.CreateTranslation(new Vector3(Window.ClientBounds.Size.ToVector2() / 2, 0));
        Matrix CamMatrixInv => Matrix.Invert(CamMatrix);

        public ClientGame()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            Window.AllowUserResizing = true;

            GameStateRenderer.gameState = new ChessCommon.GameState();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            GameStateRenderer.pieceTexture = Content.Load<Texture2D>("pieces");
            GameStateRenderer.sq = Content.Load<Texture2D>("sq");
            GameStateRenderer.arsq = Content.Load<Texture2D>("arrow");
            msaa = Content.Load<Effect>("msaa");
        }

        Vector3 mouse_position_previous = Vector3.Zero;

        private bool IsPositionRendered(Point point) {
            return point.X >= 0 && point.X <= Window.ClientBounds.Width && point.Y >= 0 && point.Y <= Window.ClientBounds.Height;
        }

        protected override void Update(GameTime gameTime)
        {
            //if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
            //    Exit();

            // TODO: Add your update logic here

            if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                GameStateRenderer.AttemptSelection();
            }




            Vector3 mouse_position = new Vector3(Mouse.GetState().X, Mouse.GetState().Y, Mouse.GetState().ScrollWheelValue);

            Vector3 mouse_delta = mouse_position - mouse_position_previous;

            mouse_position_previous = mouse_position;

            if (IsPositionRendered(Mouse.GetState().Position)) {

                cameraPosition.Z += mouse_delta.Z * SCROLL_SENSITIVITY;

                if (Mouse.GetState().MiddleButton == ButtonState.Pressed) {
                    cameraPosition.X += mouse_delta.X / MathF.Pow(10, cameraPosition.Z);
                    cameraPosition.Y += mouse_delta.Y / MathF.Pow(10, cameraPosition.Z);
                }

            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Lavender);

            // TODO: Add your drawing code here

            spriteBatch.Begin(
                transformMatrix: CamMatrix,
                samplerState: SamplerState.PointClamp,
                effect: msaa,
                blendState: null
            );
            Vector2 mpos = Mouse.GetState().Position.ToVector2();
            GameStateRenderer.ws_mpos = Vector2.Transform(mpos, CamMatrixInv);

            GameStateRenderer.Render(spriteBatch, cameraPosition, Window.ClientBounds.Size.ToVector2());

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
