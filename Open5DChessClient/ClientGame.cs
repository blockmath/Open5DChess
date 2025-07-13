using ChessCommon;
using ChessBot;
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

        Matrix WindowCentreMatrixX => Matrix.CreateTranslation(new Vector3(Window.ClientBounds.Size.ToVector2().X / 2, 0, 0));
        Matrix WindowCentreMatrixY => Matrix.CreateTranslation(new Vector3(0, Window.ClientBounds.Size.ToVector2().Y / 2, 0));
        Matrix WindowCentreMatrix => Matrix.CreateTranslation(new Vector3(Window.ClientBounds.Size.ToVector2() / 2, 0));
        Matrix CamMatrix => Matrix.CreateTranslation(cameraPosition * new Vector3(1, 1, 0)) * Matrix.CreateScale(MathF.Pow(10, cameraPosition.Z)) * WindowCentreMatrix;
        Matrix CamMatrixInv => Matrix.Invert(CamMatrix);


        static Rectangle ButtonSubmitRect = new Rectangle(10, 10, 200, 50);
        bool SubmitIsAllowed => GameStateRenderer.gameState.CanSubmitMoves();
        bool submitHovered = false;

        static Rectangle ButtonUndoRect = new Rectangle(-210, 10, 200, 50);
        bool UndoIsAllowed => GameStateRenderer.gameState.CanUndoMoves();
        bool undoHovered = false;

        static Color BUTTON_COLOUR_UNAVAILABLE = Color.LightGoldenrodYellow;
        static Color BUTTON_COLOUR_AVAILABLE = Color.Goldenrod;
        static Color BUTTON_COLOUR_HOVERED = Color.DarkGoldenrod;


        BotInterface whiteInterface;
        BotInterface blackInterface;

        bool thinkQueued = false;


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

            whiteInterface = null;
            blackInterface = new BotInterface<ChessBot.ChessBot>(GameStateRenderer.gameState);

            GameStateRenderer.userRights = (whiteInterface is null ? ColourRights.WHITE : ColourRights.NONE) | (blackInterface is null ? ColourRights.BLACK : ColourRights.NONE);

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

            if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                if (submitHovered) {
                    if (SubmitIsAllowed) GameStateRenderer.gameState.SubmitMoves();
                } else if (undoHovered) {
                    if (UndoIsAllowed) GameStateRenderer.gameState.UnmakeMove();
                } else {
                    GameStateRenderer.AttemptSelection();
                }
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


            // Poll bots and such

            if (GameStateRenderer.gameState.activePlayer.isWhite()) {
                if (whiteInterface is not null) {
                    if (!whiteInterface.IsThinking()) {
                        if (!thinkQueued) {
                            whiteInterface.StartThink();
                            thinkQueued = true;
                        } else {
                            thinkQueued = false;
                            Move chosenMove = whiteInterface.GetMove();
                            if (chosenMove is not null) {
                                GameStateRenderer.gameState.MakeMove(chosenMove);
                            }
                        }
                    }
                }
            } else if (GameStateRenderer.gameState.activePlayer.isBlack()) {
                if (blackInterface is not null) {
                    if (!blackInterface.IsThinking()) {
                        if (!thinkQueued) {
                            blackInterface.StartThink();
                            thinkQueued = true;
                        } else {
                            thinkQueued = false;
                            Move chosenMove = blackInterface.GetMove();
                            if (chosenMove is not null) {
                                GameStateRenderer.gameState.MakeMove(chosenMove);
                            }
                        }
                    }
                }
            }
            
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Lavender);

            // Draw game boards and other "world-space" objects
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



            // Draw UI and other screen-space objects
            spriteBatch.Begin(
                transformMatrix: WindowCentreMatrixX,
                samplerState: SamplerState.PointClamp,
                effect: msaa,
                blendState: null
            );

            submitHovered = undoHovered = false;

            Color submitButtonColour = BUTTON_COLOUR_UNAVAILABLE;
            Color undoButtonColour = BUTTON_COLOUR_UNAVAILABLE;

            if (SubmitIsAllowed) {
                submitButtonColour = BUTTON_COLOUR_AVAILABLE;
                if (ButtonSubmitRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrixX)))) {
                    submitButtonColour = BUTTON_COLOUR_HOVERED;
                    submitHovered = true;
                }
            }

            if (UndoIsAllowed) {
                undoButtonColour = BUTTON_COLOUR_AVAILABLE;
                if (ButtonUndoRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrixX)))) {
                    undoButtonColour = BUTTON_COLOUR_HOVERED;
                    undoHovered = true;
                }
            }

            spriteBatch.Draw(GameStateRenderer.sq, ButtonSubmitRect, submitButtonColour);
            spriteBatch.Draw(GameStateRenderer.sq, ButtonUndoRect, undoButtonColour);


            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
