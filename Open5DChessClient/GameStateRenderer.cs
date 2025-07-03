using ChessCommon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

        private static Rectangle Scaled(Rectangle input) {
            Vector2 pos = positionScalar(input.Location.ToVector2());
            Vector2 size = scaleScalar(input.Size.ToVector2());

            return new Rectangle((int)pos.X, (int)pos.Y, (int)size.X, (int)size.Y);
        }

        private static Rectangle Scaled(int x, int y, int w, int h) {
            return Scaled(new Rectangle(x, y, w, h));
        }

        private static Func<Vector2, Vector2> scaleScalar;
        private static Func<Vector2, Vector2> positionScalar;
        private static SpriteBatch spriteBatch;

        private static readonly Vector2 PIECE_SIZE = new Vector2(32);
        private static readonly Vector2 BOARD_SIZE = new Vector2(8);
        private static readonly Vector2 BOARD_OFFSET = new Vector2(12);

        private static readonly Color LIGHT_SQUARE_COLOUR = Color.BlanchedAlmond;
        private static readonly Color DARK_SQUARE_COLOUR = Color.Tan;

        private static readonly Color WHITE_BOARD_COLOUR = Color.White;
        private static readonly Color BLACK_BOARD_COLOUR = Color.Black;

        private static void PieceDraw(Vector2i pos, int id) {
            if (id < 0) return;
            int idx = id % 6;
            int idy = id / 6;
            spriteBatch.Draw(pieceTexture, Scaled((int)(pos.X * PIECE_SIZE.X), (int)(pos.Y * PIECE_SIZE.Y), (int)PIECE_SIZE.X, (int)PIECE_SIZE.Y), new Rectangle(512 * idx, 512 * idy, 512, 512), Color.White);
        }

        public static void RenderBoard(Board board) {
            spriteBatch.Draw(sq, Scaled(
                (int)(board.TLVis.X * PIECE_SIZE.X * BOARD_OFFSET.X - PIECE_SIZE.X * (BOARD_SIZE.X / 2 + 0.5f)),
                (int)(board.TLVis.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y - PIECE_SIZE.Y * (BOARD_SIZE.Y / 2 + 0.5f)),
                (int)(PIECE_SIZE.X * (BOARD_SIZE.X + 1)),
                (int)(PIECE_SIZE.Y * (BOARD_SIZE.Y + 1))),
                board.turn.isWhite() ? WHITE_BOARD_COLOUR : BLACK_BOARD_COLOUR
            );
            for (int i = 0; i < BOARD_SIZE.X; i++) {
                for (int j = 0; j < BOARD_SIZE.Y; j++) {
                    spriteBatch.Draw(sq, Scaled(
                        (int)(board.TLVis.X * PIECE_SIZE.X * BOARD_OFFSET.X - PIECE_SIZE.X * (BOARD_SIZE.X / 2 - i)),
                        (int)(board.TLVis.Y * PIECE_SIZE.Y * BOARD_OFFSET.Y - PIECE_SIZE.Y * (BOARD_SIZE.Y / 2 - j)),
                        (int)(PIECE_SIZE.X),
                        (int)(PIECE_SIZE.Y)),
                        (i + j) % 2 == 0 ? LIGHT_SQUARE_COLOUR : DARK_SQUARE_COLOUR
                    );
                    int id = Methods.GetPieceID(board.GetPiece(new Vector2i(i + 1, j + 1)));
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


            scaleScalar = (Vector2 input) => input * new Vector2(MathF.Pow(10, cameraPosition.Z));
            positionScalar = (Vector2 input) => scaleScalar(input - new Vector2(cameraPosition.X, cameraPosition.Y)) + (windowSize / new Vector2(2.0f));

            foreach (Board board in gameState.boards.Values) {
                RenderBoard(board);
            }

            spriteBatch = null;
        }
    }
}
