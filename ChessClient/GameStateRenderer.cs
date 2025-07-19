using ChessCommon;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessClient {
    internal static class GameStateRenderer {

        public static GameState gameState;

        public static ColourRights userRights;

        public static Vector2 ws_mpos;
        private static Vector2i ws_mposi;

        public static Vector2 ws_i;

        public static Vector4iTL selected = null;
        public static Vector4iTL hovered = null;
        public static List<Vector4iTL> highlighted = new List<Vector4iTL>();


        public static Texture2D pieceTexture;
        public static Texture2D sq;
        public static Texture2D arsq;
        public static Texture2D circle;

        public static FontSystem fontSystem;
        private static SpriteFontBase spriteFont;

        private static SpriteBatch spriteBatch;

        public static readonly Vector2 PIECE_SIZE = new Vector2(32);
        public static readonly Vector2 BOARD_SIZE = new Vector2(8);
        public static readonly Vector2 BOARD_OFFSET = new Vector2(12);

        private static readonly Color LIGHT_SQUARE_COLOUR = Color.BlanchedAlmond;
        private static readonly Color DARK_SQUARE_COLOUR = Color.Tan;

        private static readonly Color WHITE_BOARD_COLOUR = Color.White;
        private static readonly Color BLACK_BOARD_COLOUR = Color.Black;
        private static readonly Color UNSUB_BOARD_COLOUR = new Color(255, 245, 103);
        private static readonly Color UNSUB_TRAVEL_BOARD_COLOUR = Color.MediumPurple;

        public static readonly Color TIME_COLOUR_LIGHT = Color.Lerp(Color.MediumPurple, Color.White, 0.25f);
        public static readonly Color TIME_COLOUR_DARK = Color.Lerp(Color.MediumPurple, Color.Black, 0.00f);

        public static readonly Color WHITE_BOARD_COLOUR_SHADED_A = new Color(240, 240, 240);
        public static readonly Color BLACK_BOARD_COLOUR_SHADED_A = new Color(48, 48, 48);
        
        public static readonly Color WHITE_BOARD_COLOUR_SHADED_B = Color.White;
        public static readonly Color BLACK_BOARD_COLOUR_SHADED_B = Color.Black;

        private static readonly Color LIGHT_GRID_COLOUR = new Color(232, 232, 240);
        private static readonly Color DARK_GRID_COLOUR = new Color(224, 224, 232);

        private static readonly Color TL_COLOUR_A = Color.MediumPurple;
        private static readonly Color TL_COLOUR_B = Color.MultiplyAlpha(Color.Multiply(Color.MediumPurple, 0.8f), 2.0f);

        private static readonly Color HIGHLIGHT_COLOUR_MOVED = Color.Multiply(Color.Yellow, 0.5f);
        private static readonly Color HIGHLIGHT_COLOUR_TRAVELLED = Color.Multiply(Color.MediumPurple, 0.8f);

        private static readonly Color TRAVEL_COLOUR = Color.MultiplyAlpha(Color.MediumPurple, 1f);
        private static readonly Color TRAVEL_COLOUR_WHITE = Color.MultiplyAlpha(Color.White, 1f);
        private static readonly Color TRAVEL_COLOUR_BLACK = Color.MultiplyAlpha(Color.Black, 1f);

        private static readonly Color SELECTED_COLOUR = Color.Multiply(Color.Green, 0.35f);
        private static readonly Color HIGHLIGHTED_COLOUR = Color.Multiply(Color.Lime, 0.35f);
        private static readonly Color HOVERED_COLOUR = Color.Multiply(Color.White, 0.35f);

        private static readonly Color GHOST_PIECE_COLOUR = Color.Multiply(Color.White, 0.5f);

        private static Vector2 BoardDrawPos(Vector2iTL TL) {
            return new Vector2(Methods.TVis(TL) * PIECE_SIZE.X * BOARD_OFFSET.X, TL.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y);
        }

        public static Rectangle VecRect(Vector2 pos, Vector2 size) => new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);

        public static Rectangle RectSurrounding(Vector2 center, Vector2 size) {
            return VecRect(center - size / 2, size);
        }

        public static Rectangle SqrSurrounding(Vector2 center, float size) {
            return RectSurrounding(center, new Vector2(size));
        }

        private static void PieceDraw(Vector2i pos, int id, Color colour) {
            if (id < 0) return;
            int idx = id % 6;
            int idy = id / 6;
            spriteBatch.Draw(pieceTexture, new Rectangle((int)(pos.X * PIECE_SIZE.X), (int)(pos.Y * PIECE_SIZE.Y), (int)PIECE_SIZE.X, (int)PIECE_SIZE.Y), new Rectangle(512 * idx, 512 * idy, 512, 512), colour);
        }

        public static void UDrawLineSegment(Vector2 point1, Vector2 point2, Color color, int width = 1) {
            float angle = MathF.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2) + 1;
            Vector2 midway = (point1 + point2) * 0.5f;
            spriteBatch.Draw(sq, midway, null, color, angle, new Vector2(0.5f, 0.5f), new Vector2(length, width), SpriteEffects.None, 0);
        }

        public static void UDrawTriSegment(Vector2 point1, Vector2 point2, Color color, int width = 1) {
            float angle = MathF.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = width;
            Vector2 foto = point2 - point1;
            foto.Normalize();
            Vector2 midway = point1 + (foto * (length + 1) * 0.5f);
            spriteBatch.Draw(arsq, midway, null, color, angle, new Vector2(256, 256), new Vector2(length, width) / -512, SpriteEffects.None, 0);
        }

        public static List<Vector2> GenerateBezierSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int totalSamples, bool includeLast = true) {
            List<Vector2> samples = new List<Vector2>();
            for (int j = 0; j < totalSamples; ++j) {
                float t = j / (float)totalSamples;

                Vector2 samplePoint;

                Vector2 a, b, c, d, e;

                a = Vector2.Lerp(p0, p1, t);
                b = Vector2.Lerp(p1, p2, t);
                c = Vector2.Lerp(p2, p3, t);

                d = Vector2.Lerp(a, b, t);
                e = Vector2.Lerp(b, c, t);

                samplePoint = Vector2.Lerp(d, e, t);

                samples.Add(samplePoint);
            }

            if (includeLast) {
                samples.Add(p3);
            }

            return samples;
        }

        public static List<Vector2> GenerateHorizontalBezier(List<Vector2> points, int samplesPerSegment, float pushLength, float handleLength) {
            List<Vector2> samples = new List<Vector2>();

            for (int i = 0; i < points.Count - 1; ++i) {
                Vector2 p0 = points[i] + new Vector2(-pushLength, 0);
                Vector2 p1 = points[i] + new Vector2(-handleLength, 0);

                Vector2 p3 = points[i + 1] + new Vector2(pushLength, 0);
                Vector2 p2 = points[i + 1] + new Vector2(handleLength, 0);

                samples.AddRange(GenerateBezierSegment(p0, p1, p2, p3, samplesPerSegment, false));
            }

            samples.Add(points.Last());

            return samples;
        }

        // Render a gizmo for the timelines (connecting boards to their parents and children)
        public static void RenderTimelineGizmo(Vector2iTL headTL) {


            Color tlColour = gameState.TimelineIsActive(headTL.L) ? TL_COLOUR_A : (headTL.L > 0 ? WHITE_BOARD_COLOUR_SHADED_A : BLACK_BOARD_COLOUR_SHADED_A);
            Color tlColourShaded = gameState.TimelineIsActive(headTL.L) ? TL_COLOUR_B : (headTL.L > 0 ? WHITE_BOARD_COLOUR_SHADED_B : BLACK_BOARD_COLOUR_SHADED_B);

            // Get the basic path points (where the boards are)
            List<Vector2> points = new List<Vector2>();

            Board b = gameState.GetBoard(headTL);

            int initL = headTL.Y;

            while (true) {
                points.Add(BoardDrawPos(b.TL));

                b = gameState.GetBoard(b.parentTL);

                if (b is null || b.TL.Y != initL) {
                    break;
                }
            }

            if (b is not null) {
                points.Add(BoardDrawPos(b.TL));
            }


            // Generate and sample a bezier curve through the boards. The control points are all assumed to be horizontal and a constant distance from the controllee.
            List<Vector2> bezierPoints = GenerateHorizontalBezier(points, 256, 0.25f * PIECE_SIZE.X * BOARD_SIZE.X, 1.25f * PIECE_SIZE.X * BOARD_SIZE.X);

            for (int i = 0; i < bezierPoints.Count - 1; ++i) {
                UDrawLineSegment(bezierPoints[i], bezierPoints[i + 1], tlColourShaded, (int)(PIECE_SIZE.Y * 2));
            }
            for (int i = 0; i < bezierPoints.Count - 1; ++i) {
                UDrawLineSegment(bezierPoints[i], bezierPoints[i + 1], tlColour, (int)(PIECE_SIZE.Y * 1.5));
            }
        }
        
        // Rotate vectors clockwise/counterclockwise
        private static Vector2 RCW(Vector2 i) => new Vector2(i.Y, -i.X);
        private static Vector2 RCCW(Vector2 i) => -RCW(i);

        // Rotate depending on colour
        private static Vector2 RDOC(Vector2 i, GameColour colour) => (colour.isWhite()) ? RCW(i) : RCCW(i);

        public static void RenderTravelGizmo(Vector4iTL tailVPos, Vector4iTL headVPos) {

            GameColour colour = tailVPos.colour;

            Vector2 tp0 = new Vector2(Methods.TVis(tailVPos) * PIECE_SIZE.X * BOARD_OFFSET.X + (tailVPos.X - 4.5f) * PIECE_SIZE.X, tailVPos.L * PIECE_SIZE.Y * BOARD_OFFSET.Y + (tailVPos.Y - 4.5f) * PIECE_SIZE.Y);
            Vector2 tp3 = new Vector2(Methods.TVis(headVPos) * PIECE_SIZE.X * BOARD_OFFSET.X + (headVPos.X - 4.5f) * PIECE_SIZE.X, headVPos.L * PIECE_SIZE.Y * BOARD_OFFSET.Y + (headVPos.Y - 4.5f) * PIECE_SIZE.Y);

            Vector2 tdp = tp3 - tp0;
            tdp.Normalize();


            Vector2 p0 = tp0 + tdp * PIECE_SIZE.X * 0.4f;
            Vector2 p3 = tp3 - tdp * PIECE_SIZE.X * 0.5f;

            Vector2 dp = p3 - p0;

            Vector2 p1 = Vector2.Lerp(p0, p3, 1 / 3f) + RDOC(dp, colour) * 0.1f;
            Vector2 p2 = Vector2.Lerp(p0, p3, 2 / 3f) + RDOC(dp, colour) * 0.1f;

            List<Vector2> points = GenerateBezierSegment(p0, p1, p2, p3, 256);

            Color colourA = colour.isWhite() ? TRAVEL_COLOUR_WHITE : TRAVEL_COLOUR_BLACK;
            for (int i = 0; i < points.Count - 1; ++i) {
                UDrawLineSegment(points[i], points[i + 1], colourA, (int)(PIECE_SIZE.Y * 0.66));
            }
            UDrawTriSegment(points[points.Count - 1], 2 * points[points.Count - 1] - points[points.Count - 2], colourA, (int)(PIECE_SIZE.Y * 0.66));

            for (int i = 0; i < points.Count - 1; ++i) {
                UDrawLineSegment(points[i], points[i + 1], TRAVEL_COLOUR, (int)(PIECE_SIZE.Y * 0.5));
            }
            UDrawTriSegment(points[points.Count - 1], 2 * points[points.Count - 1] - points[points.Count - 2], TRAVEL_COLOUR, (int)(PIECE_SIZE.Y * 0.5));
        }

        // Render a gizmo for the big bar that shows the Present (the time of the earliest active playable board)
        // If you make 5.6 million timelines and run out of bar that's on you
        // actually that would probably break **all** the rendering code outright sooooooo...
        public static void RenderThePresent() {
            float presentPos = gameState.GetPresentPly() * PIECE_SIZE.X * BOARD_OFFSET.X;

            float minTLPos = (gameState.GetMinTL() - 1) * PIECE_SIZE.Y * BOARD_OFFSET.Y;
            float maxTLPos = (gameState.GetMaxTL() + 1) * PIECE_SIZE.Y * BOARD_OFFSET.Y;


            Color presentColour = gameState.GetPresentColour().isWhite() ? WHITE_BOARD_COLOUR_SHADED_A : BLACK_BOARD_COLOUR_SHADED_A;
            Color presentColourShaded = gameState.GetPresentColour().isWhite() ? WHITE_BOARD_COLOUR_SHADED_B : BLACK_BOARD_COLOUR_SHADED_B;
            Color presentTextColour = gameState.GetPresentColour().isBlack() ? WHITE_BOARD_COLOUR_SHADED_A : BLACK_BOARD_COLOUR_SHADED_A;


            // Draw the bar

            spriteBatch.Draw(sq, new Rectangle((int)(presentPos - (PIECE_SIZE.X * 3)), int.MinValue / 2, (int)(PIECE_SIZE.X * 6), int.MaxValue), presentColourShaded);
            spriteBatch.Draw(sq, new Rectangle((int)(presentPos - (PIECE_SIZE.X * 2.5)), int.MinValue / 2, (int)(PIECE_SIZE.X * 5), int.MaxValue), presentColour);


            // Draw the indicators for ability to active travel

            // For White
            if (gameState.ColourCanActiveTravel(GameColour.WHITE)) {
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, maxTLPos), (int)(PIECE_SIZE.X * 3)), WHITE_BOARD_COLOUR_SHADED_B);
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, maxTLPos), (int)(PIECE_SIZE.X * 2.5)), WHITE_BOARD_COLOUR_SHADED_A);
            } else {
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, maxTLPos), (int)(PIECE_SIZE.X * 2)), WHITE_BOARD_COLOUR_SHADED_B);
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, maxTLPos), (int)(PIECE_SIZE.X * 1.5)), WHITE_BOARD_COLOUR_SHADED_A);
            }

            // For Black
            if (gameState.ColourCanActiveTravel(GameColour.BLACK)) {
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, minTLPos), (int)(PIECE_SIZE.X * 3)), BLACK_BOARD_COLOUR_SHADED_B);
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, minTLPos), (int)(PIECE_SIZE.X * 2.5)), BLACK_BOARD_COLOUR_SHADED_A);
            } else {
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, minTLPos), (int)(PIECE_SIZE.X * 2)), BLACK_BOARD_COLOUR_SHADED_B);
                spriteBatch.Draw(circle, SqrSurrounding(new Vector2(presentPos, minTLPos), (int)(PIECE_SIZE.X * 1.5)), BLACK_BOARD_COLOUR_SHADED_A);
            }


            // Draw the "The Present" label
            Vector2 labelSize = spriteFont.MeasureString("The Present");

            spriteBatch.DrawString(spriteFont, "The Present", new Vector2(presentPos + labelSize.Y / 2 + 16, maxTLPos + PIECE_SIZE.Y * BOARD_OFFSET.Y), presentTextColour, (float)(Math.PI / 2));
            spriteBatch.DrawString(spriteFont, "The Present", new Vector2(presentPos + labelSize.Y / 2 + 16, minTLPos - PIECE_SIZE.Y * BOARD_OFFSET.Y - labelSize.X), presentTextColour, (float)(Math.PI / 2));
        }

        // Render a single board, including the border (showing whether it's playable, whose turn it is, etc.)
        public static void RenderBoard(Board board) {

            float borderWidth = gameState.BoardIsPlayable(board.TL) ? 0.5f : 0.125f;

            Point boardPos = new Point (
                (int)(Methods.TVis(board.TL) * PIECE_SIZE.X * BOARD_OFFSET.X - PIECE_SIZE.X * (BOARD_SIZE.X / 2 + borderWidth)),
                (int)(board.TL.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y - PIECE_SIZE.Y * (BOARD_SIZE.Y / 2 + borderWidth))
            );
            Point boardSize = new Point(
                (int)(PIECE_SIZE.X * (BOARD_SIZE.X + 2 * borderWidth)),
                (int)(PIECE_SIZE.Y * (BOARD_SIZE.Y + 2 * borderWidth))
            );
            

            Color boardBorderColour = Color.Transparent;
            GameState.BoardSubmissionState submissionState = gameState.GetBoardSubmissionState(board.TL);

            if (submissionState.isSubmitted) {
                boardBorderColour = (board.TL.colour.isWhite() ? WHITE_BOARD_COLOUR : BLACK_BOARD_COLOUR);
            } else {
                boardBorderColour = (submissionState.isTravel ? UNSUB_TRAVEL_BOARD_COLOUR : UNSUB_BOARD_COLOUR);
            }

            spriteBatch.Draw(sq, new Rectangle(
                boardPos,
                boardSize),
                boardBorderColour
            );
            for (int i = 0; i < BOARD_SIZE.X; i++) {
                for (int j = 0; j < BOARD_SIZE.Y; j++) {

                    // Draw the actual square on the board first
                    Rectangle targetRect = new Rectangle(
                        (int)(Methods.TVis(board.TL) * PIECE_SIZE.X * BOARD_OFFSET.X - PIECE_SIZE.X * (BOARD_SIZE.X / 2 - i)),
                        (int)(board.TL.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y - PIECE_SIZE.Y * (BOARD_SIZE.Y / 2 - j)),
                        (int)(PIECE_SIZE.X),
                        (int)(PIECE_SIZE.Y));
                    spriteBatch.Draw(sq, targetRect, (i + j) % 2 == 0 ? LIGHT_SQUARE_COLOUR : DARK_SQUARE_COLOUR);


                    Vector2i xy = new Vector2i(i + 1, j + 1);
                    Vector4iTL xytl = new Vector4iTL(xy, board.TL);

                    Vector2i xypos = new Vector2i(
                        (int)(Methods.TVis(board.TL) * BOARD_OFFSET.X - BOARD_SIZE.X / 2 + i),
                        (int)(board.TL.Y * BOARD_OFFSET.Y - BOARD_SIZE.Y / 2 + j));

                    if (xypos == ws_mposi) {
                        hovered = xytl;
                    }

                    // Draw overlays for a piece having moved/travelled, if a square is hovered/selected/highlighted, etc.
                    if (xytl == selected) {
                        spriteBatch.Draw(sq, targetRect, SELECTED_COLOUR);
                    } else if (highlighted.Contains(xytl)) {
                        spriteBatch.Draw(sq, targetRect, HIGHLIGHTED_COLOUR);
                    } else if (xy == board.moveFrom || xy == board.moveTo) {
                        spriteBatch.Draw(sq, targetRect, HIGHLIGHT_COLOUR_MOVED);
                    } else if (xy == board.moveTravel) {
                        spriteBatch.Draw(sq, targetRect, HIGHLIGHT_COLOUR_TRAVELLED);
                    }

                    if (xytl == hovered) {
                        spriteBatch.Draw(sq, targetRect, HOVERED_COLOUR);
                    }

                    int id = Methods.GetPieceID(board.GetPiece(xy));
                    // Draw piece after square
                    PieceDraw(xypos, id, Color.White);


                    if (xytl == hovered && highlighted.Contains(xytl)) {
                        // Congratulations! We're hovering over a valid move. Show a ghostly image of the piece!
                        PieceDraw(xypos, Methods.GetPieceID(gameState.GetPiece(selected)), GHOST_PIECE_COLOUR);
                    }
                }
            }
        }


        public static void AttemptSelection() {
            if (hovered is null) {
                return;
            }

            if (highlighted.Contains(hovered)) {
                // Make a move!!
                Move move = new Move(selected, hovered);

                if (gameState.MoveShouldPromote(move)) {
                    // TODO: Actually put in a modal for the player to select a piece to promote to.
                    // Currently we're just back at the "you can only promote to queen due to UI limitations" bs.
                    // Too bad!
                    move = new Move(selected, hovered, MoveSpec.PromoteQueen);
                } else if (gameState.MoveShouldDoublePush(move)) {
                    move = new Move(selected, hovered, MoveSpec.DoublePush);
                } else if (gameState.MoveShouldEnPassant(move)) {
                    move = new Move(selected, hovered, MoveSpec.EnPassant);
                } else if (gameState.MoveShouldCastle(move)) {
                    MoveSpec castleSpec = MoveSpec.None;

                    if (move.target.XY == GameState.castlesTgtWK) castleSpec = MoveSpec.CastlesWK;
                    if (move.target.XY == GameState.castlesTgtWQ) castleSpec = MoveSpec.CastlesWQ;
                    if (move.target.XY == GameState.castlesTgtBK) castleSpec = MoveSpec.CastlesBK;
                    if (move.target.XY == GameState.castlesTgtBQ) castleSpec = MoveSpec.CastlesBQ;

                    move = new Move(selected, hovered, castleSpec);
                }

                gameState.MakeMoveValidated(move, userRights);
            }

            Board board = gameState.GetBoard(hovered.TL);

            if (board is not null) {
                if (gameState.BoardIsPlayable(hovered.TL)) {
                    if (board.GetPiece(hovered.XY).getColour() == board.TL.colour) {
                        selected = hovered;
                        highlighted = Move.GetTargets(gameState.GetLegalMoves(selected));
                    } else {
                        selected = null;
                        highlighted = new List<Vector4iTL>();
                    }
                } else {
                    selected = null;
                    highlighted = new List<Vector4iTL>();
                }
            }
        }


        public static void RenderGrid() {
            int ws_overscan = 2 * Math.Max(Math.Max((int)(ws_i.X / BOARD_OFFSET.X), (int)(ws_i.Y / BOARD_OFFSET.Y)), 10);

            for (int t = gameState.GetMinT() - ws_overscan; t <= gameState.GetMaxT() + ws_overscan; ++t) {
                for (int l = gameState.GetMinTL() - ws_overscan; l <= gameState.GetMaxTL() + ws_overscan; ++l) {
                    Rectangle target = new Rectangle(
                        (int)((2 * t) * PIECE_SIZE.X * BOARD_OFFSET.X - PIECE_SIZE.X * BOARD_OFFSET.X / 2),
                        (int)(l * PIECE_SIZE.Y * BOARD_OFFSET.Y - PIECE_SIZE.Y * BOARD_OFFSET.Y / 2),
                        (int)(2 * PIECE_SIZE.X * BOARD_OFFSET.X),
                        (int)(PIECE_SIZE.Y * BOARD_OFFSET.Y)
                    );

                    spriteBatch.Draw(sq, target, (t + l) % 2 == 0 ? LIGHT_GRID_COLOUR : DARK_GRID_COLOUR);
                }
            }
        }


        public static Vector2 GetInitialCameraState() {
            Vector2 cam_min = (new Vector2(gameState.bi_min.X, gameState.bi_min.Y) - new Vector2(0.5f)) * PIECE_SIZE * BOARD_OFFSET;
            Vector2 cam_max = (new Vector2(gameState.bi_max.X, gameState.bi_max.Y) + new Vector2(0.5f)) * PIECE_SIZE * BOARD_OFFSET;

            return (cam_min + cam_max) / 2;
        }


        // Note: Checking `hovered` (for interactions like clicking) should be done outside of this function,
        // because this function also does double duty of figuring out what square is hovered in the first place!
        public static void Render(SpriteBatch batch, Vector3 cameraPosition, Vector2 windowSize) {

            try {

                // Store the batch temporarily in a static field so it can be accessed in subroutines without having to pass it around everywhere
                spriteBatch = batch;

                // Setup sprite fonts, etc
                spriteFont = fontSystem.GetFont(192);


                // Update the mouse's position (in piece tiles), clear `hovered` (the mouse may have moved)
                ws_mposi = new Vector2i((int)MathF.Floor(ws_mpos.X / PIECE_SIZE.X), (int)MathF.Floor(ws_mpos.Y / PIECE_SIZE.Y));
                hovered = null;

                // Render all the gizmos and boards in depth order
                RenderGrid();

                RenderThePresent();

                foreach (Board board in gameState.GetMoveableBoards()) {
                    RenderTimelineGizmo(board.TL);
                }

                foreach (Board board in gameState.boards.Values) {
                    RenderBoard(board);
                }

                foreach (IMove move in gameState.GetMoves()) {
                    if (move.origin.TL != move.target.TL) {
                        RenderTravelGizmo(move.origin, move.target);
                    }
                }

            } finally {
                // The batch is obviously only accessible *while* we're rendering, not in other calls.
                spriteBatch = null;
            }

        }
    }
}
