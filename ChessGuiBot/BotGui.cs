using ChessBot;
using ChessCommon;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;

namespace ChessGuiBot {
    public class BotGui : Game {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public BotGui() {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            sq = Content.Load<Texture2D>("sq");
            circle = Content.Load<Texture2D>("circle");

            fontSystem = new FontSystem();
            fontSystem.AddFont(File.ReadAllBytes("CrimsonText-Regular.ttf"));

            clockFontSystem = new FontSystem();
            clockFontSystem.AddFont(File.ReadAllBytes("Inconsolata-Regular.ttf"));
        }

        public GameColour colour = GameColour.NONE;

        public ChessClient.ChessClient client;
        public BotInterface botInterface;

        public IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 1500);
        string ipstr;
        string password = "";
        string botClassStr = "";

        private bool hasConnected = false;


        public static readonly Color TIME_COLOUR_LIGHT = Color.Lerp(Color.MediumPurple, Color.White, 0.25f);
        public static readonly Color TIME_COLOUR_DARK = Color.Lerp(Color.MediumPurple, Color.Black, 0.00f);

        public Texture2D sq;
        public Texture2D circle;


        FontSystem clockFontSystem;

        FontSystem fontSystem;
        SpriteFontBase spriteFont;
        SpriteFontBase monospace_font;
        
        protected Matrix WindowCentreMatrix => Matrix.CreateTranslation(new Vector3(Window.ClientBounds.Size.ToVector2() / 2, 0));


        public bool MouseWasntPressed = true;

        static Rectangle BotClassRect = new Rectangle(-115, -200, 250, 32);

        static Rectangle IPRect = new Rectangle(-115, -120, 250, 32);
        static Rectangle PasswordRect = new Rectangle(-115, -80, 250, 32);

        static Rectangle JoinWhiteRect = new Rectangle(-115, -40, 250, 50);
        static Rectangle JoinBlackRect = new Rectangle(-115, 20, 250, 50);


        bool JoinWhiteHovered = false;
        bool JoinBlackHovered = false;
        bool IpRectHovered = false;
        bool PasswordRectHovered = false;
        bool botRectHovered;

        bool IpFieldOpen = false;
        bool PasswordFieldOpen = false;
        bool BotFieldOpen = false;

        bool ipFieldFull = false;
        bool passwordFieldFull = false;
        bool botFieldFull = false;

        protected override void Initialize() {
            Window.AllowUserResizing = true;

            base.Initialize();

            client = new ChessClient.ChessClient();
            client.personalState = new GameState();

            ipstr = "localhost";

            //Connect();
        }

        protected override void Update(GameTime gameTime) {

            //client.personalState = renderer.gameState;

            if (client.socket is null && hasConnected) {
                hasConnected = false;
                infoText = TextLocalizer.Get(client.DisconnectReason);
                client.DisconnectReason = "server_disconnected";
            }


            if (!hasConnected) {
                JoinWhiteHovered = JoinWhiteRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                JoinBlackHovered = JoinBlackRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                IpRectHovered = IPRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                PasswordRectHovered = PasswordRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));
                botRectHovered = BotClassRect.Contains(Vector2.Transform(Mouse.GetState().Position.ToVector2(), Matrix.Invert(WindowCentreMatrix)));

                if (Mouse.GetState().LeftButton == ButtonState.Pressed && MouseWasntPressed && !isConnecting) {
                    if (!IpRectHovered) {
                        UnregisterIpInput();
                    }
                    if (!PasswordRectHovered) {
                        UnregisterPasswordInput();
                    }
                    if (!botRectHovered) {
                        UnregisterBotClassInput();
                    }

                    if (JoinWhiteHovered) {
                        colour = GameColour.WHITE;
                        Connect();
                    } else if (JoinBlackHovered) {
                        colour = GameColour.BLACK;
                        Connect();
                    } else if (IpRectHovered) {
                        RegisterIpInput();
                    } else if (PasswordRectHovered) {
                        RegisterPasswordInput();
                    } else if (botRectHovered) {
                        RegisterBotClassInput();
                    }
                }
            } else {
                if (colour == client.personalState.activePlayer && !botInterface.IsThinking()) {
                    botInterface.StartThink();
                }
            }

            MouseWasntPressed = !(Mouse.GetState().LeftButton == ButtonState.Pressed);


            base.Update(gameTime);
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

                GraphicsDevice.Clear(new Color(64,64,64));

                _spriteBatch.Begin();

                monospace_font = clockFontSystem.GetFont(24);

                _spriteBatch.DrawString(monospace_font, botClassStr + "\n\n" + botInterface.GetConsoleText(), new Vector2(10, 10), Color.White);

                _spriteBatch.End();

                base.Draw(gameTime);
            } else {

                GraphicsDevice.Clear(new Color(64, 64, 64));

                _spriteBatch.Begin(
                    transformMatrix: WindowCentreMatrix
                );

                monospace_font = clockFontSystem.GetFont(24);

                _spriteBatch.Draw(sq, BotClassRect, botRectHovered ? new Color(48, 48, 48) : new Color(32, 32, 32));
                string botDispStr = botClassStr + (BotFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? (botFieldFull ? "|" : "_") : "");
                bool bdsIsEmpty = botDispStr.Length == 0 && !BotFieldOpen;
                if (bdsIsEmpty) {
                    botDispStr = "ChessBot";
                }
                _spriteBatch.DrawString(monospace_font, botDispStr, new Vector2(BotClassRect.Left, BotClassRect.Center.Y) + new Vector2(9, -13f), bdsIsEmpty ? Color.Gray : Color.White);


                _spriteBatch.Draw(sq, IPRect, IpRectHovered ? new Color(48, 48, 48) : new Color(32, 32, 32));
                string ipDispStr = ipstr + (IpFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? (ipFieldFull ? "|" : "_") : "");
                bool idsIsEmpty = ipDispStr.Length == 0 && !IpFieldOpen;
                if (idsIsEmpty) {
                    ipDispStr = "127.0.0.1";
                }
                _spriteBatch.DrawString(monospace_font, ipDispStr, new Vector2(IPRect.Left, IPRect.Center.Y) + new Vector2(9, -13f), idsIsEmpty ? Color.Gray : Color.White);


                _spriteBatch.Draw(sq, PasswordRect, PasswordRectHovered ? new Color(48, 48, 48) : new Color(32, 32, 32));
                string hiddenPassword = new('*', password.Length);
                string pwDispStr = hiddenPassword + (PasswordFieldOpen && ((int)(gameTime.TotalGameTime.TotalSeconds * 2) % 2 == 0) ? (passwordFieldFull ? "|" : "_") : "");
                bool pdsIsEmpty = pwDispStr.Length == 0 && !PasswordFieldOpen;
                if (pdsIsEmpty) {
                    pwDispStr = "Password";
                }
                _spriteBatch.DrawString(monospace_font, pwDispStr, new Vector2(PasswordRect.Left, PasswordRect.Center.Y) + new Vector2(9, -13f), pdsIsEmpty ? Color.Gray : Color.White);


                SpriteFontBase text_font = fontSystem.GetFont(42);


                _spriteBatch.Draw(sq, JoinWhiteRect, JoinWhiteHovered ? TIME_COLOUR_LIGHT : Color.AntiqueWhite);
                string WHITE_JOIN_STR = TextLocalizer.Get("join_as_white");
                bool show_throbber_white = isConnecting && colour.isWhite();

                if (show_throbber_white) {
                    DrawThrobber(_spriteBatch, circle, JoinWhiteRect.Center.ToVector2() - new Vector2(3.75f, 3.75f), Color.Black, (int)(gameTime.TotalGameTime.TotalSeconds * 8));
                } else {
                    Vector2 white_join_bounds = text_font.MeasureString(WHITE_JOIN_STR);
                    _spriteBatch.DrawString(text_font, WHITE_JOIN_STR, JoinWhiteRect.Center.ToVector2() + white_join_bounds / -2 + new Vector2(0, -2.5f), Color.Black);
                }



                _spriteBatch.Draw(sq, JoinBlackRect, JoinBlackHovered ? TIME_COLOUR_DARK : new Color(79, 58, 47));
                string BLACK_JOIN_STR = TextLocalizer.Get("join_as_black");
                bool show_throbber_black = isConnecting && colour.isBlack();

                if (show_throbber_black) {
                    DrawThrobber(_spriteBatch, circle, JoinBlackRect.Center.ToVector2() - new Vector2(3.75f, 3.75f), Color.White, (int)(gameTime.TotalGameTime.TotalSeconds * 8));
                } else {
                    Vector2 black_join_bounds = text_font.MeasureString(BLACK_JOIN_STR);
                    _spriteBatch.DrawString(text_font, BLACK_JOIN_STR, JoinBlackRect.Center.ToVector2() + black_join_bounds / -2 + new Vector2(0, -2.5f), Color.White);
                }

                _spriteBatch.End();


                if (infoText != "") {
                    // Draw lower-left info text

                    _spriteBatch.Begin(
                        transformMatrix: Matrix.CreateTranslation(new Vector3(0, Window.ClientBounds.Height, 0)),
                        samplerState: SamplerState.PointClamp,
                        blendState: null
                    );

                    Vector2 textBounds = text_font.MeasureString(infoText, lineSpacing: -10);
                    _spriteBatch.DrawString(text_font, infoText, new Vector2(10, -10 - textBounds.Y), Color.Black, lineSpacing: -10);

                    _spriteBatch.End();

                }



                base.Draw(gameTime);
            }
        }

        public bool isConnecting = false;

        private async void Connect() {
            Type botType = typeof(BotInterface<>).Assembly.GetType("ChessBot." + botClassStr);


            if (botType == null) {
                infoText = TextLocalizer.Get("invalid_bot_class") + '"' + botClassStr + '"';
            } else {
                Type botInterfaceType = typeof(BotInterface<>).MakeGenericType(botType);
                botInterface = (BotInterface)Activator.CreateInstance(botInterfaceType, client, colour);

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
        }


        public ColourRights GetRights() {
            return colour.GetRights();
        }

        public void MakeMove(Move move) {
            client.personalState.MakeMoveValidated(move, GetRights());
            client.SendCommand(new ChessCommand(CommandType.MOVE, colour, move: move));
        }

        public void SubmitMoves() {
            if (client.personalState.CanSubmitMoves(GetRights())) {
                client.personalState.SubmitMoves();
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

        public void OnInputBotField(object sender, TextInputEventArgs e) {
            Keys key = e.Key;
            char c = e.Character;

            if (key == Keys.Enter) {
                UnregisterBotClassInput();
            } else if (key == Keys.Delete || key == Keys.Back) {
                if (botClassStr.Length > 0) {
                    botClassStr = botClassStr.Substring(0, botClassStr.Length - 1);
                }
                botFieldFull = false;
            } else {
                botClassStr += c;
                if (monospace_font.MeasureString(botClassStr).X + 10 > BotClassRect.Width) {
                    botClassStr = botClassStr.Substring(0, botClassStr.Length - 1);
                    botFieldFull = true;
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

        public void RegisterBotClassInput() {
            if (!BotFieldOpen) {
                BotFieldOpen = true;
                Window.TextInput += OnInputBotField;
            }
        }

        public void UnregisterBotClassInput() {
            if (BotFieldOpen) {
                BotFieldOpen = false;
                Window.TextInput -= OnInputBotField;
            }
        }
    }
}
