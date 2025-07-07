using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public class Board {

        public static readonly string STARTING_FEN = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

        public Vector2iTL TL;

        public Vector2iTL parentTL;

        public Piece[,] pieces = new Piece[8,8];

        public CastleRights castleRights;

        public Vector2i epTarget;

        public Vector2i moveFrom = null, moveTo = null, moveTravel = null;

        public Board(Vector2iTL TL = null, string fen = null) {
            if (TL is null) TL = Vector2iTL.ORIGIN_WHITE;
            if (fen is null) fen = STARTING_FEN;

            this.TL = TL;
            parentTL = null;

            LoadFen(fen);
        }

        public Board(Board source, Piece piece, Vector2i from, Vector2i to) {
            init(source.TL.Y, source, piece, to, from);
        }

        public Board(int l, Board source, Piece piece, Vector2i to) {
            init(l, source, piece, to, null);
        }

        private void init(int l, Board source, Piece piece, Vector2i to, Vector2i from) {
            parentTL = source.TL;
            TL = new Vector2iTL(source.TL.X, l, source.TL.colour).NextTurn();

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
                // Handle en passant
                if (to == epTarget && ((piece & Piece.MASK_KIND) == Piece.PIECE_PAWN)) {
                    // holy hell
                    int offset = (int)piece.getColour();
                    // new response just dropped
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
                TL.colour = GameColour.WHITE;
            } else if (things[1] == "b") {
                TL.colour = GameColour.BLACK;
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
            Array.Copy(o.pieces, pieces, 64);
        }
        public Board Clone() {
            return new Board(this);
        }
    }
}
