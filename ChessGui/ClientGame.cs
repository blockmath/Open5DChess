using ChessCommon;
using ChessBot;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using FontStashSharp;
using System.Diagnostics;
using System.IO;

namespace ChessGui
{
    public class ClientGame : ChessGui
    {

        public override ColourRights GetRights() {
            return ColourRights.BOTH;
        }

        public override void GuiUndoMove() {
            
        }

        public override void SubmitMoves() {
            
        }

        public override void MakeMove(Move move) {
            
        }
    }
}
