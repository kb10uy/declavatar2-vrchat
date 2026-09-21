using System;
using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public sealed class AnimatorLayer
    {
        public AnimatorLayer(
            string name,
            int? defaultState,
            IReadOnlyList<AnimatorState> states,
            IReadOnlyList<AnimatorTransition> transitions)
        {
            Name = name;
            DefaultState = defaultState;
            States = states;
            Transitions = transitions;
        }

        public string Name { get; }
        public int? DefaultState { get; }
        public IReadOnlyList<AnimatorState> States { get; }
        public IReadOnlyList<AnimatorTransition> Transitions { get; }
    }

    public sealed class AnimatorState
    {
        public AnimatorState(
            string name,
            Motion motion,
            double speed,
            string speedBy,
            string timeBy,
            bool writeDefaults,
            IReadOnlyList<Behavior> behaviors)
        {
            Name = name;
            Motion = motion;
            Speed = speed;
            SpeedBy = speedBy;
            TimeBy = timeBy;
            WriteDefaults = writeDefaults;
            Behaviors = behaviors;
        }

        public string Name { get; }
        public Motion Motion { get; }
        public double Speed { get; }
        public string SpeedBy { get; }
        public string TimeBy { get; }
        public bool WriteDefaults { get; }
        public IReadOnlyList<Behavior> Behaviors { get; }
    }

    public sealed class AnimatorTransition
    {
        public AnimatorTransition(
            TransitionSource from,
            TransitionTarget to,
            double duration,
            IReadOnlyList<AnimatorCondition> conditions)
        {
            From = from;
            To = to;
            Duration = duration;
            Conditions = conditions;
        }

        public TransitionSource From { get; }
        public TransitionTarget To { get; }
        public double Duration { get; }
        public IReadOnlyList<AnimatorCondition> Conditions { get; }
    }

    public readonly struct TransitionSource : IEquatable<TransitionSource>
    {
        private TransitionSource(int? stateIndex)
        {
            StateIndex = stateIndex;
        }

        public static TransitionSource Entry => new TransitionSource(null);

        public static TransitionSource State(int index)
        {
            return new TransitionSource(index);
        }

        public int? StateIndex { get; }
        public bool IsEntry => StateIndex is null;

        public bool Equals(TransitionSource other)
        {
            return StateIndex == other.StateIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StateIndex ?? -1;
        }

        public override string ToString()
        {
            return StateIndex is int index ? $"state #{index}" : "entry";
        }
    }

    public readonly struct TransitionTarget : IEquatable<TransitionTarget>
    {
        private TransitionTarget(int? stateIndex)
        {
            StateIndex = stateIndex;
        }

        public static TransitionTarget Exit => new TransitionTarget(null);

        public static TransitionTarget State(int index)
        {
            return new TransitionTarget(index);
        }

        public int? StateIndex { get; }
        public bool IsExit => StateIndex is null;

        public bool Equals(TransitionTarget other)
        {
            return StateIndex == other.StateIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionTarget other && Equals(other);
        }

        public override int GetHashCode()
        {
            return StateIndex ?? -1;
        }

        public override string ToString()
        {
            return StateIndex is int index ? $"state #{index}" : "exit";
        }
    }

    public abstract class AnimatorCondition
    {
        private AnimatorCondition(string parameter)
        {
            Parameter = parameter;
        }

        public string Parameter { get; }

        public sealed class If : AnimatorCondition
        {
            public If(string parameter) : base(parameter)
            {
            }
        }

        public sealed class IfNot : AnimatorCondition
        {
            public IfNot(string parameter) : base(parameter)
            {
            }
        }

        public sealed class Equal : AnimatorCondition
        {
            public Equal(string parameter, long value) : base(parameter)
            {
                Value = value;
            }

            public long Value { get; }
        }

        public sealed class NotEqual : AnimatorCondition
        {
            public NotEqual(string parameter, long value) : base(parameter)
            {
                Value = value;
            }

            public long Value { get; }
        }

        public sealed class Greater : AnimatorCondition
        {
            public Greater(string parameter, double value) : base(parameter)
            {
                Value = value;
            }

            public double Value { get; }
        }

        public sealed class Less : AnimatorCondition
        {
            public Less(string parameter, double value) : base(parameter)
            {
                Value = value;
            }

            public double Value { get; }
        }
    }
}
