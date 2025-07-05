using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public class Board {

        public static readonly string STARTING_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public Vector2i TL;

        public Vector2i parentTLVis;

        public int pL;

        public GameColour turn;

        public Piece[,] pieces = new Piece[8,8];

        public CastleRights castleRights;

        public Vector2i epTarget;

        public Vector2i moveFrom = null, moveTo = null, moveTravel = null;

        public Vector2i TLVis => TLVisImpl(TL, turn);

        public static Vector2i TLVisImpl(Vector2i TL, GameColour turn) => new Vector2i(TL.X * 2 + (turn.isBlack() ? 1 : 0), TL.Y);


        public Board(Vector2i TL, GameColour colour) {
            this.TL = TL;
            turn = colour;
            pL = 0;
            parentTLVis = null;
        }

        public Board(Vector2i TL, string fen = null) {
            if (fen == null) fen = STARTING_FEN;

            this.TL = TL;
            pL = TL.Y;
            parentTLVis = null;

            LoadFen(fen);
        }

        public Board(Board source, Piece piece, Vector2i from, Vector2i to) {
            init(source.TL.Y, source, piece, to, from);
        }

        public Board(int l, Board source, Piece piece, Vector2i to) {
            init(l, source, piece, to, null);
        }

        private void init(int l, Board source, Piece piece, Vector2i to, Vector2i from) {
            parentTLVis = source.TLVis;
            TL = new Vector2i(source.TL.X, l);
            turn = (GameColour)(-(int)source.turn);
            if (turn.isWhite()) {
                TL += new Vector2i(1, 0);
            }
            pL = source.TL.Y;

            Array.Copy(source.pieces, pieces, 64);
            RemovePiece(from);
            PlacePiece(piece, to);
        }

        public void RemovePiece(Vector2i from) {
            if (from is null) {
                return;
            } else {
                pieces[from.X - 1, from.Y - 1] = Piece.NONE;
            }
        }

        public void PlacePiece(Piece piece, Vector2i to) {
            if (to is null) {
                return;
            } else {
                pieces[to.X - 1, to.Y - 1] = piece;
                if (to == epTarget) {
                    int offset = (int)piece.getColour();
                    pieces[to.X - 1, to.Y - 1 + offset] = Piece.NONE;
                }
            }
        }

        public Piece GetPiece(Vector2i from) {
            return pieces[from.X - 1, from.Y - 1];
        }

        public void LoadFen(string fen) {
            string[] things = fen.Split(new char[] { ' ' });
            Vector2i bp = new Vector2i(0, 0);
            foreach (char c in things[0]) {
                if (c == '/') {
                    bp.X = 0;
                    bp.Y++;
                } else if ('1' <= c && c <= '8') {
                    bp.X += (c - '0');
                } else {
                    pieces[bp.X++, bp.Y] = Methods.FromChar(c);
                }
            }

            if (things[1] == "w") {
                turn = GameColour.WHITE;
            } else if (things[1] == "b") {
                turn = GameColour.BLACK;
            }

            castleRights = CastleRights.NONE;
            if (things[2].Contains("K")) castleRights |= CastleRights.WK;
            if (things[2].Contains("Q")) castleRights |= CastleRights.WQ;
            if (things[2].Contains("k")) castleRights |= CastleRights.BK;
            if (things[2].Contains("q")) castleRights |= CastleRights.BQ;

            if (things[3] == "-") {
                epTarget = null;
            } else {
                epTarget = new Vector2i(things[3][0] - ('a' - 1), things[3][1] - '0');
            }

            // We don't care about clocks here
        }

        public Board(Board o) {
            TL = o.TL;
            turn = o.turn;
        }
        public Board Clone() {
            return new Board(this);
        }
    }
}
