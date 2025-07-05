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


        public static Texture2D pieceTexture;
        public static Texture2D sq;

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

        private static Vector2 BoardDrawPos(Vector2i TLVis) {
            return new Vector2(TLVis.X * PIECE_SIZE.X * BOARD_OFFSET.X, TLVis.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y);
        }

        private static void PieceDraw(Vector2i pos, int id) {
            if (id < 0) return;
            int idx = id % 6;
            int idy = id / 6;
            spriteBatch.Draw(pieceTexture, new Rectangle((int)(pos.X * PIECE_SIZE.X), (int)(pos.Y * PIECE_SIZE.Y), (int)PIECE_SIZE.X, (int)PIECE_SIZE.Y), new Rectangle(512 * idx, 512 * idy, 512, 512), Color.White);
        }

        public static void UDrawLineSegment(Vector2 point1, Vector2 point2, Color color, int width = 1) { //Pixel => 1x1 white texture...
            float angle = MathF.Atan2(point2.Y - point1.Y, point2.X - point1.X);
            float length = Vector2.Distance(point1, point2) + 1;
            Vector2 midway = (point1 + point2) * 0.5f;
            spriteBatch.Draw(sq, midway, null, color, angle, new Vector2(0.5f, 0.5f), new Vector2(length, width), SpriteEffects.None, 0);
        }

        public static List<Vector2> GenerateBezierSegment(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3, int totalSamples, bool includeLast = true) {
            List<Vector2> samples = new List<Vector2>();
            for (int j = 0; j < totalSamples; ++j) {
                float t = (float)j / (float)totalSamples;

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
                UDrawLineSegment(bezierPoints[i], bezierPoints[i + 1], TL_COLOUR_A, (int)(PIECE_SIZE.Y * 1.5));
            }
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
                    if (xy == board.moveFrom || xy == board.moveTo) {
                        spriteBatch.Draw(sq, targetRect, HIGHLIGHT_COLOUR_MOVED);
                    }
                    if (xy == board.moveTravel) {
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

            //scaleScalar = (Vector2 input) => input * new Vector2(MathF.Pow(10, cameraPosition.Z));
            //positionScalar = (Vector2 input) => scaleScalar(input - new Vector2(cameraPosition.X, cameraPosition.Y)) + (windowSize / new Vector2(2.0f));

            foreach (Board board in gameState.GetMoveableBoards()) {
                RenderTimelineGizmo(board.TLVis);
            }

            foreach (Board board in gameState.boards.Values) {
                RenderBoard(board);
            }

            spriteBatch = null;
        }
    }
}
