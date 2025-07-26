using ChessCommon;
using ChessBot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using FontStashSharp;
using System.Diagnostics;
using System.IO;

namespace ChessClient
{
    public class ClientGame : Game
    {

        const float SCROLL_SENSITIVITY = 1.0f / 2400.0f;

        private GraphicsDeviceManager graphics;
        private SpriteBatch spriteBatch;

        private Effect msaa;
        private FontSystem fontSystem;
        private FontSystem clockFontSystem;

        Vector3 cameraPosition = Vector3.Zero;

        Matrix WindowCentreMatrixX => Matrix.CreateTranslation(new Vector3(Window.ClientBounds.Size.ToVector2().X / 2, 0, 0));
        Matrix WindowCentreMatrixY => Matrix.CreateTranslation(new Vector3(0, Window.ClientBounds.Size.ToVector2().Y / 2, 0));
        Matrix WindowCentreMatrix => Matrix.CreateTranslation(new Vector3(Window.ClientBounds.Size.ToVector2() / 2, 0));

        Matrix CamMatrixScale => Matrix.CreateScale(MathF.Pow(10, cameraPosition.Z));
        Matrix CamMatrixScaleInv => Matrix.Invert(CamMatrixScale);
        Matrix CamMatrix => Matrix.CreateTranslation(cameraPosition * new Vector3(1, 1, 0)) * CamMatrixScale * WindowCentreMatrix;
        Matrix CamMatrixInv => Matrix.Invert(CamMatrix);


        static Rectangle ButtonSubmitRect = new Rectangle(10, 10, 225, 50);
        static Rectangle ClockMainRect = new Rectangle(10, 10, 150, 80);
        static Rectangle ClockWhiteRect = new Rectangle(20, 20, 130, 30);
        static Rectangle ClockBlackRect = new Rectangle(20, 50, 130, 30);


        bool SubmitIsAllowed => GameStateRenderer.gameState.CanSubmitMoves();
        bool submitHovered = false;

        static Rectangle ButtonUndoRect = new Rectangle(-210, 10, 200, 50);
        bool UndoIsAllowed => GameStateRenderer.gameState.CanUndoMoves();
        bool undoHovered = false;

        static Color BUTTON_COLOUR_UNAVAILABLE = Color.LightGray;
        static Color BUTTON_COLOUR_SUBMITWHITE = Color.AntiqueWhite;
        static Color BUTTON_COLOUR_SUBMITBLACK = new Color(48, 48, 48);
        static Color BUTTON_COLOUR_UNDOMOVE = new Color(255, 245, 103);
        static Color BUTTON_COLOUR_UNDOTRAVEL = Color.MediumPurple;
        static Color BUTTON_COLOUR_HOVERED = Color.LimeGreen;

        static Color BG_CLEAR_COLOUR = new Color(228, 228, 236);


        BotInterface whiteInterface;
        BotInterface blackInterface;

        private bool thinkQueued = false;



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
            blackInterface = null;

            //whiteInterface = new BotInterface<ChessBot.ChessBot>(GameStateRenderer.gameState);
            //blackInterface = new BotInterface<ChessBot.ChessBot>(GameStateRenderer.gameState);

            GameStateRenderer.userRights = (whiteInterface is null ? ColourRights.WHITE : ColourRights.NONE) | (blackInterface is null ? ColourRights.BLACK : ColourRights.NONE);

            cameraPosition -= new Vector3(GameStateRenderer.GetInitialCameraState(), 0);

            GameStateRenderer.gameState.timer = new Timer(30_000_000L, 5_000_000L);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            // TODO: use this.Content to load your game content here

            GameStateRenderer.pieceTexture = Content.Load<Texture2D>("pieces");
            GameStateRenderer.sq = Content.Load<Texture2D>("sq");
            GameStateRenderer.arsq = Content.Load<Texture2D>("arrow");
            GameStateRenderer.circle = Content.Load<Texture2D>("circle");
            msaa = Content.Load<Effect>("msaa");

            fontSystem = new FontSystem();
            fontSystem.AddFont(File.ReadAllBytes("CrimsonText-Regular.ttf"));

            GameStateRenderer.fontSystem = new FontSystem();
            GameStateRenderer.fontSystem.AddFont(File.ReadAllBytes("CrimsonText-Italic.ttf"));

            GameStateRenderer.gridFontSystem = new FontSystem();
            GameStateRenderer.gridFontSystem.AddFont(File.ReadAllBytes("PublicSans-ExtraBold.ttf"));

            clockFontSystem = new FontSystem();
            clockFontSystem.AddFont(File.ReadAllBytes("Inconsolata-Regular.ttf"));
        }

        Vector3 mouse_position_previous = Vector3.Zero;

        private bool IsPositionRendered(Point point) {
            return point.X >= 0 && point.X <= Window.ClientBounds.Width && point.Y >= 0 && point.Y <= Window.ClientBounds.Height;
        }

        protected bool IsPositionRenderedBuffer(Point point, float buffer) {
            return point.X >= 0 - buffer && point.X <= Window.ClientBounds.Width + buffer && point.Y >= 0 - buffer && point.Y <= Window.ClientBounds.Height + buffer;
        }

        protected override void Update(GameTime gameTime)
        {

            if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                if (submitHovered) {
                    if (GameStateRenderer.colourWon.isNone() && SubmitIsAllowed) GameStateRenderer.gameState.SubmitMoves();
                } else if (undoHovered) {
                    if (UndoIsAllowed) GameStateRenderer.gameState.GuiUndoMove();
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

            try {
                GameStateRenderer.gameState.timer.Tick((long)(gameTime.ElapsedGameTime.TotalNanoseconds / 1000));
            } catch (ChessTimeOutException e) {
                GameStateRenderer.colourWon = e.colour.inverse();
                GameStateRenderer.gameState.timer.Stop();

                if (whiteInterface is not null && whiteInterface.IsThinking()) {
                    whiteInterface.HaltThink();
                }

                if (blackInterface is not null && blackInterface.IsThinking()) {
                    blackInterface.HaltThink();
                }
            }



            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(GameStateRenderer.NothingGridColour);

            // Draw game boards and other "world-space" objects
            spriteBatch.Begin(
                transformMatrix: CamMatrix,
                samplerState: SamplerState.PointClamp,
                effect: msaa,
                blendState: null
            );
            Vector2 mpos = Mouse.GetState().Position.ToVector2();
            GameStateRenderer.ws_mpos = Vector2.Transform(mpos, CamMatrixInv);
            GameStateRenderer.ws_i = Vector2.Transform(GameStateRenderer.PIECE_SIZE, CamMatrixScaleInv);

            GameStateRenderer.Render(spriteBatch, cameraPosition, Window.ClientBounds.Size.ToVector2());

            spriteBatch.End();



            // Draw UI and other screen-space objects


            // Draw top-middle buttons
            spriteBatch.Begin(
                transformMatrix: WindowCentreMatrixX,
                samplerState: SamplerState.PointClamp,
                effect: msaa,
                blendState: null
            );

            submitHovered = undoHovered = false;

            Color submitButtonColour = BUTTON_COLOUR_UNAVAILABLE;
            Color undoButtonColour = BUTTON_COLOUR_UNAVAILABLE;

            Color submitTextColor = Color.Black;
            Color undoTextColor = Color.Black;

            if (SubmitIsAllowed) {
                submitButtonColour = BUTTON_COLOUR_SUBMITWHITE;
                if (GameStateRenderer.gameState.activePlayer.isBlack()) {
                    submitButtonColour = BUTTON_COLOUR_SUBMITBLACK;
                    submitTextColor = Color.White;
                }
                if (ButtonSubmitRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrixX)))) {
                    submitButtonColour = Color.Lerp(BUTTON_COLOUR_HOVERED, submitButtonColour, 0.5f);
                    submitHovered = true;
                }
            }

            if (UndoIsAllowed) {
                undoButtonColour = GameStateRenderer.gameState.LastMoveWasTravel() ? BUTTON_COLOUR_UNDOTRAVEL : BUTTON_COLOUR_UNDOMOVE;
                if (ButtonUndoRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrixX)))) {
                    undoButtonColour = Color.Lerp(BUTTON_COLOUR_HOVERED, undoButtonColour, 0.5f);
                    undoHovered = true;
                }
            }

            spriteBatch.Draw(GameStateRenderer.sq, ButtonSubmitRect, submitButtonColour);
            spriteBatch.Draw(GameStateRenderer.sq, ButtonUndoRect, undoButtonColour);

            SpriteFontBase spriteFont = fontSystem.GetFont(42);
            spriteBatch.DrawString(spriteFont, "Submit Moves", ButtonSubmitRect.Center.ToVector2() - spriteFont.MeasureString("Submit Moves") / 2 + new Vector2(0, -5), submitTextColor);
            spriteBatch.DrawString(spriteFont, "Undo Move", ButtonUndoRect.Center.ToVector2() - spriteFont.MeasureString("Undo Move") / 2 + new Vector2(0, -5), undoTextColor);


            spriteBatch.End();


            // Draw top-left time controls

            spriteBatch.Begin(
                transformMatrix: Matrix.Identity,
                samplerState: SamplerState.PointClamp,
                effect: msaa,
                blendState: null
            );

            SpriteFontBase clockFont = clockFontSystem.GetFont(24);

            spriteBatch.Draw(GameStateRenderer.sq, ClockMainRect, GameStateRenderer.gameState.activePlayer.isBlack() ? GameStateRenderer.BLACK_BOARD_COLOUR_SHADED_A : GameStateRenderer.WHITE_BOARD_COLOUR_SHADED_A);

            spriteBatch.Draw(GameStateRenderer.sq, ClockWhiteRect, GameStateRenderer.gameState.activePlayer.isBlack() ? GameStateRenderer.BLACK_BOARD_COLOUR_SHADED_B : GameStateRenderer.TIME_COLOUR_LIGHT);
            spriteBatch.Draw(GameStateRenderer.sq, ClockBlackRect, GameStateRenderer.gameState.activePlayer.isBlack() ? GameStateRenderer.TIME_COLOUR_DARK : GameStateRenderer.WHITE_BOARD_COLOUR_SHADED_B);

            spriteBatch.DrawString(clockFont, "W: " + GameStateRenderer.gameState.timerView.ToString(GameColour.WHITE), ClockWhiteRect.Location.ToVector2() + new Vector2(5, 2), GameStateRenderer.gameState.activePlayer.isBlack() ? Color.White : Color.Black);
            spriteBatch.DrawString(clockFont, "B: " + GameStateRenderer.gameState.timerView.ToString(GameColour.BLACK), ClockBlackRect.Location.ToVector2() + new Vector2(5, 2), GameStateRenderer.gameState.activePlayer.isBlack() ? Color.White : Color.Black);

            spriteBatch.End();


            base.Draw(gameTime);
        }
    }
}
