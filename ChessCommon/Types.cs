

using System.Collections.Generic;
using System.Security.Policy;

namespace ChessCommon {

    public enum GameColour : int {
        WHITE = +1,
        BLACK = -1
    }

    public enum Piece : byte {
        MASK_KIND =                 0b00011111,
        MASK_ROYAL =                0b00100000,
        MASK_COLOUR =               0b01000000,

        MOVABL_ROOK =               0b00000001,
        MOVABL_BISHOP =             0b00000010,
        MOVABL_UNICORN =            0b00000100,
        MOVABL_DRAGON =             0b00001000,

        MOVABL_SPEC_PAWN =          0b00000001,
        MOVABL_SPEC_KNIGHT =        0b00000010,
        MOVABL_SPEC_BRAWN =         0b00000011,
        MOVABL_SPEC_KING =          0b00000100,

        FLAG_ROYAL =                0b00100000,
        FLAG_SPEC =                 0b00010000,

        COLOUR_WHITE =              0b00000000,
        COLOUR_BLACK =              0b01000000,

        PIECE_ROOK =                MOVABL_ROOK,
        PIECE_BISHOP =              MOVABL_BISHOP,
        PIECE_UNICORN =             MOVABL_UNICORN,
        PIECE_DRAGON =              MOVABL_DRAGON,

        PIECE_PAWN =                FLAG_SPEC | MOVABL_SPEC_PAWN,
        PIECE_KNIGHT =              FLAG_SPEC | MOVABL_SPEC_KNIGHT,
        PIECE_BRAWN =               FLAG_SPEC | MOVABL_SPEC_BRAWN,
        PIECE_COMMONKING =          FLAG_SPEC | MOVABL_SPEC_KING,

        PIECE_PRINCESS =            MOVABL_ROOK | MOVABL_BISHOP,
        PIECE_QUEEN =               MOVABL_ROOK | MOVABL_BISHOP | MOVABL_UNICORN | MOVABL_DRAGON,

        PIECE_KING =                FLAG_ROYAL | PIECE_COMMONKING,
        PIECE_ROYALQUEEN =          FLAG_ROYAL | PIECE_QUEEN,

        NONE =                      0b00000000
    }

    public enum CastleRights : byte {
        NONE = 0b0000,
        WK = 0b0001,
        WQ = 0b0010,
        BK = 0b0100,
        BQ = 0b1000
    }

    public static class Methods {
        public static Piece FromChar(char c) {
            Piece p = Piece.NONE;
            switch (c) {
                case 'R':
                    p = Piece.PIECE_ROOK | Piece.COLOUR_WHITE; break;
                case 'B':
                    p = Piece.PIECE_BISHOP | Piece.COLOUR_WHITE; break;
                case 'U':
                    p = Piece.PIECE_UNICORN | Piece.COLOUR_WHITE; break;
                case 'D':
                    p = Piece.PIECE_DRAGON | Piece.COLOUR_WHITE; break;
                case 'N':
                    p = Piece.PIECE_KNIGHT | Piece.COLOUR_WHITE; break;
                case 'Q':
                    p = Piece.PIECE_QUEEN | Piece.COLOUR_WHITE; break;
                case 'K':
                    p = Piece.PIECE_KING | Piece.COLOUR_WHITE; break;
                case 'P':
                    p = Piece.PIECE_PAWN | Piece.COLOUR_WHITE; break;
                case 'W':
                    p = Piece.PIECE_BRAWN | Piece.COLOUR_WHITE; break;
                case 'S':
                    p = Piece.PIECE_PRINCESS | Piece.COLOUR_WHITE; break;
                case 'Y':
                    p = Piece.PIECE_ROYALQUEEN | Piece.COLOUR_WHITE; break;
                case 'C':
                    p = Piece.PIECE_COMMONKING | Piece.COLOUR_WHITE; break;

                case 'r':
                    p = Piece.PIECE_ROOK | Piece.COLOUR_BLACK; break;
                case 'b':
                    p = Piece.PIECE_BISHOP | Piece.COLOUR_BLACK; break;
                case 'u':
                    p = Piece.PIECE_UNICORN | Piece.COLOUR_BLACK; break;
                case 'd':
                    p = Piece.PIECE_DRAGON | Piece.COLOUR_BLACK; break;
                case 'n':
                    p = Piece.PIECE_KNIGHT | Piece.COLOUR_BLACK; break;
                case 'q':
                    p = Piece.PIECE_QUEEN | Piece.COLOUR_BLACK; break;
                case 'k':
                    p = Piece.PIECE_KING | Piece.COLOUR_BLACK; break;
                case 'p':
                    p = Piece.PIECE_PAWN | Piece.COLOUR_BLACK; break;
                case 'w':
                    p = Piece.PIECE_BRAWN | Piece.COLOUR_BLACK; break;
                case 's':
                    p = Piece.PIECE_PRINCESS | Piece.COLOUR_BLACK; break;
                case 'y':
                    p = Piece.PIECE_ROYALQUEEN | Piece.COLOUR_BLACK; break;
                case 'c':
                    p = Piece.PIECE_COMMONKING | Piece.COLOUR_BLACK; break;

                default:
                    p = Piece.NONE; break;
            }
            return p;
        }

        public static int GetPieceID(Piece piece) {
            switch (piece) {
                case Piece.NONE: default: return -1;

                case Piece.COLOUR_WHITE | Piece.PIECE_PAWN: return 0;
                case Piece.COLOUR_WHITE | Piece.PIECE_KNIGHT: return 1;
                case Piece.COLOUR_WHITE | Piece.PIECE_BISHOP: return 2;
                case Piece.COLOUR_WHITE | Piece.PIECE_ROOK: return 3;
                case Piece.COLOUR_WHITE | Piece.PIECE_QUEEN: return 4;
                case Piece.COLOUR_WHITE | Piece.PIECE_KING: return 5;

                case Piece.COLOUR_BLACK | Piece.PIECE_PAWN: return 6;
                case Piece.COLOUR_BLACK | Piece.PIECE_KNIGHT: return 7;
                case Piece.COLOUR_BLACK | Piece.PIECE_BISHOP: return 8;
                case Piece.COLOUR_BLACK | Piece.PIECE_ROOK: return 9;
                case Piece.COLOUR_BLACK | Piece.PIECE_QUEEN: return 10;
                case Piece.COLOUR_BLACK | Piece.PIECE_KING: return 11;

                case Piece.COLOUR_WHITE | Piece.PIECE_BRAWN: return 12;
                case Piece.COLOUR_WHITE | Piece.PIECE_UNICORN: return 13;
                case Piece.COLOUR_WHITE | Piece.PIECE_DRAGON: return 14;
                case Piece.COLOUR_WHITE | Piece.PIECE_PRINCESS: return 15;
                case Piece.COLOUR_WHITE | Piece.PIECE_ROYALQUEEN: return 16;
                case Piece.COLOUR_WHITE | Piece.PIECE_COMMONKING: return 17;

                case Piece.COLOUR_BLACK | Piece.PIECE_BRAWN: return 18;
                case Piece.COLOUR_BLACK | Piece.PIECE_UNICORN: return 19;
                case Piece.COLOUR_BLACK | Piece.PIECE_DRAGON: return 20;
                case Piece.COLOUR_BLACK | Piece.PIECE_PRINCESS: return 21;
                case Piece.COLOUR_BLACK | Piece.PIECE_ROYALQUEEN: return 22;
                case Piece.COLOUR_BLACK | Piece.PIECE_COMMONKING: return 23;
            }
        }

        public static bool isWhite(this GameColour colour) => colour == GameColour.WHITE;
        public static bool isBlack(this GameColour colour) => colour == GameColour.BLACK;

        public static GameColour getColour(this Piece piece) {
            if ((piece & Piece.MASK_COLOUR) == Piece.COLOUR_WHITE) {
                return GameColour.WHITE;
            } else {
                return GameColour.BLACK;
            }
        }
    }



    public class Move {
        public readonly Vector4i origin;
        public readonly Vector4i target;
        public readonly GameColour colour;

        public Move(Vector4i origin, Vector4i target, GameColour colour) {
            this.origin = origin;
            this.target = target;
            this.colour = colour;
        }

        public static bool operator ==(Move a, Move b) {
            return a.Equals(b);
        }

        public static bool operator !=(Move a, Move b) {
            return !a.Equals(b);
        }

        public override bool Equals(object obj) {
            return obj is Move move &&
                   origin == move.origin &&
                   target == move.target &&
                   colour == move.colour;
        }

        public override int GetHashCode() {
            int hashCode = -447757500;
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector4i>.Default.GetHashCode(origin);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector4i>.Default.GetHashCode(target);
            hashCode = hashCode * -1521134295 + colour.GetHashCode();
            return hashCode;
        }
    }

    public class IMove : Move {
        public IMove(Vector4i origin, Vector4i target, GameColour colour) : base(origin, target, colour) { }

        public Vector2i target_child;
        public Vector2i origin_child;
        public Piece captured;
        public Vector2i capture_target;
    }
}