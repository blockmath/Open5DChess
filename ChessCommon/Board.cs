using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {


    public struct BoundsInfo {
        public Vector2i BoardSize;
        public Tuple<Vector2i, Vector2i> KingPos;

        public BoundsInfo(Vector2i boardSize, Tuple<Vector2i, Vector2i> kingPos) {
            this.BoardSize = boardSize;
            this.KingPos = kingPos;
        }
    }



    public class Board {

        public Vector2i BoardSize;
        public Tuple<Vector2i, Vector2i> KingPos;

        public Vector2iTL TL;

        public Vector2iTL parentTL;

        public Piece[,] pieces;

        public CastleRights castleRights;

        public Vector2i epTarget;

        public Vector2i moveFrom, moveTo, moveTravel;

        public ColourRights playerHasLost = ColourRights.NONE;

        public Board(BoundsInfo boundsInfo, Vector2iTL TL, string fen) {
            BoardSize = boundsInfo.BoardSize;
            KingPos = boundsInfo.KingPos;
            pieces = new Piece[BoardSize.X, BoardSize.Y];

            if (TL == Vector2iTL.Null) TL = Vector2iTL.ORIGIN_WHITE;

            this.TL = TL;
            parentTL = Vector2iTL.Null;

            LoadFen(fen);
        }

        public Board(Board source, Piece piece, Vector2i from, Vector2i to) {
            init(source.TL.Y, source, piece, to, from);
        }

        public Board(Board source, string fen) {
            BoardSize = source.BoardSize;
            KingPos = source.KingPos;
            pieces = new Piece[BoardSize.X, BoardSize.Y];

            LoadFen(fen);

            TL = source.TL.NextTurn();
            parentTL = source.TL;
        }

        public Board(int l, Board source, Piece piece, Vector2i to) {
            init(l, source, piece, to, Vector2i.ZERO);
        }

        private void init(int l, Board source, Piece piece, Vector2i to, Vector2i from) {
            BoardSize = source.BoardSize;
            KingPos = source.KingPos;
            pieces = new Piece[BoardSize.X, BoardSize.Y];

            parentTL = source.TL;
            TL = new Vector2iTL(source.TL.X, l, source.TL.colour).NextTurn();
            castleRights = source.castleRights;
            playerHasLost = source.playerHasLost;

            Array.Copy(source.pieces, pieces, BoardSize.X * BoardSize.Y);
            RemovePiece(from);
            PlacePiece(piece, to);
        }

        public void RemovePiece(Vector2i from) {
            if (from == Vector2i.ZERO) {
                return;
            } else {
                pieces[from.X - 1, from.Y - 1] = Piece.NONE;
            }
        }

        public void PlacePiece(Piece piece, Vector2i to) {
            if (to == Vector2i.ZERO) {
                return;
            } else {
                pieces[to.X - 1, to.Y - 1] = piece;
                // Handle en passant
                if (to == epTarget && ((piece & Piece.MASK_KIND) == Piece.PIECE_PAWN || (piece & Piece.MASK_KIND) == Piece.PIECE_BRAWN)) {
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
            int bpX = 0, bpY = 0;
            foreach (char c in things[0]) {
                if (c == '/') {
                    bpX = 0;
                    bpY++;
                } else if ('1' <= c && c <= '9') {
                    bpX += (c - '0');
                } else {
                    pieces[bpX++, bpY] = Methods.FromChar(c);
                }
            }

            /*if (things[1] == "w") {
                TL.colour = GameColour.WHITE;
            } else if (things[1] == "b") {
                TL.colour = GameColour.BLACK;
            }*/

            castleRights = CastleRights.NONE;
            if (things[2].Contains("K")) castleRights |= CastleRights.WK;
            if (things[2].Contains("Q")) castleRights |= CastleRights.WQ;
            if (things[2].Contains("k")) castleRights |= CastleRights.BK;
            if (things[2].Contains("q")) castleRights |= CastleRights.BQ;

            if (things[3] == "-") {
                epTarget = Vector2i.ZERO;
            } else {
                epTarget = new Vector2i(things[3][0] - ('a' - 1), things[3][1] - '0');
            }

            // We don't care about move clocks here
        }

        public Board(Board o) {
            TL = o.TL;
            Array.Copy(o.pieces, pieces, BoardSize.X * BoardSize.Y);
        }
        public Board Clone() {
            return new Board(this);
        }
    }
}
