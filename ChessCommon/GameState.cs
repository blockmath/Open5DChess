using ChessCommon;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChessCommon {
    public class GameState {

        public List<MoveSpec> allowedPromotions = new List<MoveSpec> {
            MoveSpec.PromoteKnight,
            MoveSpec.PromoteRook,
            MoveSpec.PromoteBishop,
            MoveSpec.PromoteUnicorn,
            MoveSpec.PromoteDragon,
            MoveSpec.PromotePrincess,
            MoveSpec.PromoteQueen
        };

        public static readonly string STANDARD_PGN = 
            "[Mode \"5D\"]\n" +
            "[Size \"8x8\"]\n" +
            "[Variant \"Standard\"]\n" +
            "[rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR:0:1:w]";

        public static readonly string TEST_PGN =
            "[Mode \"5D\"]\n" +
            "[Size \"8x8\"]\n" +
            "[Variant \"Standard\"]\n" +
            "[rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR:0:1:w]\n" +
            "1.(0T1)e2e4 / (0T1)e7e5\n" +
            "2.(0T2)Ng1>>(0T1)g3 / (1T1)Ng8>(0T1)g6 (0T2)f7f6\n" +
            "3.(0T3)Qd1>>(0T1)f3 / (0T3)Bf8b4 (2T1)Ng8h6\n" +
            "4.(0T4)f2f4 / (0T4)Bb4d2\n" +
            "5.(0T5)Bc1d2 / (0T5)Ng8h6\n" +
            "6.(0T6)Bf1>>(0T1)f6 (1T2)Ng3>(0T4)g3 / (1T2)c7c5";

        public GameState() {
            LoadPgn(TEST_PGN);
        }

        public GameState(GameState o) {
            boards = o.boards.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
            minTL = o.minTL;
            maxTL = o.maxTL;
            moveStack = new Stack<IMove>(o.moveStack.ToArray());
        }

        public void LoadPgn(string pgn) {
            List<string> things = new List<string>(pgn.Split('\n'));

            int sp = 0;

            int mp = 0;

            Dictionary<Vector2iTL, string> fen = new Dictionary<Vector2iTL, string>();

            while (sp < things.Count) {
                string line = things[sp++];

                if (line[0] == '[') {
                    List<string> slin = new List<string>(line.Substring(1, line.Length - 2).Split(' '));

                    switch (slin[0]) {
                        case "Mode":
                            if (slin[1] != "\"5D\"") {
                                throw new NotSupportedException("Non-5D modes are not supported");
                            }
                            break;
                        case "Result":
                        case "Date":
                        case "Time":
                        case "White":
                            break;
                        case "Board":
                        case "Variant":
                            // These are in a different case because I might want to use them someday whereas the others are completely useless
                            break;
                        case "Size":
                            if (slin[1] != "\"8x8\"") {
                                throw new NotSupportedException("Non-8x8 sizes are not supported");
                            }
                            break;
                        default: // Assume it's a FEN string for a single-board setup
                            List<string> lfen = new List<string>(slin[0].Replace("*", "").Split(':'));
                            fen.Add(
                                // Not sure what order the turn and timeline are in lmao
                                new Vector2iTL(int.Parse(lfen[2]), int.Parse(lfen[1]), lfen[3] == "w" ? GameColour.WHITE : GameColour.BLACK),
                                lfen[0] + " " + lfen[3] + " KQkq - 0 1"
                            );
                            break;
                    }
                } else {

                    if (mp == 0) {
                        // Metadata is done, setup initial state
                        moveStack = new Stack<IMove>();
                        boards = new Dictionary<Vector2iTL, Board>();
                        foreach (KeyValuePair<Vector2iTL, string> kv in fen) {
                            boards.Add(kv.Key, new Board(kv.Key, kv.Value));
                        }
                    }

                    List<string> lsp = new List<string>(line.Split(new char[] { '.' }, 2));
                    int newMp = int.Parse(lsp[0]);

                    if (lsp.Count <= 1 || lsp[1] == "") {
                        break;
                    }

                    string wmov, bmov = "/";

                    wmov = Regex.Replace(lsp[1].Split('/')[0], "{.*?}", "").Trim();
                    if (lsp[1].Split('/').Count() > 1) {
                        bmov = Regex.Replace(lsp[1].Split('/')[1], "{.*?}", "").Trim();
                    }

                    if (newMp != ++mp) {
                        throw new FormatException("Moves out of order (expected '" + mp.ToString() + ".', got '" + newMp.ToString() + ".')");
                    } else {
                        foreach (string mov in wmov.Split(' ')) {
                            MakeMoveStr(mov, GameColour.WHITE);
                        }

                        if (bmov != "/") {
                            foreach (string mov in bmov.Split(' ')) {
                                MakeMoveStr(mov, GameColour.BLACK);
                            }
                        }
                    }
                }
            }
        }

        public void MakeMoveStr(string move, GameColour colour) {
            Match mstrmatch = Regex.Match(move, "^[^()]*\\((.*?)\\)([A-Z]?[a-h][1-8])(.*)$");

            string tlstr = mstrmatch.Groups[1].Value;
            string xystr = mstrmatch.Groups[2].Value;

            string mstr2 = mstrmatch.Groups[3].Value;

            string tlstr2;
            string xystr2;

            Vector2iTL TL = new Vector2iTL(tlstr, colour);
            Vector2i XY = new Vector2i(xystr);

            if (mstr2[0] == '>') {
                Match mstrmatch2 = Regex.Match(mstr2, "^>+\\((.*?)\\)([A-Z]?[a-h][1-8])$");

                tlstr2 = mstrmatch2.Groups[1].Value;
                xystr2 = mstrmatch2.Groups[2].Value;
            } else {
                tlstr2 = tlstr;
                xystr2 = mstr2;
            }

            Vector2iTL TL2 = new Vector2iTL(tlstr2, colour);
            Vector2i XY2 = new Vector2i(xystr2);

            MakeMove(new Move(new Vector4iTL(XY, TL), new Vector4iTL(XY2, TL2)));
        }

        public GameState Clone() {
            return new GameState(this);
        }

        public Dictionary<Vector2iTL, Board> boards;
        public GameColour activePlayer;

        public List<Board> GetMoveableBoards() {
            List<Board> mb = new List<Board>();

            foreach (Board board in boards.Values) {
                if (BoardIsPlayable(board.TL)) {
                    mb.Add(board);
                }
            }

            return mb;
        }


        private int minTL = 0;
        private int maxTL = 0;

        private int minATL => (-maxTL) - 1;
        private int maxATL => (-minTL) + 1;

        private Stack<IMove> moveStack;
        public List<IMove> GetMoves() {
            List<IMove> ms = moveStack.ToList();
            ms.Reverse();
            return ms;
        }
        public bool TimelineIsActive(int l) {
            return minATL <= l && l <= maxATL;
        }

        public bool BoardExists(Vector2iTL TL) {
            return boards.ContainsKey(TL);
        }

        public bool BoardIsPlayable(Vector2iTL TL) {
            return !boards.ContainsKey(TL.NextTurn());
        }

        public int GetPresentPly() {
            List<Board> mb = GetMoveableBoards();

            int minTurn = int.MaxValue;

            foreach (Board board in mb) {
                if (Methods.TVis(board.TL) < minTurn && TimelineIsActive(board.TL.Y)) {
                    minTurn = Methods.TVis(board.TL);
                }
            }

            return minTurn;
        }

        public GameColour GetPresentColour() {
            if (GetPresentPly() % 2 == 0) {
                return GameColour.WHITE;
            } else {
                return GameColour.BLACK;
            }
        }

        public int GetPresentTurn() {
            return GetPresentPly() / 2;
        }

        public Piece GetPiece(Vector4iTL pos) {
            return GetBoard(pos.TL).GetPiece(pos.XY);
        }

        // MIKE, the BOARD, please
        public Board GetBoard(Vector2iTL TL) {
            if (TL is null) return null;

            return boards[TL];
        }












        private static List<Vector4i> rookSteps = new List<Vector4i> {
            new Vector4i(1,0,0,0), new Vector4i(-1,0,0,0), new Vector4i(0,1,0,0), new Vector4i(0,-1,0,0), new Vector4i(0,0,1,0), new Vector4i(0,0,-1,0), new Vector4i(0,0,0,1), new Vector4i(0,0,0,-1)
        };
        private static List<Vector4i> bishopSteps = new List<Vector4i>();
        private static List<Vector4i> unicornSteps = new List<Vector4i>();
        private static List<Vector4i> dragonSteps = new List<Vector4i>();
        private static List<Vector4i> knightSteps = new List<Vector4i>();

        private static void SetupStepTables() {
            foreach (Vector4i sa in rookSteps)
                foreach (Vector4i sb in rookSteps) {
                    if (sa.abs() == sb.abs()) {
                        continue;
                    }
                    Vector4i s = sa + sb;
                    if (s.isUnit() && !bishopSteps.Contains(s)) {
                        bishopSteps.Add(s);
                    }
                }

            foreach (Vector4i sa in rookSteps)
                foreach (Vector4i sb in rookSteps)
                    foreach (Vector4i sc in rookSteps) {
                        if (sa.abs() == sb.abs() || sb.abs() == sc.abs() || sc.abs() == sa.abs()) {
                            continue;
                        }
                        Vector4i s = sa + sb + sc;
                        if (s.isUnit() && !unicornSteps.Contains(s)) {
                            unicornSteps.Add(s);
                        }
                    }

            foreach (Vector4i sa in rookSteps)
                foreach (Vector4i sb in rookSteps)
                    foreach (Vector4i sc in rookSteps)
                        foreach (Vector4i sd in rookSteps) {
                            if (sa.abs() == sb.abs() || sb.abs() == sc.abs() || sc.abs() == sd.abs() || sd.abs() == sa.abs() || sa.abs() == sc.abs() || sb.abs() == sd.abs()) {
                                continue;
                            }
                            Vector4i s = sa + sb + sc + sd;
                            if (s.isUnit() && !dragonSteps.Contains(s)) {
                                dragonSteps.Add(s);
                            }
                        }

            foreach (Vector4i sa in rookSteps)
                foreach (Vector4i sb in rookSteps) {
                    Vector4i s = (sa * 2) + sb;
                    if (!s.isOrthogonal() && !knightSteps.Contains(s)) {
                        knightSteps.Add(s);
                    }
                }
        }

        static GameState() {
            SetupStepTables();
        }





        public List<Move> GetSlidingMoves(Vector4iTL pos, Piece p = Piece.NONE) {
            Piece piece = ((p == Piece.NONE) ? GetPiece(pos) : p);
            List<Move> moves = new List<Move>();

            if ((piece & Piece.MOVABL_ROOK) != 0) {
                foreach (Vector4i s in rookSteps) {
                    for (int i = 1;; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == piece.getColour()) {
                            break;
                        } else {
                            moves.Add(new Move(pos, spCoord));
                        }
                        if (GetPiece(spCoord).getColour() != GameColour.NONE) {
                            break;
                        }
                    }
                }
            }

            if ((piece & Piece.MOVABL_BISHOP) != 0) {
                foreach (Vector4i s in bishopSteps) {
                    for (int i = 1; ; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == piece.getColour()) {
                            break;
                        } else {
                            moves.Add(new Move(pos, spCoord));
                        }
                        if (GetPiece(spCoord).getColour() != GameColour.NONE) {
                            break;
                        }
                    }
                }
            }

            if ((piece & Piece.MOVABL_UNICORN) != 0) {
                foreach (Vector4i s in unicornSteps) {
                    for (int i = 1; ; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == piece.getColour()) {
                            break;
                        } else {
                            moves.Add(new Move(pos, spCoord));
                        }
                        if (GetPiece(spCoord).getColour() != GameColour.NONE) {
                            break;
                        }
                    }
                }
            }

            if ((piece & Piece.MOVABL_DRAGON) != 0) {
                foreach (Vector4i s in dragonSteps) {
                    for (int i = 1; ; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == piece.getColour()) {
                            break;
                        } else {
                            moves.Add(new Move(pos, spCoord));
                        }
                        if (GetPiece(spCoord).getColour() != GameColour.NONE) {
                            break;
                        }
                    }
                }
            }

            return moves;
        }


        // TODO: Implement castling
        // This will also have to be implemented in MakeMove()
        public List<Move> GetKingMoves(Vector4iTL pos, Piece p = Piece.NONE) {
            Piece piece = ((p == Piece.NONE) ? GetPiece(pos) : p);
            List<Move> moves = new List<Move>();

            for (int x = -1; x <= 1; ++x) {
                for (int y = -1; y <= 1; ++y) {
                    for (int t = -1; t <= 1; ++t) {
                        for (int l = -1; l <= 1; ++l) {
                            Vector4i sp = new Vector4i(x, y, t, l);
                            Vector4iTL spCoord = pos + sp;
                            if (sp == Vector4i.ZERO || !IsInBoard(spCoord)) {
                                continue;
                            } else if (GetPiece(spCoord).getColour() == piece.getColour()) {
                                continue;
                            } else {
                                moves.Add(new Move(pos, spCoord));
                            }
                        }
                    }
                }
            }


            if ((GetBoard(pos.TL).castleRights & CastleRights.WK) != 0) {
                Vector4iTL tgt = new Vector4iTL(new Vector2i(7, 8), pos.TL);
                if (p.getColour() == GameColour.WHITE && GetPiece(tgt + new Vector4i(-1, 0, 0, 0)) == Piece.NONE && GetPiece(tgt) == Piece.NONE && (GetPiece(tgt + new Vector4i(1, 0, 0, 0)) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                    moves.Add(new Move(pos, tgt, MoveSpec.CastlesWK));
                }
            }

            if ((GetBoard(pos.TL).castleRights & CastleRights.WQ) != 0) {
                Vector4iTL tgt = new Vector4iTL(new Vector2i(3, 8), pos.TL);
                if (p.getColour() == GameColour.WHITE && GetPiece(tgt + new Vector4i(1, 0, 0, 0)) == Piece.NONE && GetPiece(tgt) == Piece.NONE && GetPiece(tgt + new Vector4i(-1, 0, 0, 0)) == Piece.NONE && (GetPiece(tgt + new Vector4i(-2, 0, 0, 0)) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                    moves.Add(new Move(pos, tgt, MoveSpec.CastlesWQ));
                }
            }

            if ((GetBoard(pos.TL).castleRights & CastleRights.BK) != 0) {
                Vector4iTL tgt = new Vector4iTL(new Vector2i(7, 1), pos.TL);
                if (p.getColour() == GameColour.BLACK && GetPiece(tgt + new Vector4i(-1, 0, 0, 0)) == Piece.NONE && GetPiece(tgt) == Piece.NONE && (GetPiece(tgt + new Vector4i(1, 0, 0, 0)) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                    moves.Add(new Move(pos, tgt, MoveSpec.CastlesBK));
                }
            }

            if ((GetBoard(pos.TL).castleRights & CastleRights.BQ) != 0) {
                Vector4iTL tgt = new Vector4iTL(new Vector2i(3, 1), pos.TL);
                if (p.getColour() == GameColour.BLACK && GetPiece(tgt + new Vector4i(1, 0, 0, 0)) == Piece.NONE && GetPiece(tgt) == Piece.NONE && GetPiece(tgt + new Vector4i(-1, 0, 0, 0)) == Piece.NONE && (GetPiece(tgt + new Vector4i(-2, 0, 0, 0)) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                    moves.Add(new Move(pos, tgt, MoveSpec.CastlesBQ));
                }
            }


            return moves;
        }

        public List<Move> GetPawnMoves(Vector4iTL pos, Piece p = Piece.NONE) {
            Piece piece = ((p == Piece.NONE) ? GetPiece(pos) : p);
            List<Move> moves = new List<Move>();

            int offset = -(int)piece.getColour();

            // I'm **pretty** sure this is correct to check that a pawn is on *its* second rank
            bool isFirstStep = (pos.Y == 8 + offset) || (pos.Y == 0 + offset);

            Vector4iTL tgt;
            Vector4i off;

            off = new Vector4i(0, offset, 0, 0);
            tgt = pos + off;
            if (IsInBoard(tgt) && GetPiece(tgt).getColour() == GameColour.NONE) {
                moves.Add(new Move(pos, tgt));
                if (isFirstStep) {
                    tgt += off;
                    if (IsInBoard(tgt) && GetPiece(tgt).getColour() == GameColour.NONE) {
                        moves.Add(new Move(pos, tgt, MoveSpec.DoublePush));
                    }
                }
            }

            off = new Vector4i(0, 0, 0, offset);
            tgt = pos + off;
            if (IsInBoard(tgt) && GetPiece(tgt).getColour() == GameColour.NONE) {
                moves.Add(new Move(pos, tgt));
                if (isFirstStep) {
                    tgt += off;
                    if (IsInBoard(tgt) && GetPiece(tgt).getColour() == GameColour.NONE) {
                        moves.Add(new Move(pos, tgt, MoveSpec.DoublePush));
                    }
                }
            }


            off = new Vector4i(1, offset, 0, 0);
            tgt = pos + off;
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }

            off = new Vector4i(-1, offset, 0, 0);
            tgt = pos + off;
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }


            off = new Vector4i(0, 0, 1, offset);
            tgt = pos + off;
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }

            off = new Vector4i(0, 0, -1, offset);
            tgt = pos + off;
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }


            if ((piece & Piece.MASK_KIND) == Piece.PIECE_BRAWN) {
                off = new Vector4i(1, 0, 0, offset);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }

                off = new Vector4i(-1, 0, 0, offset);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }


                off = new Vector4i(0, offset, 1, 0);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }

                off = new Vector4i(0, offset, -1, 0);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == (GameColour)(-(int)piece.getColour()) || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }
            }

            return moves;
        }


        public List<Move> GetKnightMoves(Vector4iTL pos, Piece p = Piece.NONE) {
            Piece piece = ((p == Piece.NONE) ? GetPiece(pos) : p);
            List<Move> moves = new List<Move>();

            foreach (Vector4i s in knightSteps) {
                Vector4iTL spCoord = pos + s;
                if (IsInBoard(spCoord) && GetPiece(spCoord).getColour() != piece.getColour()) {
                    moves.Add(new Move(pos, spCoord));
                }
            }

            return moves;
        }



        public List<Move> GetPseudoLegalMoves(Vector4iTL pos, Piece p = Piece.NONE) {
            Piece piece = ((p == Piece.NONE) ? GetPiece(pos) : p);
            List<Move> moves = new List<Move>();

            if ((piece & Piece.MASK_SPEC) == 0) {
                moves.AddRange(GetSlidingMoves(pos, piece));
            } else {
                if ((piece & Piece.MASK_MOVABL) == Piece.MOVABL_SPEC_KING) {
                    moves.AddRange(GetKingMoves(pos, piece));
                }
                if ((piece & Piece.MASK_MOVABL) == Piece.MOVABL_SPEC_PAWN || (piece & Piece.MASK_MOVABL) == Piece.MOVABL_SPEC_BRAWN) {
                    moves.AddRange(GetPawnMoves(pos, piece));
                }
                if ((piece & Piece.MASK_MOVABL) == Piece.MOVABL_SPEC_KNIGHT) {
                    moves.AddRange(GetKnightMoves(pos, piece));
                }
            }

            return moves;
        }






        public bool IsInBoard(Vector4iTL pos) {
            return (1 <= pos.X && pos.X <= 8 && 1 <= pos.Y && pos.Y <= 8 && BoardExists(pos.TL));
        }


        public void MakeMove(Move move) {
            if (!BoardExists(move.origin.TL)) {
                throw new Exception("Error: origin board does not exist");
            }

            if (!BoardExists(move.target.TL)) {
                throw new Exception("Error: target board does not exist");
            }

            if (!BoardIsPlayable(move.origin.TL)) {
                throw new Exception("Error: Attempting to make a move on a frozen board");
            }

            CastleRights lostCastleRightsTarget = CastleRights.NONE;

            if ((GetPiece(move.target) & Piece.MASK_KIND) == Piece.KIND_KING) {
                switch (GetPiece(move.target).getColour()) {
                    case GameColour.WHITE:
                        if (move.target.XY == new Vector2i(5, 8)) {
                            lostCastleRightsTarget |= (CastleRights.WK | CastleRights.WQ);
                        }
                        break;
                    case GameColour.BLACK:
                        if (move.target.XY == new Vector2i(5, 1)) {
                            lostCastleRightsTarget |= (CastleRights.BK | CastleRights.BQ);
                        }
                        break;
                }
            }

            if ((GetPiece(move.target) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                Vector2i p = move.target.XY;
                if (p == new Vector2i(8, 8)) {
                    lostCastleRightsTarget |= CastleRights.WK;
                } else if (p == new Vector2i(1, 8)) {
                    lostCastleRightsTarget |= CastleRights.WQ;
                } else if (p == new Vector2i(8, 1)) {
                    lostCastleRightsTarget |= CastleRights.BK;
                } else if (p == new Vector2i(1, 1)) {
                    lostCastleRightsTarget |= CastleRights.BQ;
                }
            }

            Board newFromBoard, newToBoard;

            if (move.origin.TL == move.target.TL) {
                // Standard move
                Board moveBoard = GetBoard(move.origin.TL);

                IMove imove = new IMove(move);

                Piece movePiece = moveBoard.GetPiece(move.origin.XY);

                newFromBoard = newToBoard = new Board(moveBoard, movePiece, move.origin.XY, move.target.XY);

                imove.target_child = newToBoard.TL;
                imove.origin_child = newFromBoard.TL;
                imove.capture_target = move.target.XY;
                if (imove.capture_target == moveBoard.epTarget) {
                    int offset = (int)movePiece.getColour();
                    imove.capture_target.Y += offset;
                }
                imove.captured = moveBoard.GetPiece(imove.capture_target);

                // Only highlight if a move was actually made, don't highlight on ForceSkipTurn()
                if (move.origin.XY != move.target.XY) {
                    newFromBoard.moveFrom = move.origin.XY;
                    newFromBoard.moveTo = move.target.XY;
                }

                boards.Add(newFromBoard.TL, newFromBoard);
                moveStack.Push(imove);
            } else if (BoardIsPlayable(move.target.TL)) {
                // Move to active board (no timeline split)

                Board fromBoard = GetBoard(move.origin.TL);
                Board toBoard = GetBoard(move.target.TL);

                IMove imove = new IMove(move);

                Piece movePiece = fromBoard.GetPiece(move.origin.XY);

                newFromBoard = new Board(fromBoard, movePiece, move.origin.XY, null);
                newToBoard = new Board(toBoard.TL.Y, toBoard, movePiece, move.target.XY);

                imove.target_child = newToBoard.TL;
                imove.origin_child = newFromBoard.TL;
                imove.capture_target = move.target.XY;
                if (imove.capture_target == toBoard.epTarget) {
                    int offset = (int)movePiece.getColour();
                    imove.capture_target.Y += offset;
                }
                imove.captured = toBoard.GetPiece(imove.capture_target);

                newFromBoard.moveTravel = move.origin.XY;
                newToBoard.moveTravel = move.target.XY;

                boards.Add(newFromBoard.TL, newFromBoard);
                boards.Add(newToBoard.TL, newToBoard);
                moveStack.Push(imove);
            } else {
                // Move to frozen board (timeline split)

                Board fromBoard = GetBoard(move.origin.TL);
                Board toBoard = GetBoard(move.target.TL);

                IMove imove = new IMove(move);

                Piece movePiece = fromBoard.GetPiece(move.origin.XY);

                newFromBoard = new Board(fromBoard, movePiece, move.origin.XY, null);

                int newL = 0;
                if (move.getColour() == GameColour.WHITE) {
                    newL = ++maxTL;
                } else {
                    newL = --minTL;
                }

                newToBoard = new Board(newL, toBoard, movePiece, move.target.XY);

                imove.target_child = newToBoard.TL;
                imove.origin_child = newFromBoard.TL;
                imove.capture_target = move.target.XY;
                if (imove.capture_target == toBoard.epTarget) {
                    int offset = (int)movePiece.getColour();
                    imove.capture_target.Y += offset;
                }
                imove.captured = toBoard.GetPiece(imove.capture_target);

                newFromBoard.moveTravel = move.origin.XY;
                newToBoard.moveTravel = move.target.XY;

                boards.Add(newFromBoard.TL, newFromBoard);
                boards.Add(newToBoard.TL, newToBoard);
                moveStack.Push(imove);
            }

            if ((GetPiece(move.origin) & Piece.MASK_KIND) == Piece.KIND_KING) {
                switch (GetPiece(move.origin).getColour()) {
                    case GameColour.WHITE:
                        if (move.origin.XY == new Vector2i(5, 8)) {
                            newFromBoard.castleRights &=~ (CastleRights.WK | CastleRights.WQ);
                        }
                        break;
                    case GameColour.BLACK:
                        if (move.origin.XY == new Vector2i(5, 1)) {
                            newFromBoard.castleRights &=~ (CastleRights.BK | CastleRights.BQ);
                        }
                        break;
                }
            }

            if ((GetPiece(move.origin) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                Vector2i p = move.origin.XY;
                if (p == new Vector2i(8, 8)) {
                    newFromBoard.castleRights &=~ CastleRights.WK;
                } else if (p == new Vector2i(1, 8)) {
                    newFromBoard.castleRights &=~ CastleRights.WQ;
                } else if (p == new Vector2i(8, 1)) {
                    newFromBoard.castleRights &=~ CastleRights.BK;
                } else if (p == new Vector2i(1, 1)) {
                    newFromBoard.castleRights &=~ CastleRights.BQ;
                }
            }

            newToBoard.castleRights &=~ lostCastleRightsTarget;

        }

        public void UnmakeMove(Move move = null) {
            if (!(move is null) && move != moveStack.First()) {
                throw new Exception("Error: Attempting to unmake a move that's not at the top of the stack");
            }

            IMove imove = moveStack.Pop();

            if (imove.target_child.L != imove.target.L) {
                if (imove.getColour() == GameColour.WHITE) {
                    --maxTL;
                } else {
                    ++minTL;
                }
            }


            boards.Remove(imove.origin_child);
            if (boards.ContainsKey(imove.target_child)) {
                boards.Remove(imove.target_child);
            }
        }
    }
}
