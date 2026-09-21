using System;
using KusakaFactory.Declavatar2.Data;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal static class Values
    {
        private static readonly string[] ScalarSuffixes = { "" };
        private static readonly string[] Vector2Suffixes = { ".x", ".y" };
        private static readonly string[] Vector3Suffixes = { ".x", ".y", ".z" };
        private static readonly string[] Vector4Suffixes = { ".x", ".y", ".z", ".w" };
        private static readonly string[] ColorSuffixes = { ".r", ".g", ".b", ".a" };

        public static string[] Suffixes(AnimatedValueType type)
        {
            switch (type)
            {
                case AnimatedValueType.Float:
                case AnimatedValueType.Int:
                case AnimatedValueType.Bool:
                    return ScalarSuffixes;
                case AnimatedValueType.Vector2: return Vector2Suffixes;
                case AnimatedValueType.Vector3: return Vector3Suffixes;
                case AnimatedValueType.Vector4:
                case AnimatedValueType.Quaternion:
                    return Vector4Suffixes;
                case AnimatedValueType.Color: return ColorSuffixes;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, "not a numeric value type");
            }
        }

        public static float[] Components(AnimatedValue value)
        {
            switch (value)
            {
                case AnimatedValue.Float f: return new[] { (float)f.Value };
                case AnimatedValue.Int i: return new[] { (float)i.Value };
                case AnimatedValue.Bool b: return new[] { b.Value ? 1f : 0f };
                case AnimatedValue.Vector2 v: return new[] { (float)v.X, (float)v.Y };
                case AnimatedValue.Vector3 v: return new[] { (float)v.X, (float)v.Y, (float)v.Z };
                case AnimatedValue.Vector4 v: return new[] { (float)v.X, (float)v.Y, (float)v.Z, (float)v.W };
                case AnimatedValue.Quaternion q: return new[] { (float)q.X, (float)q.Y, (float)q.Z, (float)q.W };
                case AnimatedValue.Color c: return new[] { (float)c.R, (float)c.G, (float)c.B, (float)c.A };
                default: throw new ArgumentOutOfRangeException(nameof(value), value, "not a numeric value");
            }
        }

        public static float Scalar(AnimatedValue value)
        {
            switch (value)
            {
                case AnimatedValue.Float f: return (float)f.Value;
                case AnimatedValue.Int i: return i.Value;
                case AnimatedValue.Bool b: return b.Value ? 1f : 0f;
                default: throw new ArgumentOutOfRangeException(nameof(value), value, "not a scalar value");
            }
        }
    }
}
