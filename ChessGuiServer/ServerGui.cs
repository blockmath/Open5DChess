using ChessCommon;
using ChessGui;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Net;


namespace ChessGuiServer {
    public class ServerGui : ChessGui.ChessGui {

        public ChessServer.ChessServer server;

        public IPEndPoint endPoint;

        string password = "";

        private bool serverStarted = false;


        SpriteFontBase monospace_font;

        // static Rectangle IPRect = new Rectangle(-15, -120, 150, 32);
        static Rectangle PasswordRect = new Rectangle(-115, -80, 250, 32);
        static Rectangle StartServerRect = new Rectangle(-115, -40, 250, 50);

        bool StartServerHovered = false;
        // bool IpRectHovered = false;
        bool PasswordRectHovered = false;

        // bool IpFieldOpen = false;
        bool PasswordFieldOpen = false;

        // bool ipFieldFull = false;
        bool passwordFieldFull = false;


        protected override void Initialize() {
            base.Initialize();

            server = new ChessServer.ChessServer();

            server.gameState = renderer.gameState;

            renderer.ShouldDrawControlButtons = false;
        }

        protected override void Update(GameTime gameTime) {
            if (serverStarted) {
                base.Update(gameTime);
            } else {
                StartServerHovered = StartServerRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                PasswordRectHovered = PasswordRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                // IpRectHovered = IPRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && MouseWasntPressed) {
                    /*if (!IpRectHovered) {
                        UnregisterIpInput();
                    }*/
                    if (!PasswordRectHovered) {
                        UnregisterPasswordInput();
                    }

                    if (StartServerHovered) {
                        StartServer();
                    } else if (PasswordRectHovered) {
                        RegisterPasswordInput();
                    }
                }

                MouseWasntPressed = !(Mouse.GetState().LeftButton == ButtonState.Pressed);

                base.BaseUpdate(gameTime);
            }
        }

        protected override void Draw(GameTime gameTime) {

            if (serverStarted) {
                renderer.infoText = TextLocalizer.Get("instance") + " " + ChessCommand.PortFromPassCode(server.passcode) + "\n" + TextLocalizer.Get("white_player") + TextLocalizer.Get(server.IsColourConnected(GameColour.WHITE) ? "connected" : "not_connected") + "\n" + TextLocalizer.Get("black_player") + TextLocalizer.Get(server.IsColourConnected(GameColour.BLACK) ? "connected" : "not_connected");

                if (!server.gameState.playerHasLost.hasNone()) {
                    if (server.gameState.playerHasLost.hasBoth()) {
                        renderer.infoText += "\n" + TextLocalizer.Get("nobody_win");
                    } else if (server.gameState.playerHasLost.hasWhite()) {
                        if (server.gameState.timer.us_white > 0) {
                            renderer.infoText += "\n" + TextLocalizer.Get("black_win_checkmate");
                        } else {
                            renderer.infoText += "\n" + TextLocalizer.Get("black_win_timeout");
                        }
                    } else if (server.gameState.playerHasLost.hasBlack()) {
                        if (server.gameState.timer.us_black > 0) {
                            renderer.infoText += "\n" + TextLocalizer.Get("white_win_checkmate");
                        } else {
                            renderer.infoText += "\n" + TextLocalizer.Get("white_win_timeout");
                        }
                    }
                } else {
                    renderer.infoText += "\n" + TextLocalizer.Get(server.gameState.activePlayer.isBlack() ? "black_turn" : "white_turn");
                }

                base.Draw(gameTime);
            } else {

                GraphicsDevice.Clear(renderer.NothingGridColour);

                renderer.spriteBatch.Begin(
                    transformMatrix: WindowCentreMatrix
                );

                monospace_font = renderer.clockFontSystem.GetFont(24);

                SpriteFontBase text_font = renderer.fontSystem.GetFont(42);

                /*
                renderer.spriteBatch.Draw(renderer.sq, IPRect, IpRectHovered ? new Color(64, 64, 64) : new Color(32, 32, 32));
                string ipDispStr = ipstr + (IpFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? (ipFieldFull ? "|" : "_") : "");
                renderer.spriteBatch.DrawString(monospace_font, ipDispStr, new Vector2(IPRect.Left, IPRect.Center.Y) + new Vector2(9, -13f), Color.White);
                
                string port_str = TextLocalizer.Get("port");
                Vector2 portStrBounds = text_font.MeasureString(port_str);
                renderer.spriteBatch.DrawString(text_font, port_str, new Vector2(IPRect.Left - (portStrBounds.X + 9), IPRect.Center.Y - portStrBounds.Y / 2 - 5), Color.Black);
                */

                renderer.spriteBatch.Draw(renderer.sq, PasswordRect, PasswordRectHovered ? new Color(64, 64, 64) : new Color(32, 32, 32));
                string pwDispStr = password + (PasswordFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? (passwordFieldFull ? "|" : "_") : "");
                renderer.spriteBatch.DrawString(monospace_font, pwDispStr, new Vector2(PasswordRect.Left, PasswordRect.Center.Y) + new Vector2(9, -13f), Color.White);



                renderer.spriteBatch.Draw(renderer.sq, StartServerRect, StartServerHovered ? GameStateRenderer.TIME_COLOUR_LIGHT : Color.AntiqueWhite);
                string START_SERVER_STR = TextLocalizer.Get("start_server");

                Vector2 start_server_bounds = text_font.MeasureString(START_SERVER_STR);
                renderer.spriteBatch.DrawString(text_font, START_SERVER_STR, StartServerRect.Center.ToVector2() + start_server_bounds / -2 + new Vector2(0, -2.5f), Color.Black);


                renderer.spriteBatch.End();

                base.BaseDraw(gameTime);
            }

        }



        public override ColourRights GetRights() {
            return ColourRights.NONE;
        }

        public override void GuiUndoMove() {
            
        }

        public override void MakeMove(Move move) {
            
        }

        public override void SubmitMoves() {
            
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

        public void StartServer() {
            server.passcode = ChessCommand.CodeFromPassCode(password);
            endPoint = new IPEndPoint(IPAddress.Any, 0x5DC);
            server.ServerStart(endPoint);
            serverStarted = true;
        }

    }
}
