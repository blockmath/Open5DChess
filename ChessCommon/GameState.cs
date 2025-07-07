using ChessCommon;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public class GameState {
        public GameState() {
            moveStack = new Stack<IMove>();
            boards = new Dictionary<Vector2iTL, Board> { { Vector2iTL.ORIGIN_WHITE, new Board() } };
            MakeMove(new Move(new Vector4iTL(5, 7, 0, 0, GameColour.WHITE), new Vector4iTL(5, 5, 0, 0, GameColour.WHITE)));
            MakeMove(new Move(new Vector4iTL(5, 2, 0, 0, GameColour.BLACK), new Vector4iTL(5, 4, 0, 0, GameColour.BLACK)));
            MakeMove(new Move(new Vector4iTL(7, 8, 1, 0, GameColour.WHITE), new Vector4iTL(7, 6, 0, 0, GameColour.WHITE)));
            MakeMove(new Move(new Vector4iTL(7, 1, 0, 1, GameColour.BLACK), new Vector4iTL(7, 3, 0, 0, GameColour.BLACK)));
        }

        public GameState(GameState o) {
            boards = o.boards.ToDictionary(entry => entry.Key, entry => entry.Value.Clone());
            minTL = o.minTL;
            maxTL = o.maxTL;
            moveStack = new Stack<IMove>(o.moveStack.ToArray());
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

        private static void SetupStepTables() {
            foreach (Vector4i sa in rookSteps)
                foreach (Vector4i sb in rookSteps) {
                    Vector4i s = sa + sb;
                    if (s.isUnit() && !bishopSteps.Contains(s)) {
                        bishopSteps.Add(s);
                    }
                }

            foreach (Vector4i sa in rookSteps)
                foreach (Vector4i sb in rookSteps)
                    foreach (Vector4i sc in rookSteps) {
                        Vector4i s = sa + sb + sc;
                        if (s.isUnit() && !unicornSteps.Contains(s)) {
                            unicornSteps.Add(s);
                        }
                    }

            foreach (Vector4i sa in rookSteps)
                foreach (Vector4i sb in rookSteps)
                    foreach (Vector4i sc in rookSteps)
                        foreach (Vector4i sd in rookSteps) {
                            Vector4i s = sa + sb + sc + sd;
                            if (s.isUnit() && !dragonSteps.Contains(s)) {
                                dragonSteps.Add(s);
                            }
                        }
        }

        static GameState() {
            SetupStepTables();
        }





        public List<Vector4iTL> GetSlidingMoves(Vector4iTL pos, Piece p = Piece.NONE) {
            Piece piece = ((p == Piece.NONE) ? GetPiece(pos) : p);

            List<Vector4iTL> moves = new List<Vector4iTL>();
            if ((piece & Piece.MOVABL_ROOK) != 0) {
                foreach (Vector4i s in rookSteps) {
                    for (int i = 1;; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == pos.colour) {
                            break;
                        } else if (GetPiece(spCoord).getColour() != pos.colour) {
                            moves.Add(spCoord);
                        }
                    }
                }
            }

            if ((piece & Piece.MOVABL_BISHOP) != 0) {
                foreach (Vector4i s in bishopSteps) {
                    for (int i = 1; ; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == pos.colour) {
                            break;
                        } else if (GetPiece(spCoord).getColour() != pos.colour) {
                            moves.Add(spCoord);
                        }
                    }
                }
            }

            if ((piece & Piece.MOVABL_UNICORN) != 0) {
                foreach (Vector4i s in unicornSteps) {
                    for (int i = 1; ; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == pos.colour) {
                            break;
                        } else if (GetPiece(spCoord).getColour() != pos.colour) {
                            moves.Add(spCoord);
                        }
                    }
                }
            }

            if ((piece & Piece.MOVABL_DRAGON) != 0) {
                foreach (Vector4i s in dragonSteps) {
                    for (int i = 1; ; ++i) {
                        Vector4i sp = s * i;
                        Vector4iTL spCoord = pos + sp;
                        if (!IsInBoard(spCoord) || GetPiece(spCoord).getColour() == pos.colour) {
                            break;
                        } else if (GetPiece(spCoord).getColour() != pos.colour) {
                            moves.Add(spCoord);
                        }
                    }
                }
            }

            return moves;
        }
















        public bool IsInBoard(Vector4iTL pos) {
            return (0 <= pos.X && pos.X <= 7 && 0 <= pos.Y && pos.Y <= 7 && BoardExists(pos.TL));
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

            if (move.origin.TL == move.target.TL) {
                // Standard move
                Board moveBoard = GetBoard(move.origin.TL);

                IMove imove = new IMove(move.origin, move.target);

                Piece movePiece = moveBoard.GetPiece(move.origin.XY);

                Board newBoard = new Board(moveBoard, movePiece, move.origin.XY, move.target.XY);

                imove.target_child = newBoard.TL;
                imove.origin_child = newBoard.TL;
                imove.capture_target = move.target.XY;
                if (imove.capture_target == moveBoard.epTarget) {
                    int offset = (int)movePiece.getColour();
                    imove.capture_target.Y += offset;
                }
                imove.captured = moveBoard.GetPiece(imove.capture_target);

                newBoard.moveFrom = move.origin.XY;
                newBoard.moveTo = move.target.XY;

                boards.Add(newBoard.TL, newBoard);
                moveStack.Push(imove);
            } else if (BoardIsPlayable(move.target.TL)) {
                // Move to active board (no timeline split)

                Board fromBoard = GetBoard(move.origin.TL);
                Board toBoard = GetBoard(move.target.TL);

                IMove imove = new IMove(move.origin, move.target);

                Piece movePiece = fromBoard.GetPiece(move.origin.XY);

                Board newFromBoard = new Board(fromBoard, movePiece, move.origin.XY, null);
                Board newToBoard = new Board(toBoard.TL.Y, toBoard, movePiece, move.target.XY);

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

                IMove imove = new IMove(move.origin, move.target);

                Piece movePiece = fromBoard.GetPiece(move.origin.XY);

                Board newFromBoard = new Board(fromBoard, movePiece, move.origin.XY, null);

                int newL = 0;
                if (move.getColour() == GameColour.WHITE) {
                    newL = ++maxTL;
                } else {
                    newL = --minTL;
                }

                Board newToBoard = new Board(newL, toBoard, movePiece, move.target.XY);

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
