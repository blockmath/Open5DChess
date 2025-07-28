using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ChessCommon {

    public struct Vector1i {
        public int X;

        public Vector1i(int x) {
            X = x;
        }
    }


    public struct Vector1iTL {
        public int X;
        public int T => X;

        public GameColour colour;

        public Vector1iTL(int x, GameColour colour) {
            X = x;
            this.colour = colour;
        }

        public Vector1iTL NextTurn() {
            if (colour.isWhite()) {
                return new Vector1iTL(X, GameColour.BLACK);
            } else {
                return new Vector1iTL(X + 1, GameColour.WHITE);
            }
        }
    }

    public struct Vector2i {
        public int X;
        public int Y;


        public static readonly Vector2i ZERO = new Vector2i(0, 0);
        public static readonly Vector2i AXIS_X = new Vector2i(1, 0);
        public static readonly Vector2i AXIS_Y = new Vector2i(0, 1);
        public static readonly Vector2i INFINITY = new Vector2i(int.MaxValue, int.MaxValue);

        public Vector2i(int x, int y) {
            X = x;
            Y = y;
        }

        public Vector2i(Vector2i o) {
            X = o.X;
            Y = o.Y;
        }

        public Vector2i(string s, BoundsInfo bi) {
            Y = (bi.BoardSize.Y + 1) - (s[s.Length - 1] - '0');
            X = s[s.Length - 2] - ('a' - 1);
        }

        public static Vector2i operator +(Vector2i a, Vector2i b) {
            return new Vector2i(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2i operator -(Vector2i a, Vector2i b) {
            return new Vector2i(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2i operator *(Vector2i a, Vector2i b) {
            return new Vector2i(a.X * b.X, a.Y * b.Y);
        }

        public static Vector2i operator *(Vector2i a, double b) {
            return new Vector2i((int)(a.X * b), (int)(a.Y * b));
        }

        public static Vector2i operator /(Vector2i a, Vector2i b) {
            return new Vector2i(a.X / b.X, a.Y / b.Y);
        }

        public Vector2i Min(Vector2i o) {
            return new Vector2i(Math.Min(X, o.X), Math.Min(Y, o.Y));
        }

        public Vector2i Max(Vector2i o) {
            return new Vector2i(Math.Max(X, o.X), Math.Max(Y, o.Y));
        }

        public static bool operator ==(Vector2i a, Vector2i b) {
            return a.X == b.X && a.Y == b.Y;
        }

        public static bool operator !=(Vector2i a, Vector2i b) {
            return !(a == b);
        }

        public override bool Equals(object obj) {
            return obj is Vector2i i &&
                   this == i;
        }

        public override int GetHashCode() {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }
        public string ToString(BoundsInfo bi) {
            return ((char)(('a' - 1) + X)).ToString() + ((char)('1' + (bi.BoardSize.Y - Y))).ToString();
        }
    }

    public struct Vector2iTL {

        public static readonly Vector2iTL Null = new Vector2iTL(int.MinValue, int.MinValue, GameColour.NONE);

        public GameColour colour;
        
        
        public int X;
        public int Y;

        public int T => X;
        public int L => Y;

        public Vector1iTL TC => new Vector1iTL(T, colour);

        public Vector2i vpos => new Vector2i(2 * X + (colour.isWhite() ? 0 : 1), Y);

        public static readonly Vector2iTL ORIGIN_WHITE = new Vector2iTL(1, 0, GameColour.WHITE);

        public Vector2iTL(int x, int y, GameColour colour) {
            X = x;
            Y = y;
            this.colour = colour;
        }

        public Vector2iTL(Vector2i vec, GameColour colour) {
            X = vec.X;
            Y = vec.Y;
            this.colour = colour;
        }

        public Vector2iTL(Vector2iTL o) {
            X = o.X;
            Y = o.Y;
            colour = o.colour;
        }

        public Vector2iTL(string s, GameColour colour) {
            X = int.Parse(s.Split(new char[] { 'T' }, 2)[1]);
            Y = int.Parse(s.Split(new char[] { 'T' }, 2)[0]);
            this.colour = colour;
        }

        public Vector2iTL NextTurn() {
            if (colour.isWhite()) {
                return new Vector2iTL(X, Y, GameColour.BLACK);
            } else {
                return new Vector2iTL(X, Y, GameColour.WHITE) + Vector2i.AXIS_X;
            }
        }
        public static Vector2iTL operator +(Vector2iTL a, Vector2i b) {
            return new Vector2iTL(a.X + b.X, a.Y + b.Y, a.colour);
        }

        public static Vector2iTL operator -(Vector2iTL a, Vector2i b) {
            return a + (b * -1);
        }

        public static Vector2i operator -(Vector2iTL a, Vector2iTL b) {
            if (a.colour != b.colour) {
                throw new NotSupportedException("Difference between vectors is not a vector");
            }

            return new Vector2i(a.X - b.X, a.Y - b.Y);
        }

        public static bool operator ==(Vector2iTL a, Vector2iTL b) {
            return a.X == b.X && a.Y == b.Y && a.colour == b.colour;
        }
        public static bool operator !=(Vector2iTL a, Vector2iTL b) {
            return !(a == b);
        }
        public override bool Equals(object obj) {
            return obj is Vector2iTL i &&
                   this == i;
        }
        public override int GetHashCode() {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + colour.GetHashCode();
            return hashCode;
        }

        public override string ToString() {
            return "<" + X + (colour.isWhite() ? "w" : "b") + ", " + Y + ">";
        }
    }


    public struct Vector4i {
        public static readonly Vector4i ZERO = new Vector4i(0, 0, 0, 0);

        public int X;
        public int Y;
        public int T;
        public int L;

        public Vector4i(int x, int y, int t, int l) {
            X = x;
            Y = y;
            T = t;
            L = l;
        }

        public Vector4i(Vector4i o) {
            X = o.X;
            Y = o.Y;
            T = o.T;
            L = o.L;
        }

        public static Vector4i operator +(Vector4i a, Vector4i b) {
            return new Vector4i(a.X + b.X, a.Y + b.Y, a.T + b.T, a.L + b.L);
        }

        public static Vector4i operator -(Vector4i a, Vector4i b) {
            return new Vector4i(a.X - b.X, a.Y - b.Y, a.T - b.T, a.L - b.L);
        }

        public static Vector4i operator *(Vector4i a, Vector4i b) {
            return new Vector4i(a.X * b.X, a.Y * b.Y, a.T * b.T, a.L * b.L);
        }

        public static Vector4i operator *(Vector4i a, int b) {
            return new Vector4i((int)(a.X * b), (int)(a.Y * b), (int)(a.T * b), (int)(a.L * b));
        }

        public static Vector4i operator /(Vector4i a, Vector4i b) {
            return new Vector4i(a.X / b.X, a.Y / b.Y, a.T / b.T, a.L / b.L);
        }

        public bool isUnit() {
            return (-1 <= X && X <= 1) && (-1 <= Y && Y <= 1) && (-1 <= T && T <= 1) && (-1 <= L && L <= 1);
        }

        public bool isOrthogonal() {
            return (X != 0 && Y == 0 && T == 0 && L == 0) || (X == 0 && Y != 0 && T == 0 && L == 0) || (X == 0 && Y == 0 && T != 0 && L == 0) || (X == 0 && Y == 0 && T == 0 && L != 0);
        }

        public Vector4i abs() {
            return new Vector4i(Math.Abs(X), Math.Abs(Y), Math.Abs(T), Math.Abs(L));
        }

        public static bool operator ==(Vector4i a, Vector4i b) {
            return (a.X == b.X && a.Y == b.Y && a.T == b.T && a.L == b.L);
        }

        public static bool operator !=(Vector4i a, Vector4i b) {
            return !(a == b);
        }

        public override bool Equals(object obj) {
            return obj is Vector4i i &&
                   this == i;
        }

        public override int GetHashCode() {
            int hashCode = 411555305;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + T.GetHashCode();
            hashCode = hashCode * -1521134295 + L.GetHashCode();
            return hashCode;
        }

        public override string ToString() {
            return "<" + X + ", " + Y + ", " + T + ", " + L + ">";
        }
    }

    public struct Vector4iTL {
        public static readonly Vector4iTL Null = new Vector4iTL(int.MinValue, int.MinValue, int.MinValue, int.MinValue, GameColour.NONE);

        public GameColour colour;

        public int X;
        public int Y;
        public int T;
        public int L;

        public Vector2i XY => new Vector2i(X, Y);
        public Vector2iTL TL => new Vector2iTL(T, L, colour);

        public Vector4iTL(int x, int y, int t, int l, GameColour colour) {
            X = x;
            Y = y;
            T = t;
            L = l;
            this.colour = colour;
        }

        public Vector4iTL(Vector4i pos, GameColour colour) {
            X = pos.X;
            Y = pos.Y;
            T = pos.T;
            L = pos.L;
            this.colour = colour;
        }

        public Vector4iTL(Vector2i XY, Vector2iTL TL) {
            X = XY.X;
            Y = XY.Y;
            T = TL.X;
            L = TL.Y;
            colour = TL.colour;
        }

        public Vector4iTL(Vector4iTL o) {
            X = o.X;
            Y = o.Y;
            T = o.T;
            L = o.L;
            colour = o.colour;
        }

        public static Vector4iTL operator +(Vector4iTL a, Vector4i b) {
            return new Vector4iTL(a.X + b.X, a.Y + b.Y, a.T + b.T, a.L + b.L, a.colour);
        }

        public static Vector4iTL operator -(Vector4iTL a, Vector4i b) {
            return a + (b * -1);
        }

        public static Vector4i operator -(Vector4iTL a, Vector4iTL b) {
            if (a.colour != b.colour) {
                throw new NotSupportedException("Difference between vectors is not a vector");
            }

            return new Vector4i(a.X - b.X, a.Y - b.Y, a.T - b.T, a.L - b.L);

        }

        public Vector4iTL NextTurn() {
            return new Vector4iTL(XY, TL.NextTurn());
        }


        public static bool operator ==(Vector4iTL a, Vector4iTL b) {
            return (a.X == b.X && a.Y == b.Y && a.T == b.T && a.L == b.L && a.colour == b.colour);
        }

        public static bool operator !=(Vector4iTL a, Vector4iTL b) {
            return !(a == b);
        }

        public override bool Equals(object obj) {
            return obj is Vector4iTL i &&
                   this == i;
        }

        public override int GetHashCode() {
            int hashCode = 411555305;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            hashCode = hashCode * -1521134295 + T.GetHashCode();
            hashCode = hashCode * -1521134295 + L.GetHashCode();
            hashCode = hashCode * -1521134295 + colour.GetHashCode();
            return hashCode;
        }

        public override string ToString() {
            return "<" + X + ", " + Y + ", " + T + (colour.isWhite() ? "" : ".5") + ", " + L + ">";
        }

        public Vector4iTL(string str) {
            Match match = Regex.Match(str, "<(\\d+), (\\d+), (\\d+)(\\.5)?, (\\d+)>");

            X = int.Parse(match.Groups[1].Value);
            Y = int.Parse(match.Groups[2].Value);
            T = int.Parse(match.Groups[3].Value);

            if (match.Groups[4].Value == ".5") {
                colour = GameColour.BLACK;
                L = int.Parse(match.Groups[5].Value);
            } else {
                colour = GameColour.WHITE;
                L = int.Parse(match.Groups[5].Value);
            }
        }
    }
}
