

using System.Collections.Generic;
using System.Security.Policy;
using System.Text.RegularExpressions;

namespace ChessCommon {

    public enum GameColour : int {
        WHITE = +1,
        BLACK = -1,
        NONE = 0
    }

    public enum Piece : byte {
        MASK_PIECE =                0b00111111,
        MASK_KIND =                 0b00011111,
        MASK_MOVABL =               0b00001111,
        MASK_SPEC =                 0b00010000,
        MASK_ROYAL =                0b00100000,
        MASK_COLOUR =               0b01000000,

        MOVABL_ROOK =               0b00000001,
        MOVABL_BISHOP =             0b00000010,
        MOVABL_UNICORN =            0b00000100,
        MOVABL_DRAGON =             0b00001000,

        MOVABL_SPEC_PAWN =          0b00000001,
        MOVABL_SPEC_BRAWN =         0b00000011,
        MOVABL_SPEC_KNIGHT =        0b00000100,
        MOVABL_SPEC_KING =          0b00001000,

        FLAG_ROYAL =                0b00100000,
        FLAG_SPEC =                 0b00010000,

        COLOUR_WHITE =              0b00000000,
        COLOUR_BLACK =              0b01000000,

        KIND_KING =                 FLAG_SPEC | MOVABL_SPEC_KING,

        PIECE_ROOK =                MOVABL_ROOK,
        PIECE_BISHOP =              MOVABL_BISHOP,
        PIECE_UNICORN =             MOVABL_UNICORN,
        PIECE_DRAGON =              MOVABL_DRAGON,

        PIECE_PAWN =                FLAG_SPEC | MOVABL_SPEC_PAWN,
        PIECE_KNIGHT =              FLAG_SPEC | MOVABL_SPEC_KNIGHT,
        PIECE_BRAWN =               FLAG_SPEC | MOVABL_SPEC_BRAWN,
        PIECE_COMMONKING =          KIND_KING,

        PIECE_PRINCESS =            MOVABL_ROOK | MOVABL_BISHOP,
        PIECE_QUEEN =               MOVABL_ROOK | MOVABL_BISHOP | MOVABL_UNICORN | MOVABL_DRAGON,

        PIECE_KING =                FLAG_ROYAL | KIND_KING,
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

    public enum ColourRights : byte {
        NONE = 0b00,
        WHITE = 0b01,
        BLACK = 0b10,
        BOTH = WHITE | BLACK
    }

    public static class Methods {

        public static ColourRights GetRights(this GameColour colour) {
            switch (colour) {
                case GameColour.NONE: default: return ColourRights.NONE;
                case GameColour.WHITE: return ColourRights.WHITE;
                case GameColour.BLACK: return ColourRights.BLACK;
            }
        }

        public static int TVis(int T, GameColour colour) {
            return (T * 2 + (colour.isBlack() ? 1 : 0));
        }

        public static int TVis(Vector2iTL TL) => TVis(TL.X, TL.colour);

        public static int TVis(Vector4iTL v) => TVis(v.TL);

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

        public static int GetPieceID(this Piece piece) {
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

        public static string GetPgnChar(this Piece piece) {
            switch (piece & Piece.MASK_PIECE) {
                case Piece.PIECE_PAWN: return "";
                case Piece.PIECE_KNIGHT: return "N";
                case Piece.PIECE_BISHOP: return "B";
                case Piece.PIECE_ROOK: return "R";
                case Piece.PIECE_QUEEN: return "Q";
                case Piece.PIECE_KING: return "K";

                case Piece.PIECE_BRAWN: return "W";
                case Piece.PIECE_UNICORN: return "U";
                case Piece.PIECE_DRAGON: return "D";
                case Piece.PIECE_PRINCESS: return "S";
                case Piece.PIECE_ROYALQUEEN: return "Y";
                case Piece.PIECE_COMMONKING: return "C";

                default: return "";
            }
        }

        public static bool isWhite(this GameColour colour) => colour == GameColour.WHITE;
        public static bool isBlack(this GameColour colour) => colour == GameColour.BLACK;
        public static bool isNone(this GameColour colour) => colour == GameColour.NONE;


        public static bool hasWhite(this ColourRights rights) => rights.hasRights(GameColour.WHITE);
        public static bool hasBlack(this ColourRights rights) => rights.hasRights(GameColour.BLACK);
        public static bool hasNone(this ColourRights rights) => !(rights.hasWhite() || rights.hasBlack());

        public static GameColour inverse(this GameColour colour) => (GameColour)(-(int)colour);

        public static bool hasRights(this ColourRights rights, GameColour colour) => colour.isWhite() ? (rights & ColourRights.WHITE) != 0 : (rights & ColourRights.BLACK) != 0;

        public static GameColour getColour(this Piece piece) {
            if (piece == Piece.NONE) return 0;


            if ((piece & Piece.MASK_COLOUR) == Piece.COLOUR_WHITE) {
                return GameColour.WHITE;
            } else {
                return GameColour.BLACK;
            }
        }

        public static bool IsCastles(this MoveSpec spec) {
            return spec == MoveSpec.CastlesWK || spec == MoveSpec.CastlesBK || spec == MoveSpec.CastlesWQ || spec == MoveSpec.CastlesBQ;
        }

        public static bool IsPromotion(this MoveSpec spec) {
            return spec == MoveSpec.PromoteQueen || spec == MoveSpec.PromotePrincess || spec == MoveSpec.PromoteKnight || spec == MoveSpec.PromoteRook || spec == MoveSpec.PromoteBishop || spec == MoveSpec.PromoteUnicorn || spec == MoveSpec.PromoteDragon;
        }

        public static MoveSpec GetPromotionSpec(this Piece piece) {
            switch (piece & Piece.MASK_KIND) {
                case Piece.PIECE_PAWN:
                    return MoveSpec.PromotePawn;
                case Piece.PIECE_KNIGHT:
                    return MoveSpec.PromoteKnight;
                case Piece.PIECE_ROOK:
                    return MoveSpec.PromoteRook;
                case Piece.PIECE_BISHOP:
                    return MoveSpec.PromoteBishop;
                case Piece.PIECE_UNICORN:
                    return MoveSpec.PromoteUnicorn;
                case Piece.PIECE_DRAGON:
                    return MoveSpec.PromoteDragon;
                case Piece.PIECE_PRINCESS:
                    return MoveSpec.PromotePrincess;
                case Piece.PIECE_QUEEN:
                    return MoveSpec.PromoteQueen;
                case Piece.PIECE_KING:
                    return MoveSpec.PromoteCommonKing;
                case Piece.PIECE_BRAWN:
                    return MoveSpec.PromoteBrawn;
                default:
                    return MoveSpec.None;
            }
        }
    }

    public enum MoveSpec : byte {
        None = 0,

        PromotePawn,
        PromoteKnight,
        PromoteRook,
        PromoteBishop,
        PromoteUnicorn,
        PromoteDragon,
        PromotePrincess,
        PromoteQueen,
        PromoteCommonKing,
        PromoteBrawn,

        DoublePush,
        EnPassant,

        ForceSkipTurn,

        CastlesWK,
        CastlesWQ,
        CastlesBK,
        CastlesBQ
    }


    public class Move {
        public static Move ForceSkipTurn(Vector2iTL TL) => new Move(new Vector4iTL(Vector2i.ZERO, TL), new Vector4iTL(Vector2i.ZERO, TL), MoveSpec.ForceSkipTurn);


        public readonly Vector4iTL origin;
        public readonly Vector4iTL target;
        public readonly MoveSpec spec;

        public Move(Vector4iTL origin, Vector4iTL target, MoveSpec spec = MoveSpec.None) {
            this.origin = origin;
            this.target = target;
            this.spec = spec;
        }

        public GameColour getColour() {
            return origin.colour;
        }

        public bool isTravel() {
            return origin.TL != target.TL;
        }
        public static List<Vector4iTL> GetTargets(List<Move> moves) {
            List<Vector4iTL> targets = new List<Vector4iTL>();
            foreach (Move move in moves) {
                targets.Add(move.target);
            }
            return targets;
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
                   spec == move.spec;
        }

        public override int GetHashCode() {
            int hashCode = -447757500;
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector4iTL>.Default.GetHashCode(origin);
            hashCode = hashCode * -1521134295 + EqualityComparer<Vector4iTL>.Default.GetHashCode(target);
            hashCode = hashCode * -1521134295 + EqualityComparer<MoveSpec>.Default.GetHashCode(spec);
            return hashCode;
        }

        public override string ToString() {
            return "{[" + origin.ToString() + "][" + target.ToString() + "][" + spec.ToString() + "]}";
        }

        public Move(string str) {
            Match match = Regex.Match(str, "{\\[(.*?)]\\[(.*?)]\\[(.*?)]}");

            origin = new Vector4iTL(match.Groups[1].Value);
            target = new Vector4iTL(match.Groups[2].Value);
            MoveSpec.TryParse(match.Groups[3].Value, out spec);
        }
    }

    public class IMove : Move {
        public IMove(Move move) : base(move.origin, move.target, move.spec) {}

        public Vector2iTL target_child;
        public Vector2iTL origin_child;
        public Piece captured;
        public Vector2i capture_target;
    }
}