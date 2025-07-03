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
            boards = new Dictionary<Vector2i, Board> { { new Vector2i(0, 0), new Board(new Vector2i(0, 0)) } };
            MakeMove(new Move(new Vector4i(5, 7, 0, 0), new Vector4i(5, 5, 0, 0), GameColour.WHITE));
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

        public Dictionary<Vector2i, Board> boards;
        public GameColour activePlayer;

        public List<Board> GetMoveableBoards() {
            List<Board> mb = new List<Board>();

            foreach (Board board in boards.Values) {
                if (!boards.ContainsKey(board.TLVis + new Vector2i(1, 0))) {
                    mb.Add(board);
                }
            }

            return mb;
        }


        private int minATL => (-maxTL) - 1;
        private int maxATL => (-minTL) + 1;
        private int minTL;
        private int maxTL;

        private Stack<IMove> moveStack;

        public bool TimelineIsActive(int l) {
            return minATL <= l && l <= maxATL;
        }

        public bool BoardExists(Vector2i TL, GameColour colour) {
            Debug.WriteLine(Board.TLVisImpl(TL, colour));
            return boards.ContainsKey(Board.TLVisImpl(TL, colour));
        }

        public bool BoardIsPlayable(Vector2i TL, GameColour colour) {
            return !boards.ContainsKey(Board.TLVisImpl(TL, colour) + new Vector2i(1, 0));
        }

        public int GetPresentPly() {
            List<Board> mb = GetMoveableBoards();

            int minTurn = int.MaxValue;

            foreach (Board board in mb) {
                if (board.TLVis.X < minTurn && TimelineIsActive(board.TLVis.Y)) {
                    minTurn = board.TLVis.X;
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

        // MIKE, the BOARD, please
        public Board GetBoard(Vector2i TL, GameColour colour) {
            Vector2i TLVis = Board.TLVisImpl(TL, colour);

            return boards[TLVis];
        }

        public void MakeMove(Move move) {
            if (!BoardExists(move.origin.TL, move.colour)) {
                throw new Exception("Error: origin board does not exist");
            }

            if (!BoardExists(move.target.TL, move.colour)) {
                throw new Exception("Error: target board does not exist");
            }

            if (!BoardIsPlayable(move.origin.TL, move.colour)) {
                throw new Exception("Error: Attempting to make a move on a frozen board");
            }

            if (move.origin.TL == move.target.TL) {
                // Standard move
                Board moveBoard = GetBoard(move.origin.TL, move.colour);

                IMove imove = new IMove(move.origin, move.target, move.colour);

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

                boards.Add(newBoard.TLVis, newBoard);
                moveStack.Push(imove);
            } else if (BoardIsPlayable(move.target.TL, move.colour)) {
                // Move to active board (no timeline split)

                Board fromBoard = GetBoard(move.origin.TL, move.colour);
                Board toBoard = GetBoard(move.target.TL, move.colour);

                IMove imove = new IMove(move.origin, move.target, move.colour);

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

                boards.Add(newFromBoard.TLVis, newFromBoard);
                boards.Add(newToBoard.TLVis, newToBoard);
                moveStack.Push(imove);
            } else {
                // Move to frozen board (timeline split)

                Board fromBoard = GetBoard(move.origin.TL, move.colour);
                Board toBoard = GetBoard(move.target.TL, move.colour);

                IMove imove = new IMove(move.origin, move.target, move.colour);

                Piece movePiece = fromBoard.GetPiece(move.origin.XY);

                Board newFromBoard = new Board(fromBoard, movePiece, move.origin.XY, null);

                int newL = 0;
                if (move.colour == GameColour.WHITE) {
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

                boards.Add(newFromBoard.TLVis, newFromBoard);
                boards.Add(newToBoard.TLVis, newToBoard);
                moveStack.Push(imove);
            }
        }

        public void UnmakeMove(Move move) {
            if (move != moveStack.First()) {
                throw new Exception("Error: Attempting to unmake a move that's not at the top of the stack");
            }

            IMove imove = moveStack.Pop();

            boards.Remove(imove.origin_child);
            if (boards.ContainsKey(imove.target_child)) {
                boards.Remove(imove.target_child);
            }
        }
    }
}
