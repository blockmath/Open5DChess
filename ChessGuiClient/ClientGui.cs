using ChessCommon;
using ChessGui;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Net;

namespace ChessGuiClient {
    public class ClientGui : ChessGui.ChessGui {

        public GameColour colour = GameColour.NONE;

        public ChessClient.ChessClient client;

        public IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 1500);
        string ipstr;

        private bool hasConnected = false;



        static Rectangle IPRect = new Rectangle(-115, -80, 250, 32);

        static Rectangle JoinWhiteRect = new Rectangle(-115, -40, 250, 50);
        static Rectangle JoinBlackRect = new Rectangle(-115, 20, 250, 50);


        bool JoinWhiteHovered = false;
        bool JoinBlackHovered = false;
        bool IpRectHovered = false;

        bool IpFieldOpen = false;

        protected override void Initialize() {
            base.Initialize();

            client = new ChessClient.ChessClient();
            client.personalState = renderer.gameState;

            renderer.ShouldDrawControlButtons = true;

            ipstr = endPoint.ToString();

            //Connect();
        }

        protected override void Update(GameTime gameTime) {

            //client.personalState = renderer.gameState;

            renderer.perspective = colour;

            if (client.socket is null && hasConnected) {
                hasConnected = false;
                infoText = TextLocalizer.Get("server_disconnected");
            }

            if (hasConnected) {

                renderer.infoText = "";

                if (!client.personalState.playerHasLost.hasNone()) {

                    if (client.personalState.playerHasLost.hasBoth()) {
                        renderer.infoText = TextLocalizer.Get("nobody_win");
                    } else if (client.personalState.playerHasLost.hasWhite()) {
                        if (client.personalState.timer.us_white > 0) {
                            renderer.infoText = TextLocalizer.Get("black_win_checkmate");
                        } else {
                            renderer.infoText = TextLocalizer.Get("black_win_timeout");
                        }
                    } else if (client.personalState.playerHasLost.hasBlack()) {
                        if (client.personalState.timer.us_black > 0) {
                            renderer.infoText = TextLocalizer.Get("white_win_checkmate");
                        } else {
                            renderer.infoText = TextLocalizer.Get("white_win_timeout");
                        }
                    }

                    if (!colour.isNone()) {
                        if (client.personalState.playerHasLost.hasBoth()) {
                            renderer.infoText = TextLocalizer.Get("draw") + "\n" + renderer.infoText;
                        } else if (client.personalState.playerHasLost.hasRights(colour)) {
                            renderer.infoText = TextLocalizer.Get("loss") + "\n" + renderer.infoText;
                        } else {
                            renderer.infoText = TextLocalizer.Get("win") + "\n" + renderer.infoText;
                        }
                    }

                } else {
                    switch (colour) {
                        case GameColour.NONE:
                            renderer.infoText = TextLocalizer.Get(client.personalState.activePlayer.isBlack() ? "black_turn" : "white_turn");
                            break;
                        case GameColour.WHITE:
                            renderer.infoText = TextLocalizer.Get(client.personalState.activePlayer.isBlack() ? "opp_turn_black" : "your_turn_white");
                            break;
                        case GameColour.BLACK:
                            renderer.infoText = TextLocalizer.Get(client.personalState.activePlayer.isBlack() ? "your_turn_black" : "opp_turn_white");
                            break;
                    }
                }

                base.Update(gameTime);
            } else {

                JoinWhiteHovered = JoinWhiteRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                JoinBlackHovered = JoinBlackRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                IpRectHovered = IPRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));


                if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                    if (!IpRectHovered) {
                        UnregisterIpInput();
                    }

                    if (JoinWhiteHovered) {
                        colour = GameColour.WHITE;
                        Connect();
                    } else if (JoinBlackHovered) {
                        colour = GameColour.BLACK;
                        Connect();
                    } else if (IpRectHovered) {
                        RegisterIpInput();
                    }
                }



                base.BaseUpdate(gameTime);
            }
        }

        string infoText = "";

        protected override void Draw(GameTime gameTime) {
            if (hasConnected) {
                base.Draw(gameTime);
            } else {

                GraphicsDevice.Clear(renderer.NothingGridColour);

                renderer.spriteBatch.Begin(
                    transformMatrix: WindowCentreMatrix
                );

                SpriteFontBase monospace_font = renderer.clockFontSystem.GetFont(24);

                renderer.spriteBatch.Draw(renderer.sq, IPRect, IpRectHovered ? new Color(64, 64, 64) : new Color(32, 32, 32));
                // ipstr = "255.255.255.255:65535";
                string ipDispStr = ipstr + (IpFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? "_" : "");
                renderer.spriteBatch.DrawString(monospace_font, ipDispStr, new Vector2(IPRect.Left, IPRect.Center.Y) + new Vector2(9, -13f), Color.White);

                SpriteFontBase text_font = renderer.fontSystem.GetFont(42);


                renderer.spriteBatch.Draw(renderer.sq, JoinWhiteRect, JoinWhiteHovered ? GameStateRenderer.TIME_COLOUR_LIGHT : Color.AntiqueWhite);
                string WHITE_JOIN_STR = TextLocalizer.Get("join_as_white");

                Vector2 white_join_bounds = text_font.MeasureString(WHITE_JOIN_STR);
                renderer.spriteBatch.DrawString(text_font, WHITE_JOIN_STR, JoinWhiteRect.Center.ToVector2() + white_join_bounds / -2 + new Vector2(0, -2.5f), Color.Black);



                renderer.spriteBatch.Draw(renderer.sq, JoinBlackRect, JoinBlackHovered ? GameStateRenderer.TIME_COLOUR_DARK : new Color(79, 58, 47));
                string BLACK_JOIN_STR = TextLocalizer.Get("join_as_black");

                Vector2 black_join_bounds = text_font.MeasureString(BLACK_JOIN_STR);
                renderer.spriteBatch.DrawString(text_font, BLACK_JOIN_STR, JoinBlackRect.Center.ToVector2() + black_join_bounds / -2 + new Vector2(0, -2.5f), Color.White);


                renderer.spriteBatch.End();


                if (infoText != "") {
                    // Draw lower-left info text

                    renderer.spriteBatch.Begin(
                        transformMatrix: Matrix.CreateTranslation(new Vector3(0, Window.ClientBounds.Height, 0)),
                        samplerState: SamplerState.PointClamp,
                        effect: renderer.msaa,
                        blendState: null
                    );

                    Vector2 textBounds = text_font.MeasureString(infoText, lineSpacing: -10);
                    renderer.spriteBatch.DrawString(text_font, infoText, new Vector2(10, -10 - textBounds.Y), Color.Black, lineSpacing: -10);

                    renderer.spriteBatch.End();

                }



                base.BaseDraw(gameTime);
            }
        }

        private void Connect() {

            if (client.Connect(endPoint, colour)) {
                hasConnected = true;
            } else {
                infoText = TextLocalizer.Get("server_connection_failed");
            }

        }


        public override ColourRights GetRights() {
            return colour.GetRights();
        }

        public override void GuiUndoMove() {
            if (renderer.gameState.CanUndoMoves(GetRights())) {
                renderer.gameState.GuiUndoMove();
                client.SendCommand(new ChessCommand(CommandType.UNDO, colour));
            }
        }

        public override void MakeMove(Move move) {
            renderer.gameState.MakeMoveValidated(move, renderer.userRights);
            client.SendCommand(new ChessCommand(CommandType.MOVE, colour, move));
        }

        public override void SubmitMoves() {
            if (renderer.gameState.CanSubmitMoves(GetRights())) {
                renderer.gameState.SubmitMoves();
                client.SendCommand(new ChessCommand(CommandType.SUBMIT, colour));
            }
        }



        public void OnInput(object sender, TextInputEventArgs e) {
            Keys key = e.Key;
            char c = e.Character;

            if (key == Keys.Enter) {
                UnregisterIpInput();
            } else if (key == Keys.Delete || key == Keys.Back) {
                ipstr = ipstr.Substring(0, ipstr.Length - 1);
            } else {
                ipstr += c;
            }
        }


        public void RegisterIpInput() {
            if (!IpFieldOpen) {
                IpFieldOpen = true;
                Window.TextInput += OnInput;
            }
        }

        public void UnregisterIpInput() {
            if (IpFieldOpen) {
                IpFieldOpen = false;
                Window.TextInput -= OnInput;
            }
        }
    }
}
