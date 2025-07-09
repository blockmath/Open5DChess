using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ChessCommon {
    public class Vector2i {
        public int X;
        public int Y;

        public Vector2i(int x, int y) {
            X = x;
            Y = y;
        }

        public Vector2i(Vector2i o) {
            X = o.X;
            Y = o.Y;
        }

        public Vector2i(string s) {
            Y = 9 - (s[s.Length - 1] - '0');
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

        public static Vector2i operator *(Vector2i a, int b) {
            return new Vector2i((int)(a.X * b), (int)(a.Y * b));
        }

        public static Vector2i operator /(Vector2i a, Vector2i b) {
            return new Vector2i(a.X / b.X, a.Y / b.Y);
        }

        public static bool operator ==(Vector2i a, Vector2i b) {
            return !(a is null) && !(b is null) && a.X == b.X && a.Y == b.Y;
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
        public override string ToString() {
            return "<" + X + ", " + Y + ">";
        }
    }

    public class Vector2iTL : Vector2i {
        public GameColour colour;

        public int T => X;
        public int L => Y;

        public static readonly Vector2iTL ORIGIN_WHITE = new Vector2iTL(1, 0, GameColour.WHITE);

        public Vector2iTL(int x, int y, GameColour colour) : base(x, y) { 
            this.colour = colour;
        }

        public Vector2iTL(Vector2i vec, GameColour colour) : base(vec.X, vec.Y) {
            this.colour = colour;
        }

        public Vector2iTL(Vector2iTL o) : base(o) {
            colour = o.colour;
        }

        public Vector2iTL(string s, GameColour colour) : base(0, 0) {
            Y = int.Parse(s.Split(new char[] { 'T' }, 2)[0]);
            X = int.Parse(s.Split(new char[] { 'T' }, 2)[1]);
            this.colour = colour;
        }

        public Vector2iTL NextTurn() {
            if (colour.isWhite()) {
                return new Vector2iTL(this, GameColour.BLACK);
            } else {
                return new Vector2iTL(this, GameColour.WHITE) + new Vector2i(1, 0);
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

        public static Vector2iTL operator *(Vector2iTL a, Vector2i b) {
            throw new NotSupportedException("Vector2iTL is an absolute coordinate system. Use Vector2i for relative vector math.");
        }

        public static Vector2iTL operator *(Vector2iTL a, int b) {
            throw new NotSupportedException("Vector2iTL is an absolute coordinate system. Use Vector2i for relative vector math.");
        }

        public static Vector2iTL operator /(Vector2iTL a, Vector2i b) {
            throw new NotSupportedException("Vector2iTL is an absolute coordinate system. Use Vector2i for relative vector math.");
        }

        public static bool operator ==(Vector2iTL a, Vector2iTL b) {
            return !(a is null) && !(b is null) && a.X == b.X && a.Y == b.Y && a.colour == b.colour;
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
    }


    public class Vector4i {
        public static readonly Vector4i ZERO = new Vector4i(0, 0, 0, 0);

        public int X;
        public int Y;
        public int T;
        public int L;

        protected Vector4i() {
            X = Y = T = L = 0;
        }

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
            return (a is null && b is null) || (!(a is null) && !(b is null) && a.X == b.X && a.Y == b.Y && a.T == b.T && a.L == b.L);
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

    public class Vector4iTL : Vector4i {

        public GameColour colour;

        public Vector2i XY => new Vector2i(X, Y);
        public Vector2iTL TL => new Vector2iTL(T, L, colour);

        public Vector4iTL(int x, int y, int t, int l, GameColour colour) : base(x, y, t, l) {
            this.colour = colour;
        }

        protected Vector4iTL(Vector4i pos, GameColour colour) : base(pos.X, pos.Y, pos.T, pos.L) {
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
            if (b is Vector4iTL) {
                throw new NotSupportedException("Vector4iTL is an absolute coordinate system. Use Vector4i for relative vector math.");
            }
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


        public static Vector4iTL operator *(Vector4iTL a, Vector4i b) {
            throw new NotSupportedException("Vector4iTL is an absolute coordinate system. Use Vector4i for relative vector math.");
        }

        public static Vector4i operator *(Vector4iTL a, int b) {
            throw new NotSupportedException("Vector4iTL is an absolute coordinate system. Use Vector4i for relative vector math.");
        }

        public static Vector4i operator /(Vector4iTL a, Vector4i b) {
            throw new NotSupportedException("Vector4iTL is an absolute coordinate system. Use Vector4i for relative vector math.");
        }


        public static bool operator ==(Vector4iTL a, Vector4iTL b) {
            return (a is null && b is null) || (!(a is null) && !(b is null) && a.X == b.X && a.Y == b.Y && a.T == b.T && a.L == b.L && a.colour == b.colour);
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
    }
}
