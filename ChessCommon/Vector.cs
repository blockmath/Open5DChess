using System;
using System.Collections.Generic;
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

    public class Vector4i {
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

        public Vector4i(Vector2i XY, Vector2i TL) {
            X = XY.X;
            Y = XY.Y;
            T = TL.X;
            L = TL.Y;
        }

        public Vector4i(Vector4i o) {
            X = o.X;
            Y = o.Y;
            T = o.T;
            L = o.L;
        }

        public Vector2i XY => new Vector2i(X, Y);
        public Vector2i TL => new Vector2i(T, L);

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

        public static bool operator ==(Vector4i a, Vector4i b) {
            return a.X == b.X && a.Y == b.Y && a.T == b.T && a.L == b.L;
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
}
