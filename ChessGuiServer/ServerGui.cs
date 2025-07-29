using ChessCommon;
using ChessGui;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
//using System.Drawing;
using System.Net;


namespace ChessGuiServer {
    public class ServerGui : ChessGui.ChessGui {

        public ChessServer.ChessServer server;

        public IPEndPoint endPoint;

        string ipstr;

        private bool serverStarted = false;



        static Rectangle IPRect = new Rectangle(-15, -80, 150, 32);
        static Rectangle StartServerRect = new Rectangle(-115, -40, 250, 50);

        bool StartServerHovered = false;
        bool IpRectHovered = false;

        bool IpFieldOpen = false;

        protected override void Initialize() {
            base.Initialize();

            server = new ChessServer.ChessServer();

            server.gameState = renderer.gameState;

            renderer.ShouldDrawControlButtons = false;

            ipstr = OptionsLoader.Get("server_port");
        }

        protected override void Update(GameTime gameTime) {
            if (serverStarted) {
                base.Update(gameTime);
            } else {
                StartServerHovered = StartServerRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                IpRectHovered = IPRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));

                if (Mouse.GetState().LeftButton == ButtonState.Pressed) {
                    if (!IpRectHovered) {
                        UnregisterIpInput();
                    }

                    if (StartServerHovered) {
                        StartServer();
                    } else if (IpRectHovered) {
                        RegisterIpInput();
                    }
                }

                base.BaseUpdate(gameTime);
            }
        }

        protected override void Draw(GameTime gameTime) {

            if (serverStarted) {
                renderer.infoText = TextLocalizer.Get("port") + " " + endPoint.Port + "\n" + TextLocalizer.Get("white_player") + TextLocalizer.Get(server.IsColourConnected(GameColour.WHITE) ? "connected" : "not_connected") + "\n" + TextLocalizer.Get("black_player") + TextLocalizer.Get(server.IsColourConnected(GameColour.BLACK) ? "connected" : "not_connected");

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

                SpriteFontBase monospace_font = renderer.clockFontSystem.GetFont(24);

                renderer.spriteBatch.Draw(renderer.sq, IPRect, IpRectHovered ? new Color(64, 64, 64) : new Color(32, 32, 32));
                string ipDispStr = ipstr + (IpFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? "_" : "");
                renderer.spriteBatch.DrawString(monospace_font, ipDispStr, new Vector2(IPRect.Left, IPRect.Center.Y) + new Vector2(9, -13f), Color.White);

                SpriteFontBase text_font = renderer.fontSystem.GetFont(42);

                string port_str = TextLocalizer.Get("port");
                Vector2 portStrBounds = text_font.MeasureString(port_str);
                renderer.spriteBatch.DrawString(text_font, port_str, new Vector2(IPRect.Left - (portStrBounds.X + 9), IPRect.Center.Y - portStrBounds.Y / 2 - 5), Color.Black);


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

        public void StartServer() {
            endPoint = new IPEndPoint(IPAddress.Loopback, int.Parse(ipstr));
            server.ServerStart(endPoint);
            serverStarted = true;
        }

    }
}
