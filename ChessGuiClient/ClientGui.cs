using ChessCommon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Net;

namespace ChessGuiClient {
    public class ClientGui : ChessGui.ChessGui {

        public GameColour colour = GameColour.BLACK;

        public ChessClient.ChessClient client;

        public IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 1500);

        private bool hasConnected = false;

        protected override void Initialize() {
            base.Initialize();

            client = new ChessClient.ChessClient();

            Connect();
        }

        protected override void Update(GameTime gameTime) {
            renderer.perspective = colour;

            if (hasConnected) {
                base.Update(gameTime);
            } else {


                // TODO: UI buttons for ip, connect as black/white


                base.BaseUpdate(gameTime);
            }
        }

        protected override void Draw(GameTime gameTime) {
            if (hasConnected) {
                base.Draw(gameTime);
            } else {

                // TODO: see above

                base.BaseDraw(gameTime);
            }
        }

        private void Connect() {
            client.Connect(endPoint, colour);

            hasConnected = true;
        }


        public override ColourRights GetRights() {
            return colour.GetRights();
        }

        public override void GuiUndoMove() {
            client.SendCommand(new ChessCommand(CommandType.UNDO, colour));
        }

        public override void MakeMove(Move move) {
            client.SendCommand(new ChessCommand(CommandType.MOVE, colour, move));
        }

        public override void SubmitMoves() {
            client.SendCommand(new ChessCommand(CommandType.SUBMIT, colour));
        }

    }
}
