using ChessCommon;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChessCommon {
    public class GameState {

        public static List<MoveSpec> allowedPromotions = new List<MoveSpec> {
            MoveSpec.PromoteKnight,
            MoveSpec.PromoteRook,
            MoveSpec.PromoteBishop,
            MoveSpec.PromoteUnicorn,
            MoveSpec.PromoteDragon,
            MoveSpec.PromotePrincess,
            MoveSpec.PromoteQueen
        };

        public static Dictionary<string, Tuple<string, string>> PgnDict;

        static GameState() {
            LoadPgnFiles();

            SetupStepTables();
        }

        private static void LoadPgnFiles() {
            PgnDict = new Dictionary<string, Tuple<string, string>>();

            string[] variantFileSearchPaths = File.ReadAllLines("variants.txt");

            List<string> variantFilePaths = new List<string>();

            foreach (string variantFileSearchPath in variantFileSearchPaths) {
                variantFilePaths.AddRange(Directory.GetFiles(".", variantFileSearchPath));
            }

            foreach (string variantFilePath in variantFilePaths) {
                if (variantFilePath.Trim() == "") continue;

                string variant = File.ReadAllText(variantFilePath);

                List<string> things = ParsePgnToList(variant);

                string name = "Unknown", id = "";

                int sp = 0;
                while (sp < things.Count && (name == "Unknown" || id == "")) {
                    string line = things[sp++];

                    if (line[0] == '[') {
                        List<string> slin = new List<string>(line.Substring(1, line.Length - 2).Split(' '));

                        switch (slin[0]) {
                            case "Variant":
                                name = slin[1].Trim('"');
                                break;
                            case "ID":
                                id = slin[1].Trim('"');
                                break;
                            default: break;
                        }
                    } else {
                        break;
                    }
                }

                PgnDict.Add(id, new Tuple<string, string>(name, variant));
            }
        }


        public GameState() : this("standard") { }

        public GameState(string pgn) {
            LoadPgn(pgn);
            activePlayer = GetPresentColour();
        }

        public GameState(GameState o) {
            boards = o.boards.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
            minTL = o.minTL;
            maxTL = o.maxTL;
            moveStack = new Stack<IMove>(o.moveStack.ToArray());
            activePlayer = o.activePlayer;
        }

        public Vector2i bi_min { get; private set; }
        public Vector2i bi_max { get; private set; }

        private static List<string> ParsePgnToList(string text) {
            List<string> presult = new List<string>(text.Split('\n'));
            List<string> result = new List<string>();

            for (int i = 0; i < presult.Count; ++i) {
                string ps = presult[i].Trim();
                if (ps != "") {
                    result.Add(ps);
                }
            }

            return result;
        }

        public static List<string> SplitUnquotedWhitespace(string text) {
            List<string> result = new List<string>();

            bool inQuotes = false;
            bool inEscape = false;

            string current = "";

            foreach (char c in text) {
                if (inEscape) {
                    current += c;
                } else if (c == '\\') {
                    inEscape = true;
                } else if (c == '"') {
                    inQuotes = !inQuotes;
                    current += c;
                } else if (char.IsWhiteSpace(c)) {
                    result.Add(current);
                    current = "";
                } else {
                    current += c;
                }
            }

            result.Add(current);


            return result;
        }

        public void LoadPgn(string id) {
            LoadPgnStr(PgnDict[id].Item2);
        }

        public void LoadPgnStr(string pgn) {
            initialPgn = pgn;

            bi_min = Vector2i.INFINITY;
            bi_max = Vector2i.INFINITY * -1;

            timer = null;

            List<string> things = ParsePgnToList(pgn);

            int sp = 0;

            int mp = 0;

            Dictionary<Vector2iTL, string> fen = new Dictionary<Vector2iTL, string>();

            while (sp < things.Count) {
                string line = things[sp++];

                if (line[0] == '[') {
                    List<string> slin = SplitUnquotedWhitespace(line.Trim(new char[]{'[', ']'}));

                    switch (slin[0]) {
                        case "Mode":
                            if (slin[1].Trim('"') != "5D") {
                                throw new NotSupportedException("Non-5D modes are not supported");
                            }
                            break;
                        case "Result":
                        case "Date":
                        case "Time":
                        case "White":
                        case "ID":
                            break;
                        case "Board":
                        case "Variant":
                            // These are in a different case because I might want to use them someday whereas the others are completely useless
                            break;
                        case "Active":
                            switch (slin[1].Trim('"')) {
                                case "WHITE":
                                    activePlayer = GameColour.WHITE;
                                    break;
                                case "BLACK":
                                    activePlayer = GameColour.BLACK;
                                    break;
                                default:
                                    activePlayer = GameColour.NONE;
                                    break;
                            }
                            break;
                        case "TimeControls":
                            Match timeMatch = Regex.Match(slin[1].Trim('"'), "(\\d+)\\+(\\d+)");
                            timer = new Timer(long.Parse(timeMatch.Groups[1].Value) * 60_000_000L, long.Parse(timeMatch.Groups[2].Value) * 1_000_000L);
                            break;
                        case "TimeClocks":
                            Match clocksMatch = Regex.Match(slin[1].Trim('"'), "(\\d+),(\\d+),(\\d+)");
                            timer.us_white = long.Parse(clocksMatch.Groups[1].Value);
                            timer.us_black = long.Parse(clocksMatch.Groups[2].Value);
                            timer.us_turn_started = long.Parse(clocksMatch.Groups[3].Value);
                            break;
                        case "Size":
                            Match sizeMatch = Regex.Match(slin[1].Trim('"'), "(\\d+)x(\\d+)");
                            boundsInfo.BoardSize.X = int.Parse(sizeMatch.Groups[1].Value);
                            boundsInfo.BoardSize.Y = int.Parse(sizeMatch.Groups[2].Value);
                            break;
                        case "KingPos":
                        case "CastlePos":
                        case "CastlerPos":
                            string wkPos = slin[1].Trim('"');
                            string bkPos = slin[2].Trim('"');
                            boundsInfo.KingPos = new Tuple<Vector2i, Vector2i>(new Vector2i(wkPos, boundsInfo), new Vector2i(bkPos, boundsInfo));
                            break;
                        case "Promotions":
                            allowedPromotions.Clear();
                            if (slin[1] == "-") {
                                break;
                            }
                            foreach (char c in slin[1]) {
                                Piece piece = Methods.FromChar(c);
                                allowedPromotions.Add(piece.GetPromotionSpec());
                            }
                            break;
                        default: // Assume it's a FEN string for a single-board setup
                            List<string> lfen = new List<string>(slin[0].Replace("*", "").Split(':'));
                            Vector2iTL bpos = new Vector2iTL(int.Parse(lfen[2]), int.Parse(lfen[1]), lfen[3] == "w" ? GameColour.WHITE : GameColour.BLACK);
                            fen.Add(
                                bpos,
                                lfen[0] + " " + lfen[3] + " " + lfen[4] + " - 0 1"
                            );
                            bi_min = bpos.vpos.Min(bi_min);
                            bi_max = bpos.vpos.Max(bi_max);
                            break;
                    }
                } else {

                    if (mp == 0) {
                        FenSetupPreMp(fen);
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

            if (mp == 0) {
                FenSetupPreMp(fen);
            }

            RecalculateGameBounds();
        }

        private void FenSetupPreMp(Dictionary<Vector2iTL, string> fen) {
            // Metadata is done, setup initial state
            moveStack = new Stack<IMove>();
            boards = new Dictionary<Vector2iTL, Board>();
            foreach (KeyValuePair<Vector2iTL, string> kv in fen) {
                Vector2iTL prev = (kv.Key - new Vector2i(1, 0)).NextTurn();
                Board board;
                if (boards.ContainsKey(prev)) {
                    board = new Board(boards[prev], kv.Value);
                } else {
                    board = new Board(boundsInfo, kv.Key, kv.Value);
                }
                boards.Add(kv.Key, board);
            }
        }

        public void MakeMoveStr(string move, GameColour colour) {
            Match castlesmatch = Regex.Match(move, "^[^()]*\\((.*?)\\)(O-O|O-O-O)(?:[+!?#])*$");
            if (castlesmatch.Success) {

                string tlstr = castlesmatch.Groups[1].Value;
                string castlesstr = castlesmatch.Groups[2].Value;

                Vector2iTL TL = new Vector2iTL(tlstr, colour);
                Vector2i XY, XY2;


                if (colour.isWhite()) {
                    XY = castlesOrigW;
                } else {
                    XY = castlesOrigB;
                }

                if (castlesstr == "O-O") {
                    if (colour.isWhite()) {
                        XY2 = castlesTgtWK;
                    } else {
                        XY2 = castlesTgtBK;
                    }
                } else {
                    if (colour.isWhite()) {
                        XY2 = castlesTgtWQ;
                    } else {
                        XY2 = castlesTgtBQ;
                    }
                }

                MakeMove(new Move(new Vector4iTL(XY, TL), new Vector4iTL(XY2, TL)));

            } else {

                Match mstrmatch = Regex.Match(move, "^[^()]*\\((.*?)\\)([A-Z]?[a-i][1-9])x?(.*?)(=[A-Z])?(?:[+!?#])*$");

                string tlstr = mstrmatch.Groups[1].Value;
                string xystr = mstrmatch.Groups[2].Value;

                string mstr2 = mstrmatch.Groups[3].Value;

                string tlstr2;
                string xystr2;

                string promotionstr;

                Vector2iTL TL = new Vector2iTL(tlstr, colour);
                Vector2i XY = new Vector2i(xystr, boundsInfo);

                if (mstr2[0] == '>') {
                    Match mstrmatch2 = Regex.Match(mstr2, "^>+x?\\((.*?)\\)([A-Z]?[a-i][1-9])$");

                    tlstr2 = mstrmatch2.Groups[1].Value;
                    xystr2 = mstrmatch2.Groups[2].Value;
                } else {
                    tlstr2 = tlstr;
                    xystr2 = mstr2;
                }

                Vector2iTL TL2 = new Vector2iTL(tlstr2, colour);
                Vector2i XY2 = new Vector2i(xystr2, boundsInfo);

                if (mstrmatch.Groups.Count > 4) {
                    promotionstr = mstrmatch.Groups[4].Value;
                } else {
                    promotionstr = "";
                }

                Move preliminaryMove = new Move(new Vector4iTL(XY, TL), new Vector4iTL(XY2, TL2));

                if (MoveShouldDoublePush(preliminaryMove)) {
                    preliminaryMove = new Move(new Vector4iTL(XY, TL), new Vector4iTL(XY2, TL2), MoveSpec.DoublePush);
                } else if (MoveShouldEnPassant(preliminaryMove)) {
                    preliminaryMove = new Move(new Vector4iTL(XY, TL), new Vector4iTL(XY2, TL2), MoveSpec.EnPassant);
                } else if (MoveShouldPromote(preliminaryMove)) {
                    char pc = promotionstr[1];
                    preliminaryMove = new Move(new Vector4iTL(XY, TL), new Vector4iTL(XY2, TL2), Methods.FromChar(pc).GetPromotionSpec());
                }

                MakeMove(preliminaryMove);

            }
        }

        public string MoveToString(Move move) {
            if (move.origin.TL == move.target.TL) {
                return string.Format("({1}T{0})", move.origin.T, move.origin.L) + GetPiece(move.origin).GetPgnChar() + move.origin.XY.ToString(boundsInfo) + (MoveShouldCapture(move) ? "x" : "") + move.target.XY.ToString(boundsInfo);
            } else {
                string sep = (move.origin.L == move.target.L) ? ">>" : ">";
                return string.Format("({1}T{0})", move.origin.T, move.origin.L) + GetPiece(move.origin).GetPgnChar() + move.origin.XY.ToString(boundsInfo) + sep + (MoveShouldCapture(move) ? "x" : "") + string.Format("({1}T{0})", move.target.T, move.target.L) + move.target.XY.ToString(boundsInfo);
            }
        }

        public static string GetPgnMetadata(string pgn) {
            string pgn_metadata = "";

            List<string> things = ParsePgnToList(pgn);

            int sp = 0;

            Dictionary<Vector2iTL, string> fen = new Dictionary<Vector2iTL, string>();

            while (sp < things.Count) {
                string line = things[sp++];

                if (line[0] == '[' && !line.StartsWith("[Active")) {
                    pgn_metadata += line + "\n";
                } else {
                    break;
                }
            }

            return pgn_metadata;
        }

        public string GetPgn() {
            string pgn = GetPgnMetadata(initialPgn);

            if (timer != null) {
                pgn += "\n[TimeClocks " + timer.us_white + "," + timer.us_black + "," + timer.us_turn_started + "]";
            }

            pgn += "\n[Active " + (activePlayer.ToString()) + "]";

            Vector1iTL mt = new Vector1iTL(1, GameColour.WHITE);

            string compound_move = "";

            foreach (IMove move in moveStack.Reverse()) {

                if (mt.colour != move.getColour()) {

                    if (mt.colour.isWhite()) {
                        pgn += "\n" + mt.T.ToString() + "." + compound_move;
                    } else {
                        pgn += " /" + compound_move;
                    }

                    compound_move = "";

                    mt = mt.NextTurn();
                }
                

                compound_move += " " + MoveToString(move);
            }

            if (mt.colour.isWhite()) {
                pgn += "\n" + mt.T.ToString() + "." + compound_move;
            } else {
                pgn += " /" + compound_move;
            }

            return pgn;
        }



        public GameState Clone() {
            return new GameState(this);
        }

        public string initialPgn { get; private set; }

        public BoundsInfo boundsInfo = new BoundsInfo(new Vector2i(8, 8), new Tuple<Vector2i, Vector2i>(new Vector2i(5, 8), new Vector2i(5, 1)));

        public Dictionary<Vector2iTL, Board> boards;
        public GameColour activePlayer;
        public Timer timer;
        public TimerView timerView => timer is null ? null : new TimerView(timer);

        public void StartWithTimer(Timer timer) {
            this.timer = timer;
        }

        public List<Board> GetMoveableBoards(GameColour colour = GameColour.NONE) {
            List<Board> mb = new List<Board>();

            List<Board> boardsList = boards.Values.ToList();
            foreach (Board board in boardsList) {
                if (BoardIsPlayable(board.TL) && board.TL.colour != colour.inverse()) {
                    mb.Add(board);
                }
            }

            return mb;
        }

        private int minT = 0;
        private int maxT = 0;

        private int minTL = 0;
        private int maxTL = 0;

        private int minATL => (-maxTL) - 1;
        private int maxATL => (-minTL) + 1;

        private int maxT5;

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

        
        public Board GetBoard(Vector2iTL TL) {
            if (TL == Vector2iTL.Null) return null;

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




        // It's almost definitely not viable to precalculate move tables, even for rooks. Luckily the *step* tables can be trivially precalculated, those would be quite expensive.
        public List<Move> GetSlidingMoves(Vector4iTL pos, Piece p = Piece.NONE) {
            Piece piece = ((p == Piece.NONE) ? GetPiece(pos) : p);
            List<Move> moves = new List<Move>();

            if ((piece & Piece.MASK_SPEC) != 0) {
                return moves;
            }

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



        public Vector2i castlesOrigW => boundsInfo.KingPos.Item1;
        public Vector2i castlesOrigB => boundsInfo.KingPos.Item2;

        public Vector2i castlesTgtWK => castlesOrigW + new Vector2i(+2, 0);
        public Vector2i castlesTgtWQ => castlesOrigW + new Vector2i(-2, 0);
        public Vector2i castlesTgtBK => castlesOrigB + new Vector2i(+2, 0);
        public Vector2i castlesTgtBQ => castlesOrigB + new Vector2i(-2, 0);

        public Vector2i castlesRookWK => new Vector2i(boundsInfo.BoardSize.X, castlesOrigW.Y);
        public Vector2i castlesRookWQ => new Vector2i(1, castlesOrigW.Y);
        public Vector2i castlesRookBK => new Vector2i(boundsInfo.BoardSize.X, castlesOrigB.Y);
        public Vector2i castlesRookBQ => new Vector2i(1, castlesOrigB.Y);

        public Vector2i castlesRTWK => castlesOrigW + new Vector2i(+1, 0);
        public Vector2i castlesRTWQ => castlesOrigW + new Vector2i(-1, 0);
        public Vector2i castlesRTBK => castlesOrigB + new Vector2i(+1, 0);
        public Vector2i castlesRTBQ => castlesOrigB + new Vector2i(-1, 0);

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
                Vector4iTL tgt = new Vector4iTL(castlesTgtWK, pos.TL);
                if (p.getColour() == GameColour.WHITE && GetPiece(tgt + new Vector4i(-1, 0, 0, 0)) == Piece.NONE && GetPiece(tgt) == Piece.NONE && (GetPiece(tgt + new Vector4i(1, 0, 0, 0)) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                    moves.Add(new Move(pos, tgt, MoveSpec.CastlesWK));
                }
            }

            if ((GetBoard(pos.TL).castleRights & CastleRights.WQ) != 0) {
                Vector4iTL tgt = new Vector4iTL(castlesTgtWQ, pos.TL);
                if (p.getColour() == GameColour.WHITE && GetPiece(tgt + new Vector4i(1, 0, 0, 0)) == Piece.NONE && GetPiece(tgt) == Piece.NONE && GetPiece(tgt + new Vector4i(-1, 0, 0, 0)) == Piece.NONE && (GetPiece(tgt + new Vector4i(-2, 0, 0, 0)) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                    moves.Add(new Move(pos, tgt, MoveSpec.CastlesWQ));
                }
            }

            if ((GetBoard(pos.TL).castleRights & CastleRights.BK) != 0) {
                Vector4iTL tgt = new Vector4iTL(castlesTgtBK, pos.TL);
                if (p.getColour() == GameColour.BLACK && GetPiece(tgt + new Vector4i(-1, 0, 0, 0)) == Piece.NONE && GetPiece(tgt) == Piece.NONE && (GetPiece(tgt + new Vector4i(1, 0, 0, 0)) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                    moves.Add(new Move(pos, tgt, MoveSpec.CastlesBK));
                }
            }

            if ((GetBoard(pos.TL).castleRights & CastleRights.BQ) != 0) {
                Vector4iTL tgt = new Vector4iTL(castlesTgtBQ, pos.TL);
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
            bool isFirstStep = (pos.Y == boundsInfo.BoardSize.Y + offset) || (pos.Y == 1 + offset);

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
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }

            off = new Vector4i(-1, offset, 0, 0);
            tgt = pos + off;
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }


            off = new Vector4i(0, 0, 1, offset);
            tgt = pos + off;
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }

            off = new Vector4i(0, 0, -1, offset);
            tgt = pos + off;
            if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
            }


            if ((piece & Piece.MASK_KIND) == Piece.PIECE_BRAWN) {
                off = new Vector4i(1, 0, 0, offset);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }

                off = new Vector4i(-1, 0, 0, offset);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }


                off = new Vector4i(0, offset, 1, 0);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }

                off = new Vector4i(0, offset, -1, 0);
                tgt = pos + off;
                if (IsInBoard(tgt) && (GetPiece(tgt).getColour() == piece.getColour().inverse() || GetBoard(tgt.TL).epTarget == tgt.XY)) {
                    moves.Add(new Move(pos, tgt, GetBoard(tgt.TL).epTarget == tgt.XY ? MoveSpec.EnPassant : MoveSpec.None));
                }
            }


            if (pos.Y + offset == 1 || pos.Y + offset == boundsInfo.BoardSize.Y) {
                // Promotion
                List<Move> promotionMoves = new List<Move>();
                foreach (Move move in moves) {
                    foreach (MoveSpec spec in allowedPromotions) {
                        promotionMoves.Add(new Move(move.origin, move.target, spec));
                    }
                }
                return promotionMoves;
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



        public List<Move> GetLegalMoves(Vector4iTL pos, Piece p = Piece.NONE) {
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

        public List<Move> GetLegalMoves(Vector2iTL bv) {
            List<Move> moves = new List<Move>();
            for (int x = 1; x <= boundsInfo.BoardSize.X; ++x) {
                for (int y = 1; y <= boundsInfo.BoardSize.Y; ++y) {
                    Vector4iTL pos = new Vector4iTL(new Vector2i(x, y), bv);
                    if (GetPiece(pos).getColour() == bv.colour) {
                        moves.AddRange(GetLegalMoves(pos));
                    }
                }
            }
            return moves;
        }


        public List<Move> GetLegalMoves(GameColour colour = GameColour.NONE) {
            if (colour == GameColour.NONE) {
                colour = activePlayer;
            }


            List<Move> moves = new List<Move>();

            foreach (Board board in GetMoveableBoards(colour)) {
                moves.AddRange(GetLegalMoves(board.TL));
            }

            return moves;
        }





        public bool IsInBoard(Vector4iTL pos) {
            return (1 <= pos.X && pos.X <= boundsInfo.BoardSize.X && 1 <= pos.Y && pos.Y <= boundsInfo.BoardSize.Y && BoardExists(pos.TL));
        }

        public bool MoveShouldPromote(Move move) {
            if (!BoardExists(move.origin.TL)) {
                throw new Exception("Error: origin board does not exist");
            }

            if (!BoardExists(move.target.TL)) {
                throw new Exception("Error: target board does not exist");
            }

            switch (GetPiece(move.origin) & Piece.MASK_KIND) {
                case Piece.PIECE_PAWN:
                case Piece.PIECE_BRAWN:
                    return (move.target.Y == 1 || move.target.Y == boundsInfo.BoardSize.Y);
                default:
                    return false;
            }
        }

        public bool MoveShouldCastle(Move move) {
            if (!BoardExists(move.origin.TL)) {
                throw new Exception("Error: origin board does not exist");
            }

            if (!BoardExists(move.target.TL)) {
                throw new Exception("Error: target board does not exist");
            }

            if ((GetPiece(move.origin) & Piece.MASK_KIND) == Piece.KIND_KING) {
                return Math.Abs(move.origin.X - move.target.X) == 2;
            } else {
                return false;
            }
        }

        public bool MoveShouldDoublePush(Move move) {
            if (!BoardExists(move.origin.TL)) {
                throw new Exception("Error: origin board does not exist");
            }

            if (!BoardExists(move.target.TL)) {
                throw new Exception("Error: target board does not exist");
            }

            switch (GetPiece(move.origin) & Piece.MASK_KIND) {
                case Piece.PIECE_PAWN:
                case Piece.PIECE_BRAWN:
                    return Math.Abs(move.target.Y - move.origin.Y) == 2;
                default:
                    return false;
            }
        }

        public bool MoveShouldEnPassant(Move move) {
            if (!BoardExists(move.origin.TL)) {
                throw new Exception("Error: origin board does not exist");
            }

            if (!BoardExists(move.target.TL)) {
                throw new Exception("Error: target board does not exist");
            }

            switch (GetPiece(move.origin) & Piece.MASK_KIND) {
                case Piece.PIECE_PAWN:
                case Piece.PIECE_BRAWN:

                    return move.target.XY == GetBoard(move.target.TL).epTarget;
                default:
                    return false;
            }
        }

        public bool MoveShouldCapture(Move move) {
            if (!BoardExists(move.origin.TL)) {
                throw new Exception("Error: origin board does not exist");
            }

            if (!BoardExists(move.target.TL)) {
                throw new Exception("Error: target board does not exist");
            }

            return GetPiece(move.target) != Piece.NONE || MoveShouldEnPassant(move);
        }

        public bool MakeMoveValidated(Move move, ColourRights rights) {
            if (rights.hasRights(move.getColour()) && GetLegalMoves(move.getColour()).Contains(move) && move.getColour() == activePlayer) {
                MakeMove(move);
                return true;
            } else {
                return false;
            }
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


            IMove imove = new IMove(move);
            CastleRights lostCastleRightsTarget = CastleRights.NONE;

            if ((GetPiece(move.target) & Piece.MASK_KIND) == Piece.KIND_KING) {
                switch (GetPiece(move.target).getColour()) {
                    case GameColour.WHITE:
                        if (move.target.XY == castlesOrigW) {
                            lostCastleRightsTarget |= (CastleRights.WK | CastleRights.WQ);
                        }
                        break;
                    case GameColour.BLACK:
                        if (move.target.XY == castlesOrigB) {
                            lostCastleRightsTarget |= (CastleRights.BK | CastleRights.BQ);
                        }
                        break;
                }
            }

            if ((GetPiece(move.target) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                Vector2i p = move.target.XY;
                if (p == castlesRookWK) {
                    lostCastleRightsTarget |= CastleRights.WK;
                } else if (p == castlesRookWQ) {
                    lostCastleRightsTarget |= CastleRights.WQ;
                } else if (p == castlesRookBK) {
                    lostCastleRightsTarget |= CastleRights.BK;
                } else if (p == castlesRookBQ) {
                    lostCastleRightsTarget |= CastleRights.BQ;
                }
            }

            Board newFromBoard, newToBoard;

            Piece promotionPiece = Piece.NONE;

            switch (move.spec) {
                case MoveSpec.PromoteKnight:
                    promotionPiece = Piece.PIECE_KNIGHT;
                    break;
                case MoveSpec.PromoteRook:
                    promotionPiece = Piece.PIECE_ROOK;
                    break;
                case MoveSpec.PromoteBishop:
                    promotionPiece = Piece.PIECE_BISHOP;
                    break;
                case MoveSpec.PromoteUnicorn:
                    promotionPiece = Piece.PIECE_UNICORN;
                    break;
                case MoveSpec.PromoteDragon:
                    promotionPiece = Piece.PIECE_DRAGON;
                    break;
                case MoveSpec.PromotePrincess:
                    promotionPiece = Piece.PIECE_PRINCESS;
                    break;
                case MoveSpec.PromoteQueen:
                    promotionPiece = Piece.PIECE_QUEEN;
                    break;
                default:
                    break;
            }

            if (move.origin.TL == move.target.TL) {
                // Standard move
                Board moveBoard = GetBoard(move.origin.TL);

                Piece movePiece = moveBoard.GetPiece(move.origin.XY);

                if (promotionPiece != Piece.NONE) {
                    movePiece = (movePiece.getColour().isWhite() ? Piece.COLOUR_WHITE : Piece.COLOUR_BLACK) | promotionPiece;
                }

                newFromBoard = newToBoard = new Board(moveBoard, movePiece, move.origin.XY, move.target.XY);

                imove.target_child = newToBoard.TL;
                imove.origin_child = newFromBoard.TL;
                imove.capture_target = move.target.XY;
                if (imove.capture_target == moveBoard.epTarget) {
                    int offset = (int)movePiece.getColour();
                    imove.capture_target += new Vector2i(0, offset);
                }
                imove.captured = moveBoard.GetPiece(imove.capture_target);

                // Only highlight if a move was actually made, don't highlight on ForceSkipTurn()
                if (move.origin.XY != move.target.XY) {
                    newFromBoard.moveFrom = move.origin.XY;
                    newFromBoard.moveTo = move.target.XY;
                }

                if (move.spec.IsCastles()) {
                    Vector2i rookFrom, rookTo;
                    switch (move.spec) {
                        case MoveSpec.CastlesWK:
                            rookFrom = castlesRookWK;
                            rookTo = castlesRTWK;
                            break;
                        case MoveSpec.CastlesWQ:
                            rookFrom = castlesRookWQ;
                            rookTo = castlesRTWQ;
                            break;
                        case MoveSpec.CastlesBK:
                            rookFrom = castlesRookBK;
                            rookTo = castlesRTBK;
                            break;
                        case MoveSpec.CastlesBQ:
                            rookFrom = castlesRookBQ;
                            rookTo = castlesRTBQ;
                            break;
                        default:
                            rookFrom = Vector2i.ZERO;
                            rookTo = Vector2i.ZERO;
                            break;
                    }
                    Piece p2 = newToBoard.GetPiece(rookFrom);
                    newToBoard.RemovePiece(rookFrom);
                    newToBoard.PlacePiece(p2, rookTo);
                } else if (move.spec == MoveSpec.DoublePush) {
                    newToBoard.epTarget = (move.target.XY + move.origin.XY) * 0.5;
                } else if (move.spec == MoveSpec.EnPassant) {
                    // En passant
                    int offset = (int)movePiece.getColour();
                    newToBoard.RemovePiece(move.target.XY + new Vector2i(0, offset));
                }

                boards.Add(newFromBoard.TL, newFromBoard);
                moveStack.Push(imove);
            } else if (BoardIsPlayable(move.target.TL)) {
                // Move to active board (no timeline split)

                Board fromBoard = GetBoard(move.origin.TL);
                Board toBoard = GetBoard(move.target.TL);

                Piece movePiece = fromBoard.GetPiece(move.origin.XY);

                if (promotionPiece != Piece.NONE) {
                    movePiece = (movePiece.getColour().isWhite() ? Piece.COLOUR_WHITE : Piece.COLOUR_BLACK) | promotionPiece;
                }

                newFromBoard = new Board(fromBoard, movePiece, move.origin.XY, Vector2i.ZERO);
                newToBoard = new Board(toBoard.TL.Y, toBoard, movePiece, move.target.XY);

                imove.target_child = newToBoard.TL;
                imove.origin_child = newFromBoard.TL;
                imove.capture_target = move.target.XY;
                if (imove.capture_target == toBoard.epTarget) {
                    int offset = (int)movePiece.getColour();
                    imove.capture_target += new Vector2i(0, offset);
                }
                imove.captured = toBoard.GetPiece(imove.capture_target);

                newFromBoard.moveTravel = move.origin.XY;
                newToBoard.moveTravel = move.target.XY;

                if (move.spec == MoveSpec.EnPassant) {
                    // En passant
                    int offset = (int)movePiece.getColour();
                    newToBoard.RemovePiece(move.target.XY + new Vector2i(0, offset));
                }

                boards.Add(newFromBoard.TL, newFromBoard);
                boards.Add(newToBoard.TL, newToBoard);
                moveStack.Push(imove);
            } else {
                // Move to frozen board (timeline split)

                Board fromBoard = GetBoard(move.origin.TL);
                Board toBoard = GetBoard(move.target.TL);

                Piece movePiece = fromBoard.GetPiece(move.origin.XY);

                if (promotionPiece != Piece.NONE) {
                    movePiece = (movePiece.getColour().isWhite() ? Piece.COLOUR_WHITE : Piece.COLOUR_BLACK) | promotionPiece;
                }

                newFromBoard = new Board(fromBoard, movePiece, move.origin.XY, Vector2i.ZERO);

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
                    imove.capture_target += new Vector2i(0, offset);
                }
                imove.captured = toBoard.GetPiece(imove.capture_target);

                newFromBoard.moveTravel = move.origin.XY;
                newToBoard.moveTravel = move.target.XY;

                if (move.spec == MoveSpec.EnPassant) {
                    // En passant
                    int offset = (int)movePiece.getColour();
                    newToBoard.RemovePiece(move.target.XY + new Vector2i(0, offset));
                }

                boards.Add(newFromBoard.TL, newFromBoard);
                boards.Add(newToBoard.TL, newToBoard);
                moveStack.Push(imove);
            }

            if ((GetPiece(move.origin) & Piece.MASK_KIND) == Piece.KIND_KING) {
                switch (GetPiece(move.origin).getColour()) {
                    case GameColour.WHITE:
                        if (move.origin.XY == castlesOrigW) {
                            newFromBoard.castleRights &=~ (CastleRights.WK | CastleRights.WQ);
                        }
                        break;
                    case GameColour.BLACK:
                        if (move.origin.XY == castlesOrigB) {
                            newFromBoard.castleRights &=~ (CastleRights.BK | CastleRights.BQ);
                        }
                        break;
                }
            }

            if ((GetPiece(move.origin) & Piece.MASK_KIND) == Piece.PIECE_ROOK) {
                Vector2i p = move.origin.XY;
                if (p == castlesRookWK) {
                    newFromBoard.castleRights &=~ CastleRights.WK;
                } else if (p == castlesRookWQ) {
                    newFromBoard.castleRights &=~ CastleRights.WQ;
                } else if (p == castlesRookBK) {
                    newFromBoard.castleRights &=~ CastleRights.BK;
                } else if (p == castlesRookBQ) {
                    newFromBoard.castleRights &=~ CastleRights.BQ;
                }
            }

            newToBoard.castleRights &=~ lostCastleRightsTarget;

            if ((GetPiece(move.target) & Piece.MASK_ROYAL) != 0) {
                // uhoh the player haseth deaddoed
                newToBoard.playerHasLost |= GetPiece(move.target).getColour().isWhite() ? ColourRights.WHITE : ColourRights.BLACK;
            }

            maxT = Math.Max(maxT, Math.Max(newFromBoard.TL.T, newToBoard.TL.T));
            maxT5 = Math.Max(maxT5, Math.Max(newFromBoard.TL.NextTurn().T, newToBoard.TL.NextTurn().T));

            playerLossBuf = PlayerHasLostImpl();

        }

        public ColourRights PlayerHasLostImpl() {
            ColourRights pl = ColourRights.NONE;
            foreach (Board board in GetMoveableBoards()) {
                pl |= board.playerHasLost;
            }
            return pl;
        }

        public ColourRights playerHasLost { get; private set; }

        private ColourRights playerLossBuf;

        public bool PlayerRoyalCaptured => playerLossBuf != ColourRights.NONE;

        public bool CanSubmitMoves(ColourRights rights = ColourRights.BOTH) {
            return GetPresentColour() != activePlayer && rights.hasRights(activePlayer);
        }

        public bool CanUndoMoves(ColourRights rights = ColourRights.BOTH) {
            return moveStack.Count > 0 && moveStack.First().getColour() == activePlayer && rights.hasRights(activePlayer);
        }

        public bool LastMoveWasTravel() {
            return moveStack.Count > 0 && moveStack.First().isTravel();
        }

        public bool ColourCanActiveTravel(GameColour colour) {
            switch (colour) {
                case GameColour.WHITE:
                    return maxATL > maxTL;
                case GameColour.BLACK:
                    return minATL < minTL;
                default:
                    return minATL < minTL && maxATL > maxTL;
            }
        }

        public int GetMinATL() => minATL;
        public int GetMaxATL() => maxATL;

        public int GetMinTL() => minTL;
        public int GetMaxTL() => maxTL;

        public int GetMinT() => minT;
        public int GetMaxT() => maxT;

        public int GetMaxT5() => maxT5;


        public void RecalculateGameBounds() {
            minTL = int.MaxValue;
            maxTL = int.MinValue;

            minT = int.MaxValue;
            maxT = int.MinValue;
            maxT5 = int.MinValue;

            foreach (Vector2iTL pos in boards.Keys) {
                minTL = Math.Min(minTL, pos.L);
                maxTL = Math.Max(maxTL, pos.L);

                minT = Math.Min(minT, pos.T);
                maxT = Math.Max(maxT, pos.T);
                maxT5 = Math.Max(maxT5, pos.NextTurn().T);
            }
        }

        public List<IMove> GetUnsubmittedMoves() {
            List<IMove> boards = new List<IMove>();
            foreach (IMove move in moveStack) {
                if (move.getColour() != activePlayer) {
                    return boards;
                }
                boards.Add(move);
            }
            return boards;
        }

        public struct BoardSubmissionState {
            public BoardSubmissionState(bool isSubmitted, bool isTravel = false) {
                this.isSubmitted = isSubmitted;
                this.isTravel = isTravel;
            }

            public readonly bool isSubmitted;
            public readonly bool isTravel;
        }

        public BoardSubmissionState GetBoardSubmissionState(Vector2iTL pos) {
            foreach (IMove move in GetUnsubmittedMoves()) {
                if (pos == move.origin_child || pos == move.target_child) {
                    return new BoardSubmissionState(isSubmitted: false, isTravel: move.isTravel());
                }
            }
            return new BoardSubmissionState(true);
        }

        public void SubmitMoves(ColourRights rights = ColourRights.BOTH) {
            if (!CanSubmitMoves()) {
                //throw new InvalidOperationException("Attempted to submit moves when the Present did not change colour");
                return;
            }

            if (!rights.hasRights(activePlayer)) {
                return;
            }

            activePlayer = GetPresentColour();
            playerHasLost = playerLossBuf;
            if (timer != null) {
                if (!playerHasLost.hasNone()) {
                    timer.Stop();
                } else {
                    timer.SetTurn(activePlayer);
                }
            }
        }



        public void GuiUndoMove() {
            if (CanUndoMoves()) {
                UnmakeMove();
            } else if (moveStack.Count > 0) {
                activePlayer = activePlayer.inverse();
                UnmakeMove();
            }
        }


        public void UnmakeMove(Move move = null, ColourRights rights = ColourRights.BOTH) {
            if (!(move is null) && move != moveStack.First()) {
                throw new InvalidOperationException("Error: Attempting to unmake a move that's not at the top of the stack");
            }


            if (!(rights.hasRights(moveStack.First().getColour()) && rights.hasRights(activePlayer))) {
                return;
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

            playerLossBuf = PlayerHasLostImpl();
        }
    }
}
