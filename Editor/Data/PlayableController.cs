using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public enum PlayableLayer : byte
    {
        Base = 0,
        Additive = 1,
        Gesture = 2,
        Action = 3,
        Fx = 4,
        Sitting = 5,
        TPose = 6,
        IkPose = 7,
    }

    public enum MergeMode : byte
    {
        Append = 0,
        Replace = 1,
    }

    public enum PathMode : byte
    {
        Absolute = 0,
        Relative = 1,
    }

    public sealed class PlayableController
    {
        public PlayableController(
            PlayableLayer playable,
            MergeMode mode,
            int priority,
            PathMode pathMode,
            AssetIndex? mask,
            IReadOnlyList<AnimatorParameter> parameters,
            IReadOnlyList<AnimatorLayer> layers)
        {
            Playable = playable;
            Mode = mode;
            Priority = priority;
            PathMode = pathMode;
            Mask = mask;
            Parameters = parameters;
            Layers = layers;
        }

        public PlayableLayer Playable { get; }
        public MergeMode Mode { get; }
        public int Priority { get; }
        public PathMode PathMode { get; }
        public AssetIndex? Mask { get; }
        public IReadOnlyList<AnimatorParameter> Parameters { get; }
        public IReadOnlyList<AnimatorLayer> Layers { get; }
    }

    public sealed class AnimatorParameter
    {
        public AnimatorParameter(string name, AnimatorParameterKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public string Name { get; }
        public AnimatorParameterKind Kind { get; }
    }

    public abstract class AnimatorParameterKind
    {
        private AnimatorParameterKind()
        {
        }

        public abstract AnimatedValueType ValueType { get; }

        public sealed class Bool : AnimatorParameterKind
        {
            public Bool(bool? defaultValue)
            {
                Default = defaultValue;
            }

            public bool? Default { get; }

            public override AnimatedValueType ValueType => AnimatedValueType.Bool;
        }

        public sealed class Int : AnimatorParameterKind
        {
            public Int(int? defaultValue)
            {
                Default = defaultValue;
            }

            public int? Default { get; }

            public override AnimatedValueType ValueType => AnimatedValueType.Int;
        }

        public sealed class Float : AnimatorParameterKind
        {
            public Float(float? defaultValue)
            {
                Default = defaultValue;
            }

            public float? Default { get; }

            public override AnimatedValueType ValueType => AnimatedValueType.Float;
        }
    }
}
