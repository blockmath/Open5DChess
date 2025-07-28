using ChessCommon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Net;


namespace ChessGuiServer {
    public class ServerGui : ChessGui.ChessGui {

        public ChessServer.ChessServer server;

        public IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 1500);

        protected override void Initialize() {
            base.Initialize();

            server = new ChessServer.ChessServer();

            server.gameState = renderer.gameState;

            server.ServerStart(endPoint);
        }

        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);

            // Draw server UI afterwards (i.e. connections)
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

    }
}
