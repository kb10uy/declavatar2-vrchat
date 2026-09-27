using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public abstract class Behavior
    {
        private Behavior()
        {
        }

        public sealed class ParameterDrive : Behavior
        {
            public ParameterDrive(ParameterDriveTarget target)
            {
                Target = target;
            }

            public ParameterDriveTarget Target { get; }
        }

        public sealed class TrackingControl : Behavior
        {
            public const int TargetCount = 10;

            private readonly TrackingControlMode[] _modes;

            public TrackingControl(TrackingControlMode[] modes)
            {
                _modes = modes;
            }

            public TrackingControlMode this[TrackingControlTarget target] => _modes[(int)target];

            public IReadOnlyList<TrackingControlMode> Modes => _modes;
        }

        public sealed class Generic : Behavior
        {
            public Generic(ComponentTypeIndex type, IReadOnlyDictionary<string, GenericValue> fields)
            {
                Type = type;
                Fields = fields;
            }

            public ComponentTypeIndex Type { get; }
            public IReadOnlyDictionary<string, GenericValue> Fields { get; }
        }

        public sealed class LayerControl : Behavior
        {
            public LayerControl(LayerRef layer, double goalWeight, double blendDuration)
            {
                Layer = layer;
                GoalWeight = goalWeight;
                BlendDuration = blendDuration;
            }

            public LayerRef Layer { get; }
            public double GoalWeight { get; }
            public double BlendDuration { get; }
        }

        public sealed class LocomotionControl : Behavior
        {
            public LocomotionControl(bool disableLocomotion)
            {
                DisableLocomotion = disableLocomotion;
            }

            public bool DisableLocomotion { get; }
        }

        public sealed class TemporaryPoseSpace : Behavior
        {
            public TemporaryPoseSpace(bool enter, bool fixedDelay, double delay)
            {
                Enter = enter;
                FixedDelay = fixedDelay;
                Delay = delay;
            }

            public bool Enter { get; }
            public bool FixedDelay { get; }
            public double Delay { get; }
        }

        public sealed class PlayableLayerControl : Behavior
        {
            public PlayableLayerControl(BlendablePlayable playable, double goalWeight, double blendDuration)
            {
                Playable = playable;
                GoalWeight = goalWeight;
                BlendDuration = blendDuration;
            }

            public BlendablePlayable Playable { get; }
            public double GoalWeight { get; }
            public double BlendDuration { get; }
        }
    }

    public enum BlendablePlayable : byte
    {
        Action = 0,
        Fx = 1,
        Gesture = 2,
        Additive = 3,
    }

    public readonly struct LayerRef
    {
        public LayerRef(int controller, int layer)
        {
            Controller = controller;
            Layer = layer;
        }

        public int Controller { get; }
        public int Layer { get; }

        public override string ToString()
        {
            return $"layer {Layer} of controller {Controller}";
        }
    }

    public abstract class ParameterDriveTarget
    {
        private ParameterDriveTarget()
        {
        }

        public sealed class Set : ParameterDriveTarget
        {
            public Set(string parameter, AnimatedValue value)
            {
                Parameter = parameter;
                Value = value;
            }

            public string Parameter { get; }
            public AnimatedValue Value { get; }
        }

        public sealed class Add : ParameterDriveTarget
        {
            public Add(string parameter, AnimatedValue value)
            {
                Parameter = parameter;
                Value = value;
            }

            public string Parameter { get; }
            public AnimatedValue Value { get; }
        }

        public sealed class RandomInt : ParameterDriveTarget
        {
            public RandomInt(string parameter, long min, long max)
            {
                Parameter = parameter;
                Min = min;
                Max = max;
            }

            public string Parameter { get; }
            public long Min { get; }
            public long Max { get; }
        }

        public sealed class RandomBool : ParameterDriveTarget
        {
            public RandomBool(string parameter, double chance)
            {
                Parameter = parameter;
                Chance = chance;
            }

            public string Parameter { get; }
            public double Chance { get; }
        }

        public sealed class RandomFloat : ParameterDriveTarget
        {
            public RandomFloat(string parameter, double min, double max)
            {
                Parameter = parameter;
                Min = min;
                Max = max;
            }

            public string Parameter { get; }
            public double Min { get; }
            public double Max { get; }
        }

        public sealed class Copy : ParameterDriveTarget
        {
            public Copy(string from, string to)
            {
                From = from;
                To = to;
            }

            public string From { get; }
            public string To { get; }
        }

        public sealed class RangedCopy : ParameterDriveTarget
        {
            public RangedCopy(string from, double fromMin, double fromMax, string to, double toMin, double toMax)
            {
                From = from;
                FromMin = fromMin;
                FromMax = fromMax;
                To = to;
                ToMin = toMin;
                ToMax = toMax;
            }

            public string From { get; }
            public double FromMin { get; }
            public double FromMax { get; }
            public string To { get; }
            public double ToMin { get; }
            public double ToMax { get; }
        }
    }

    public enum TrackingControlTarget : byte
    {
        Head = 0,
        LeftHand = 1,
        RightHand = 2,
        Hip = 3,
        LeftFoot = 4,
        RightFoot = 5,
        LeftFingers = 6,
        RightFingers = 7,
        Eyes = 8,
        Mouth = 9,
    }

    public enum TrackingControlMode : byte
    {
        NoChange = 0,
        Tracking = 1,
        Animation = 2,
    }

    public abstract class GenericValue
    {
        private GenericValue()
        {
        }

        public sealed class Bool : GenericValue
        {
            public Bool(bool value)
            {
                Value = value;
            }

            public bool Value { get; }
        }

        public sealed class Int : GenericValue
        {
            public Int(long value)
            {
                Value = value;
            }

            public long Value { get; }
        }

        public sealed class Float : GenericValue
        {
            public Float(double value)
            {
                Value = value;
            }

            public double Value { get; }
        }

        public sealed class String : GenericValue
        {
            public String(string value)
            {
                Value = value;
            }

            public string Value { get; }
        }

        public sealed class List : GenericValue
        {
            public List(IReadOnlyList<GenericValue> values)
            {
                Values = values;
            }

            public IReadOnlyList<GenericValue> Values { get; }
        }

        public sealed class Map : GenericValue
        {
            public Map(IReadOnlyDictionary<string, GenericValue> values)
            {
                Values = values;
            }

            public IReadOnlyDictionary<string, GenericValue> Values { get; }
        }
    }
}
