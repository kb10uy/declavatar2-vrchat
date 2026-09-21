namespace KusakaFactory.Declavatar2.Data
{
    public sealed class ExpressionParameter
    {
        public ExpressionParameter(string name, ExpressionParameterKind kind, bool saved, bool synced)
        {
            Name = name;
            Kind = kind;
            Saved = saved;
            Synced = synced;
        }

        public string Name { get; }
        public ExpressionParameterKind Kind { get; }
        public bool Saved { get; }
        public bool Synced { get; }
    }

    public abstract class ExpressionParameterKind
    {
        private ExpressionParameterKind()
        {
        }

        public abstract AnimatedValueType ValueType { get; }

        public sealed class Bool : ExpressionParameterKind
        {
            public Bool(bool? defaultValue)
            {
                Default = defaultValue;
            }

            public bool? Default { get; }

            public override AnimatedValueType ValueType => AnimatedValueType.Bool;
        }

        public sealed class Int : ExpressionParameterKind
        {
            public Int(byte? width, int? defaultValue)
            {
                Width = width;
                Default = defaultValue;
            }

            public byte? Width { get; }
            public int? Default { get; }

            public override AnimatedValueType ValueType => AnimatedValueType.Int;
        }

        public sealed class Float : ExpressionParameterKind
        {
            public Float(byte? width, float? defaultValue)
            {
                Width = width;
                Default = defaultValue;
            }

            public byte? Width { get; }
            public float? Default { get; }

            public override AnimatedValueType ValueType => AnimatedValueType.Float;
        }
    }
}
