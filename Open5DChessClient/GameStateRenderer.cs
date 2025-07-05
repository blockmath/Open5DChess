using ChessCommon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessClient {
    internal static class GameStateRenderer {

        public static GameState gameState;

        public static readonly Vector4i DESELECTED = new Vector4i(int.MinValue, int.MinValue, int.MinValue, int.MinValue);
        public static Vector4i selected = new Vector4i(DESELECTED);


        public static Texture2D pieceTexture;
        public static Texture2D sq;
        public static Texture2D arsq;

        private static Func<Vector2, Vector2> scaleScalar = (Vector2 input) => input;
        private static Func<Vector2, Vector2> positionScalar = (Vector2 input) => input;
        private static SpriteBatch spriteBatch;

        private static readonly Vector2 PIECE_SIZE = new Vector2(32);
        private static readonly Vector2 BOARD_SIZE = new Vector2(8);
        private static readonly Vector2 BOARD_OFFSET = new Vector2(12);

        private static readonly Color LIGHT_SQUARE_COLOUR = Color.BlanchedAlmond;
        private static readonly Color DARK_SQUARE_COLOUR = Color.Tan;

        private static readonly Color WHITE_BOARD_COLOUR = Color.White;
        private static readonly Color BLACK_BOARD_COLOUR = Color.Black;

        private static readonly Color TL_COLOUR_A = Color.MediumPurple;
        private static readonly Color TL_COLOUR_B = Color.MultiplyAlpha(Color.Multiply(Color.MediumPurple, 0.8f), 2.0f);

        private static readonly Color HIGHLIGHT_COLOUR_MOVED = Color.MultiplyAlpha(Color.Yellow, 0.5f);
        private static readonly Color HIGHLIGHT_COLOUR_TRAVELLED = Color.MultiplyAlpha(Color.MediumPurple, 0.8f);
        private static readonly Color HIGHLIGHT_COLOUR_SELECTED = Color.LimeGreen;

        private static readonly Color TRAVEL_COLOUR = Color.MultiplyAlpha(Color.MediumPurple, 1f);
        private static readonly Color TRAVEL_COLOUR_WHITE = Color.MultiplyAlpha(Color.White, 1f);
        private static readonly Color TRAVEL_COLOUR_BLACK = Color.MultiplyAlpha(Color.Black, 1f);

        private static Vector2 BoardDrawPos(Vector2i TLVis) {
            return new Vector2(TLVis.X * PIECE_SIZE.X * BOARD_OFFSET.X, TLVis.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y);
        }

        private static void PieceDraw(Vector2i pos, int id) {
            if (id < 0) return;
            int idx = id % 6;
            int idy = id / 6;
            spriteBatch.Draw(pieceTexture, new Rectangle((int)(pos.X * PIECE_SIZE.X), (int)(pos.Y * PIECE_SIZE.Y), (int)PIECE_SIZE.X, (int)PIECE_SIZE.Y), new Rectangle(512 * idx, 512 * idy, 512, 512), Color.White);
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

        public static List<Vector2> GenerateHorizontalBezier(List<Vector2> points, int samplesPerSegment, float handleLength) {
            List<Vector2> samples = new List<Vector2>();

            for (int i = 0; i < points.Count - 1; ++i) {
                Vector2 p0 = points[i];
                Vector2 p1 = p0 + new Vector2(-handleLength, 0);

                Vector2 p3 = points[i + 1];
                Vector2 p2 = p3 + new Vector2(handleLength, 0);

                samples.AddRange(GenerateBezierSegment(p0, p1, p2, p3, samplesPerSegment, false));
            }

            samples.Add(points.Last());

            return samples;
        }

        public static void RenderTimelineGizmo(Vector2i headTL) {
            List<Vector2> points = new List<Vector2>();

            Board b = gameState.GetBoardVis(headTL);

            int initL = headTL.Y;

            while (b.TL.X >= 0) {
                points.Add(BoardDrawPos(b.TLVis));

                b = gameState.GetBoardVis(b.parentTLVis);

                if (b is null || b.TL.Y != initL) {
                    break;
                }
            }

            if (!(b is null)) {
                points.Add(BoardDrawPos(b.TLVis));
            }

            List<Vector2> bezierPoints = GenerateHorizontalBezier(points, 256, 1.5f * PIECE_SIZE.X * BOARD_SIZE.X);

            for (int i = 0; i < bezierPoints.Count - 1; ++i) {
                UDrawLineSegment(bezierPoints[i], bezierPoints[i + 1], TL_COLOUR_B, (int)(PIECE_SIZE.Y * 2));
            }
            for (int i = 0; i < bezierPoints.Count - 1; ++i) {
                UDrawLineSegment(bezierPoints[i], bezierPoints[i + 1], TL_COLOUR_A, (int)(PIECE_SIZE.Y * 1.5));
            }
        }

        private static Vector2 RCW(Vector2 i) => new Vector2(i.Y, -i.X);
        private static Vector2 RCCW(Vector2 i) => -RCW(i);

        private static Vector2 RDOC(Vector2 i, GameColour colour) => (colour.isWhite()) ? RCW(i) : RCCW(i);

        public static void RenderTravelGizmo(Vector4i tailVPos, Vector4i headVPos, GameColour colour) {

            Vector2 tp0 = new Vector2(tailVPos.T * PIECE_SIZE.X * BOARD_OFFSET.X + (tailVPos.X - 4.5f) * PIECE_SIZE.X, tailVPos.L * PIECE_SIZE.Y * BOARD_OFFSET.Y + (tailVPos.Y - 4.5f) * PIECE_SIZE.Y);
            Vector2 tp3 = new Vector2(headVPos.T * PIECE_SIZE.X * BOARD_OFFSET.X + (headVPos.X - 4.5f) * PIECE_SIZE.X, headVPos.L * PIECE_SIZE.Y * BOARD_OFFSET.Y + (headVPos.Y - 4.5f) * PIECE_SIZE.Y);

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

        public static void RenderBoard(Board board) {
            float borderWidth = gameState.BoardIsPlayable(board.TL, board.turn) ? 0.5f : 0.125f;
            spriteBatch.Draw(sq, new Rectangle(
                (int)(board.TLVis.X * PIECE_SIZE.X * BOARD_OFFSET.X - PIECE_SIZE.X * (BOARD_SIZE.X / 2 + borderWidth)),
                (int)(board.TLVis.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y - PIECE_SIZE.Y * (BOARD_SIZE.Y / 2 + borderWidth)),
                (int)(PIECE_SIZE.X * (BOARD_SIZE.X + 2 * borderWidth)),
                (int)(PIECE_SIZE.Y * (BOARD_SIZE.Y + 2 * borderWidth))),
                board.turn.isWhite() ? WHITE_BOARD_COLOUR : BLACK_BOARD_COLOUR
            );
            for (int i = 0; i < BOARD_SIZE.X; i++) {
                for (int j = 0; j < BOARD_SIZE.Y; j++) {
                    Rectangle targetRect = new Rectangle(
                        (int)(board.TLVis.X * PIECE_SIZE.X * BOARD_OFFSET.X - PIECE_SIZE.X * (BOARD_SIZE.X / 2 - i)),
                        (int)(board.TLVis.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y - PIECE_SIZE.Y * (BOARD_SIZE.Y / 2 - j)),
                        (int)(PIECE_SIZE.X),
                        (int)(PIECE_SIZE.Y));
                    spriteBatch.Draw(sq, targetRect, (i + j) % 2 == 0 ? LIGHT_SQUARE_COLOUR : DARK_SQUARE_COLOUR);
                    Vector2i xy = new Vector2i(i + 1, j + 1);
                    if (board.TLVis == selected.TL && xy == selected.XY) {
                        spriteBatch.Draw(sq, targetRect, HIGHLIGHT_COLOUR_SELECTED);
                    } else if (xy == board.moveFrom || xy == board.moveTo) {
                        spriteBatch.Draw(sq, targetRect, HIGHLIGHT_COLOUR_MOVED);
                    } else if (xy == board.moveTravel) {
                        spriteBatch.Draw(sq, targetRect, HIGHLIGHT_COLOUR_TRAVELLED);
                    }
                    int id = Methods.GetPieceID(board.GetPiece(xy));
                    // Draw piece after square
                    PieceDraw(new Vector2i(
                        (int)(board.TLVis.X * BOARD_OFFSET.X - BOARD_SIZE.X / 2 + i),
                        (int)(board.TLVis.Y * BOARD_OFFSET.Y - BOARD_SIZE.Y / 2 + j)),
                        id
                    );
                }
            }
        }

        public static void Render(SpriteBatch batch, Vector3 cameraPosition, Vector2 windowSize) {
            spriteBatch = batch;

            // Todo: render the Present

            foreach (Board board in gameState.GetMoveableBoards()) {
                RenderTimelineGizmo(board.TLVis);
            }

            foreach (Board board in gameState.boards.Values) {
                RenderBoard(board);
            }

            foreach (IMove move in gameState.GetMoves()) {
                if (move.origin.TL != move.target.TL) {
                    RenderTravelGizmo(move.originV, move.targetV, move.colour);
                }
            }

            spriteBatch = null;
        }
    }
}
