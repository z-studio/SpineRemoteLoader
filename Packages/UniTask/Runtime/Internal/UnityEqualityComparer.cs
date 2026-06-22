using System;
using System.Collections.Generic;
using UnityEngine;

namespace Cysharp.Threading.Tasks.Internal {
    internal static class UnityEqualityComparer {
        public static readonly IEqualityComparer<Vector2> Vector2 = new Vector2EqualityComparer();
        public static readonly IEqualityComparer<Vector3> Vector3 = new Vector3EqualityComparer();
        public static readonly IEqualityComparer<Vector4> Vector4 = new Vector4EqualityComparer();
        public static readonly IEqualityComparer<Color> Color = new ColorEqualityComparer();
        public static readonly IEqualityComparer<Color32> Color32 = new Color32EqualityComparer();
        public static readonly IEqualityComparer<Rect> Rect = new RectEqualityComparer();
        public static readonly IEqualityComparer<Bounds> Bounds = new BoundsEqualityComparer();
        public static readonly IEqualityComparer<Quaternion> Quaternion = new QuaternionEqualityComparer();

        private static readonly RuntimeTypeHandle s_Vector2Type = typeof(Vector2).TypeHandle;
        private static readonly RuntimeTypeHandle s_Vector3Type = typeof(Vector3).TypeHandle;
        private static readonly RuntimeTypeHandle s_Vector4Type = typeof(Vector4).TypeHandle;
        private static readonly RuntimeTypeHandle s_ColorType = typeof(Color).TypeHandle;
        private static readonly RuntimeTypeHandle s_Color32Type = typeof(Color32).TypeHandle;
        private static readonly RuntimeTypeHandle s_RectType = typeof(Rect).TypeHandle;
        private static readonly RuntimeTypeHandle s_BoundsType = typeof(Bounds).TypeHandle;
        private static readonly RuntimeTypeHandle s_QuaternionType = typeof(Quaternion).TypeHandle;

        public static readonly IEqualityComparer<Vector2Int> Vector2Int = new Vector2IntEqualityComparer();
        public static readonly IEqualityComparer<Vector3Int> Vector3Int = new Vector3IntEqualityComparer();
        public static readonly IEqualityComparer<RangeInt> RangeInt = new RangeIntEqualityComparer();
        public static readonly IEqualityComparer<RectInt> RectInt = new RectIntEqualityComparer();
        public static readonly IEqualityComparer<BoundsInt> BoundsInt = new BoundsIntEqualityComparer();

        private static readonly RuntimeTypeHandle s_Vector2IntType = typeof(Vector2Int).TypeHandle;
        private static readonly RuntimeTypeHandle s_Vector3IntType = typeof(Vector3Int).TypeHandle;
        private static readonly RuntimeTypeHandle s_RangeIntType = typeof(RangeInt).TypeHandle;
        private static readonly RuntimeTypeHandle s_RectIntType = typeof(RectInt).TypeHandle;
        private static readonly RuntimeTypeHandle s_BoundsIntType = typeof(BoundsInt).TypeHandle;

        private static class Cache<T> {
            public static readonly IEqualityComparer<T> Comparer;

            static Cache() {
                var comparer = GetDefaultHelper(typeof(T));

                if (comparer == null) {
                    Comparer = EqualityComparer<T>.Default;
                } else {
                    Comparer = (IEqualityComparer<T>)comparer;
                }
            }
        }

        public static IEqualityComparer<T> GetDefault<T>() {
            return Cache<T>.Comparer;
        }

        private static object GetDefaultHelper(Type type) {
            var t = type.TypeHandle;

            if (t.Equals(s_Vector2Type)) {
                return (object)UnityEqualityComparer.Vector2;
            }

            if (t.Equals(s_Vector3Type)) {
                return (object)UnityEqualityComparer.Vector3;
            }

            if (t.Equals(s_Vector4Type)) {
                return (object)UnityEqualityComparer.Vector4;
            }

            if (t.Equals(s_ColorType)) {
                return (object)UnityEqualityComparer.Color;
            }

            if (t.Equals(s_Color32Type)) {
                return (object)UnityEqualityComparer.Color32;
            }

            if (t.Equals(s_RectType)) {
                return (object)UnityEqualityComparer.Rect;
            }

            if (t.Equals(s_BoundsType)) {
                return (object)UnityEqualityComparer.Bounds;
            }

            if (t.Equals(s_QuaternionType)) {
                return (object)UnityEqualityComparer.Quaternion;
            }

            if (t.Equals(s_Vector2IntType)) {
                return (object)UnityEqualityComparer.Vector2Int;
            }

            if (t.Equals(s_Vector3IntType)) {
                return (object)UnityEqualityComparer.Vector3Int;
            }

            if (t.Equals(s_RangeIntType)) {
                return (object)UnityEqualityComparer.RangeInt;
            }

            if (t.Equals(s_RectIntType)) {
                return (object)UnityEqualityComparer.RectInt;
            }

            if (t.Equals(s_BoundsIntType)) {
                return (object)UnityEqualityComparer.BoundsInt;
            }

            return null;
        }

        private sealed class Vector2EqualityComparer : IEqualityComparer<Vector2> {
            public bool Equals(Vector2 self, Vector2 vector) {
                return self.x.Equals(vector.x) && self.y.Equals(vector.y);
            }

            public int GetHashCode(Vector2 obj) {
                return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2;
            }
        }

        private sealed class Vector3EqualityComparer : IEqualityComparer<Vector3> {
            public bool Equals(Vector3 self, Vector3 vector) {
                return self.x.Equals(vector.x) && self.y.Equals(vector.y) && self.z.Equals(vector.z);
            }

            public int GetHashCode(Vector3 obj) {
                return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2 ^ obj.z.GetHashCode() >> 2;
            }
        }

        private sealed class Vector4EqualityComparer : IEqualityComparer<Vector4> {
            public bool Equals(Vector4 self, Vector4 vector) {
                return self.x.Equals(vector.x)
                       && self.y.Equals(vector.y)
                       && self.z.Equals(vector.z)
                       && self.w.Equals(vector.w);
            }

            public int GetHashCode(Vector4 obj) {
                return obj.x.GetHashCode()
                       ^ obj.y.GetHashCode() << 2
                       ^ obj.z.GetHashCode() >> 2
                       ^ obj.w.GetHashCode() >> 1;
            }
        }

        private sealed class ColorEqualityComparer : IEqualityComparer<Color> {
            public bool Equals(Color self, Color other) {
                return self.r.Equals(other.r)
                       && self.g.Equals(other.g)
                       && self.b.Equals(other.b)
                       && self.a.Equals(other.a);
            }

            public int GetHashCode(Color obj) {
                return obj.r.GetHashCode()
                       ^ obj.g.GetHashCode() << 2
                       ^ obj.b.GetHashCode() >> 2
                       ^ obj.a.GetHashCode() >> 1;
            }
        }

        private sealed class RectEqualityComparer : IEqualityComparer<Rect> {
            public bool Equals(Rect self, Rect other) {
                return self.x.Equals(other.x)
                       && self.width.Equals(other.width)
                       && self.y.Equals(other.y)
                       && self.height.Equals(other.height);
            }

            public int GetHashCode(Rect obj) {
                return obj.x.GetHashCode()
                       ^ obj.width.GetHashCode() << 2
                       ^ obj.y.GetHashCode() >> 2
                       ^ obj.height.GetHashCode() >> 1;
            }
        }

        private sealed class BoundsEqualityComparer : IEqualityComparer<Bounds> {
            public bool Equals(Bounds self, Bounds vector) {
                return self.center.Equals(vector.center) && self.extents.Equals(vector.extents);
            }

            public int GetHashCode(Bounds obj) {
                return obj.center.GetHashCode() ^ obj.extents.GetHashCode() << 2;
            }
        }

        private sealed class QuaternionEqualityComparer : IEqualityComparer<Quaternion> {
            public bool Equals(Quaternion self, Quaternion vector) {
                return self.x.Equals(vector.x)
                       && self.y.Equals(vector.y)
                       && self.z.Equals(vector.z)
                       && self.w.Equals(vector.w);
            }

            public int GetHashCode(Quaternion obj) {
                return obj.x.GetHashCode()
                       ^ obj.y.GetHashCode() << 2
                       ^ obj.z.GetHashCode() >> 2
                       ^ obj.w.GetHashCode() >> 1;
            }
        }

        private sealed class Color32EqualityComparer : IEqualityComparer<Color32> {
            public bool Equals(Color32 self, Color32 vector) {
                return self.a.Equals(vector.a)
                       && self.r.Equals(vector.r)
                       && self.g.Equals(vector.g)
                       && self.b.Equals(vector.b);
            }

            public int GetHashCode(Color32 obj) {
                return obj.a.GetHashCode()
                       ^ obj.r.GetHashCode() << 2
                       ^ obj.g.GetHashCode() >> 2
                       ^ obj.b.GetHashCode() >> 1;
            }
        }

        private sealed class Vector2IntEqualityComparer : IEqualityComparer<Vector2Int> {
            public bool Equals(Vector2Int self, Vector2Int vector) {
                return self.x.Equals(vector.x) && self.y.Equals(vector.y);
            }

            public int GetHashCode(Vector2Int obj) {
                return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2;
            }
        }

        private sealed class Vector3IntEqualityComparer : IEqualityComparer<Vector3Int> {
            public static readonly Vector3IntEqualityComparer Default = new();

            public bool Equals(Vector3Int self, Vector3Int vector) {
                return self.x.Equals(vector.x) && self.y.Equals(vector.y) && self.z.Equals(vector.z);
            }

            public int GetHashCode(Vector3Int obj) {
                return obj.x.GetHashCode() ^ obj.y.GetHashCode() << 2 ^ obj.z.GetHashCode() >> 2;
            }
        }

        private sealed class RangeIntEqualityComparer : IEqualityComparer<RangeInt> {
            public bool Equals(RangeInt self, RangeInt vector) {
                return self.start.Equals(vector.start) && self.length.Equals(vector.length);
            }

            public int GetHashCode(RangeInt obj) {
                return obj.start.GetHashCode() ^ obj.length.GetHashCode() << 2;
            }
        }

        private sealed class RectIntEqualityComparer : IEqualityComparer<RectInt> {
            public bool Equals(RectInt self, RectInt other) {
                return self.x.Equals(other.x)
                       && self.width.Equals(other.width)
                       && self.y.Equals(other.y)
                       && self.height.Equals(other.height);
            }

            public int GetHashCode(RectInt obj) {
                return obj.x.GetHashCode()
                       ^ obj.width.GetHashCode() << 2
                       ^ obj.y.GetHashCode() >> 2
                       ^ obj.height.GetHashCode() >> 1;
            }
        }

        private sealed class BoundsIntEqualityComparer : IEqualityComparer<BoundsInt> {
            public bool Equals(BoundsInt self, BoundsInt vector) {
                return Vector3IntEqualityComparer.Default.Equals(self.position, vector.position)
                       && Vector3IntEqualityComparer.Default.Equals(self.size, vector.size);
            }

            public int GetHashCode(BoundsInt obj) {
                return Vector3IntEqualityComparer.Default.GetHashCode(obj.position)
                       ^ Vector3IntEqualityComparer.Default.GetHashCode(obj.size) << 2;
            }
        }
    }
}