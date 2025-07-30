using ChessCommon;
using ChessGui;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Diagnostics;
using System.Net;

namespace ChessGuiClient {
    public class ClientGui : ChessGui.ChessGui {

        public GameColour colour = GameColour.NONE;

        public ChessClient.ChessClient client;

        public IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 1500);
        string ipstr;
        string password = "";

        private bool hasConnected = false;

        SpriteFontBase monospace_font;


        static Rectangle IPRect = new Rectangle(-115, -120, 250, 32);
        static Rectangle PasswordRect = new Rectangle(-115, -80, 250, 32);

        static Rectangle JoinWhiteRect = new Rectangle(-115, -40, 250, 50);
        static Rectangle JoinBlackRect = new Rectangle(-115, 20, 250, 50);
        static Rectangle JoinSpectatorRect = new Rectangle(-115, 80, 250, 50);


        bool JoinWhiteHovered = false;
        bool JoinBlackHovered = false;
        bool JoinSpectatorHovered = false;
        bool IpRectHovered = false;
        bool PasswordRectHovered = false;

        bool IpFieldOpen = false;
        bool PasswordFieldOpen = false;

        bool ipFieldFull = false;
        bool passwordFieldFull = false;

        protected override void Initialize() {
            base.Initialize();

            client = new ChessClient.ChessClient();
            client.personalState = renderer.gameState;

            renderer.ShouldDrawControlButtons = true;

            ipstr = "localhost";

            //Connect();
        }

        protected override void Update(GameTime gameTime) {

            //client.personalState = renderer.gameState;

            renderer.perspective = colour;

            if (client.socket is null && hasConnected) {
                hasConnected = false;
                infoText = TextLocalizer.Get(client.DisconnectReason);
                client.DisconnectReason = "server_disconnected";
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
                JoinSpectatorHovered = JoinSpectatorRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                IpRectHovered = IPRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                PasswordRectHovered = PasswordRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));


                if (Mouse.GetState().LeftButton == ButtonState.Pressed && MouseWasntPressed && !isConnecting) {
                    if (!IpRectHovered) {
                        UnregisterIpInput();
                    }
                    if (!PasswordRectHovered) {
                        UnregisterPasswordInput();
                    }

                    if (JoinWhiteHovered) {
                        colour = GameColour.WHITE;
                        Connect();
                    } else if (JoinBlackHovered) {
                        colour = GameColour.BLACK;
                        Connect();
                    } else if (JoinSpectatorHovered) {
                        colour = GameColour.NONE;
                        Connect();
                    } else if (IpRectHovered) {
                        RegisterIpInput();
                    } else if (PasswordRectHovered) {
                        RegisterPasswordInput();
                    }
                }

                MouseWasntPressed = !(Mouse.GetState().LeftButton == ButtonState.Pressed);

                base.BaseUpdate(gameTime);
            }
        }

        string infoText = "";

        private static void DrawThrobber(SpriteBatch spriteBatch, Texture2D node, Vector2 origin, Color colour, int index) {
            int i = index % 8;
            const float a = 6.21f, b = 15;
            if (i != 0) spriteBatch.Draw(node, origin + new Vector2(-a, +b), default, colour, 0, default, 0.0075f, default, default);
            if (i != 1) spriteBatch.Draw(node, origin + new Vector2(-b, +a), default, colour, 0, default, 0.0075f, default, default);
            if (i != 2) spriteBatch.Draw(node, origin + new Vector2(-b, -a), default, colour, 0, default, 0.0075f, default, default);
            if (i != 3) spriteBatch.Draw(node, origin + new Vector2(-a, -b), default, colour, 0, default, 0.0075f, default, default);
            if (i != 4) spriteBatch.Draw(node, origin + new Vector2(+a, -b), default, colour, 0, default, 0.0075f, default, default);
            if (i != 5) spriteBatch.Draw(node, origin + new Vector2(+b, -a), default, colour, 0, default, 0.0075f, default, default);
            if (i != 6) spriteBatch.Draw(node, origin + new Vector2(+b, +a), default, colour, 0, default, 0.0075f, default, default);
            if (i != 7) spriteBatch.Draw(node, origin + new Vector2(+a, +b), default, colour, 0, default, 0.0075f, default, default);
        }

        protected override void Draw(GameTime gameTime) {
            if (hasConnected) {
                base.Draw(gameTime);
            } else {

                GraphicsDevice.Clear(renderer.NothingGridColour);

                renderer.spriteBatch.Begin(
                    transformMatrix: WindowCentreMatrix,
                    effect: renderer.msaa
                );

                monospace_font = renderer.clockFontSystem.GetFont(24);

                renderer.spriteBatch.Draw(renderer.sq, IPRect, IpRectHovered ? new Color(64, 64, 64) : new Color(32, 32, 32));
                string ipDispStr = ipstr + (IpFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? (ipFieldFull ? "|" : "_") : "");
                bool idsIsEmpty = ipDispStr.Length == 0 && !IpFieldOpen;
                if (idsIsEmpty) {
                    ipDispStr = "127.0.0.1";
                }
                renderer.spriteBatch.DrawString(monospace_font, ipDispStr, new Vector2(IPRect.Left, IPRect.Center.Y) + new Vector2(9, -13f), idsIsEmpty ? Color.Gray : Color.White);


                renderer.spriteBatch.Draw(renderer.sq, PasswordRect, PasswordRectHovered ? new Color(64, 64, 64) : new Color(32, 32, 32));
                string hiddenPassword = new('*', password.Length);
                string pwDispStr = hiddenPassword + (PasswordFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? (passwordFieldFull ? "|" : "_") : "");
                bool pdsIsEmpty = pwDispStr.Length == 0 && !PasswordFieldOpen;
                if (pdsIsEmpty) {
                    pwDispStr = "Password";
                }
                renderer.spriteBatch.DrawString(monospace_font, pwDispStr, new Vector2(PasswordRect.Left, PasswordRect.Center.Y) + new Vector2(9, -13f), pdsIsEmpty ? Color.Gray : Color.White);


                SpriteFontBase text_font = renderer.fontSystem.GetFont(42);


                renderer.spriteBatch.Draw(renderer.sq, JoinWhiteRect, JoinWhiteHovered ? GameStateRenderer.TIME_COLOUR_LIGHT : Color.AntiqueWhite);
                string WHITE_JOIN_STR = TextLocalizer.Get("join_as_white");
                bool show_throbber_white = isConnecting && colour.isWhite();

                if (show_throbber_white) {
                    DrawThrobber(renderer.spriteBatch, renderer.circle, JoinWhiteRect.Center.ToVector2() - new Vector2(3.75f, 3.75f), Color.Black, (int)(gameTime.TotalGameTime.TotalSeconds * 8));
                } else {
                    Vector2 white_join_bounds = text_font.MeasureString(WHITE_JOIN_STR);
                    renderer.spriteBatch.DrawString(text_font, WHITE_JOIN_STR, JoinWhiteRect.Center.ToVector2() + white_join_bounds / -2 + new Vector2(0, -2.5f), Color.Black);
                }



                renderer.spriteBatch.Draw(renderer.sq, JoinBlackRect, JoinBlackHovered ? GameStateRenderer.TIME_COLOUR_DARK : new Color(79, 58, 47));
                string BLACK_JOIN_STR = TextLocalizer.Get("join_as_black");
                bool show_throbber_black = isConnecting && colour.isBlack();

                if (show_throbber_black) {
                    DrawThrobber(renderer.spriteBatch, renderer.circle, JoinBlackRect.Center.ToVector2() - new Vector2(3.75f, 3.75f), Color.White, (int)(gameTime.TotalGameTime.TotalSeconds * 8));
                } else {
                    Vector2 black_join_bounds = text_font.MeasureString(BLACK_JOIN_STR);
                    renderer.spriteBatch.DrawString(text_font, BLACK_JOIN_STR, JoinBlackRect.Center.ToVector2() + black_join_bounds / -2 + new Vector2(0, -2.5f), Color.White);
                }

                renderer.spriteBatch.Draw(renderer.sq, JoinSpectatorRect, JoinSpectatorHovered ? GameStateRenderer.TIME_COLOUR_LIGHT : Color.DarkGray);
                string SPECTATOR_JOIN_STR = TextLocalizer.Get("join_as_spectator");
                bool show_throbber_spectator = isConnecting && colour.isNone();

                if (show_throbber_spectator) {
                    DrawThrobber(renderer.spriteBatch, renderer.circle, JoinSpectatorRect.Center.ToVector2() - new Vector2(3.75f, 3.75f), new Color(48, 48, 48), (int)(gameTime.TotalGameTime.TotalSeconds * 8));
                } else {
                    Vector2 spectator_join_bounds = text_font.MeasureString(SPECTATOR_JOIN_STR);
                    renderer.spriteBatch.DrawString(text_font, SPECTATOR_JOIN_STR, JoinSpectatorRect.Center.ToVector2() + spectator_join_bounds / -2 + new Vector2(0, -2.5f), Color.Black);
                }

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

        public bool isConnecting = false;

        private async void Connect() {
            isConnecting = true;

            client.passcode = ChessCommand.CodeFromPassCode(password);

            IPAddress[] addrs = Dns.GetHostAddresses(ipstr);

            int i = 0;
            while (!hasConnected && i < addrs.Length) {
                endPoint = new IPEndPoint(Dns.GetHostAddresses(ipstr)[i++].MapToIPv4(), 0x5DC);

                if (await client.Connect(endPoint, colour)) {
                    hasConnected = true;
                }
            }

            if (!hasConnected) {
                infoText = TextLocalizer.Get("server_connection_failed");
            }

            isConnecting = false;
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
            client.SendCommand(new ChessCommand(CommandType.MOVE, colour, move: move));
        }

        public override void SubmitMoves() {
            if (renderer.gameState.CanSubmitMoves(GetRights())) {
                renderer.gameState.SubmitMoves();
                client.SendCommand(new ChessCommand(CommandType.SUBMIT, colour));
            }
        }

        public void OnInputPasswordField(object sender, TextInputEventArgs e) {
            Keys key = e.Key;
            char c = e.Character;

            if (key == Keys.Enter) {
                UnregisterPasswordInput();
            } else if (key == Keys.Delete || key == Keys.Back) {
                if (password.Length > 0) {
                    password = password.Substring(0, password.Length - 1);
                }
                passwordFieldFull = false;
            } else {
                password += c;
                if (monospace_font.MeasureString(password).X + 10 > PasswordRect.Width) {
                    password = password.Substring(0, password.Length - 1);
                    passwordFieldFull = true;
                }
            }
        }

        public void OnInputIpField(object sender, TextInputEventArgs e) {
            Keys key = e.Key;
            char c = e.Character;

            if (key == Keys.Enter) {
                UnregisterIpInput();
            } else if (key == Keys.Delete || key == Keys.Back) {
                if (ipstr.Length > 0) {
                    ipstr = ipstr.Substring(0, ipstr.Length - 1);
                }
                ipFieldFull = false;
            } else {
                ipstr += c;
                if (monospace_font.MeasureString(ipstr).X + 10 > IPRect.Width) {
                    ipstr = ipstr.Substring(0, ipstr.Length - 1);
                    ipFieldFull = true;
                }
            }
        }


        public void RegisterIpInput() {
            if (!IpFieldOpen) {
                IpFieldOpen = true;
                Window.TextInput += OnInputIpField;
            }
        }

        public void UnregisterIpInput() {
            if (IpFieldOpen) {
                IpFieldOpen = false;
                Window.TextInput -= OnInputIpField;
            }
        }

        public void RegisterPasswordInput() {
            if (!PasswordFieldOpen) {
                PasswordFieldOpen = true;
                Window.TextInput += OnInputPasswordField;
            }
        }

        public void UnregisterPasswordInput() {
            if (PasswordFieldOpen) {
                PasswordFieldOpen = false;
                Window.TextInput -= OnInputPasswordField;
            }
        }
    }
}
