namespace KusakaFactory.Declavatar2.Data
{
    public enum AnimatedValueType : byte
    {
        Float = 0,
        Int = 1,
        Bool = 2,
        Vector2 = 3,
        Vector3 = 4,
        Vector4 = 5,
        Quaternion = 6,
        Color = 7,
        ObjectReference = 8,
    }

    public abstract class AnimatedValue
    {
        private AnimatedValue()
        {
        }

        public abstract AnimatedValueType Type { get; }

        public sealed class Float : AnimatedValue
        {
            public Float(double value)
            {
                Value = value;
            }

            public double Value { get; }

            public override AnimatedValueType Type => AnimatedValueType.Float;
        }

        public sealed class Int : AnimatedValue
        {
            public Int(long value)
            {
                Value = value;
            }

            public long Value { get; }

            public override AnimatedValueType Type => AnimatedValueType.Int;
        }

        public sealed class Bool : AnimatedValue
        {
            public Bool(bool value)
            {
                Value = value;
            }

            public bool Value { get; }

            public override AnimatedValueType Type => AnimatedValueType.Bool;
        }

        public sealed class Vector2 : AnimatedValue
        {
            public Vector2(double x, double y)
            {
                X = x;
                Y = y;
            }

            public double X { get; }
            public double Y { get; }

            public override AnimatedValueType Type => AnimatedValueType.Vector2;
        }

        public sealed class Vector3 : AnimatedValue
        {
            public Vector3(double x, double y, double z)
            {
                X = x;
                Y = y;
                Z = z;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }

            public override AnimatedValueType Type => AnimatedValueType.Vector3;
        }

        public sealed class Vector4 : AnimatedValue
        {
            public Vector4(double x, double y, double z, double w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }
            public double W { get; }

            public override AnimatedValueType Type => AnimatedValueType.Vector4;
        }

        public sealed class Quaternion : AnimatedValue
        {
            public Quaternion(double x, double y, double z, double w)
            {
                X = x;
                Y = y;
                Z = z;
                W = w;
            }

            public double X { get; }
            public double Y { get; }
            public double Z { get; }
            public double W { get; }

            public override AnimatedValueType Type => AnimatedValueType.Quaternion;
        }

        public sealed class Color : AnimatedValue
        {
            public Color(double r, double g, double b, double a)
            {
                R = r;
                G = g;
                B = b;
                A = a;
            }

            public double R { get; }
            public double G { get; }
            public double B { get; }
            public double A { get; }

            public override AnimatedValueType Type => AnimatedValueType.Color;
        }

        public sealed class ObjectReference : AnimatedValue
        {
            public ObjectReference(AssetIndex asset)
            {
                Asset = asset;
            }

            public AssetIndex Asset { get; }

            public override AnimatedValueType Type => AnimatedValueType.ObjectReference;
        }
    }
}
