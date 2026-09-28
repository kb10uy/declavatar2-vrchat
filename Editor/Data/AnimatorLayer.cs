using System;
using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public sealed class AnimatorLayer
    {
        public AnimatorLayer(
            string name,
            int? defaultState,
            IReadOnlyList<StateMachine> machines,
            IReadOnlyList<AnimatorState> states,
            IReadOnlyList<AnimatorTransition> transitions)
        {
            Name = name;
            DefaultState = defaultState;
            Machines = machines;
            States = states;
            Transitions = transitions;
        }

        public string Name { get; }
        public int? DefaultState { get; }
        public IReadOnlyList<StateMachine> Machines { get; }
        public IReadOnlyList<AnimatorState> States { get; }
        public IReadOnlyList<AnimatorTransition> Transitions { get; }
    }

    public sealed class StateMachine
    {
        public StateMachine(string name, int? parent, int? defaultState)
        {
            Name = name;
            Parent = parent;
            DefaultState = defaultState;
        }

        public string Name { get; }
        public int? Parent { get; }
        public int? DefaultState { get; }
    }

    public sealed class AnimatorState
    {
        public AnimatorState(
            string name,
            int? machine,
            Motion motion,
            double speed,
            string speedBy,
            string timeBy,
            bool writeDefaults,
            IReadOnlyList<Behavior> behaviors)
        {
            Name = name;
            Machine = machine;
            Motion = motion;
            Speed = speed;
            SpeedBy = speedBy;
            TimeBy = timeBy;
            WriteDefaults = writeDefaults;
            Behaviors = behaviors;
        }

        public string Name { get; }
        public int? Machine { get; }
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

    public enum TransitionSourceKind
    {
        Entry,
        State,
        MachineExit,
    }

    public readonly struct TransitionSource : IEquatable<TransitionSource>
    {
        private TransitionSource(TransitionSourceKind kind, int? stateIndex, int? machineIndex)
        {
            Kind = kind;
            StateIndex = stateIndex;
            MachineIndex = machineIndex;
        }

        public static TransitionSource Entry(int? machine)
        {
            return new TransitionSource(TransitionSourceKind.Entry, null, machine);
        }

        public static TransitionSource State(int index)
        {
            return new TransitionSource(TransitionSourceKind.State, index, null);
        }

        public static TransitionSource MachineExit(int machine)
        {
            return new TransitionSource(TransitionSourceKind.MachineExit, null, machine);
        }

        public TransitionSourceKind Kind { get; }
        public int? StateIndex { get; }
        public int? MachineIndex { get; }

        public bool Equals(TransitionSource other)
        {
            return Kind == other.Kind && StateIndex == other.StateIndex && MachineIndex == other.MachineIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionSource other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, StateIndex, MachineIndex);
        }

        public override string ToString()
        {
            return Kind switch
            {
                TransitionSourceKind.Entry => MachineIndex is int machine ? $"entry of machine #{machine}" : "entry",
                TransitionSourceKind.State => $"state #{StateIndex}",
                _ => $"exit of machine #{MachineIndex}",
            };
        }
    }

    public enum TransitionTargetKind
    {
        State,
        Exit,
        Machine,
    }

    public readonly struct TransitionTarget : IEquatable<TransitionTarget>
    {
        private TransitionTarget(TransitionTargetKind kind, int? stateIndex, int? machineIndex)
        {
            Kind = kind;
            StateIndex = stateIndex;
            MachineIndex = machineIndex;
        }

        public static TransitionTarget Exit => new TransitionTarget(TransitionTargetKind.Exit, null, null);

        public static TransitionTarget State(int index)
        {
            return new TransitionTarget(TransitionTargetKind.State, index, null);
        }

        public static TransitionTarget Machine(int index)
        {
            return new TransitionTarget(TransitionTargetKind.Machine, null, index);
        }

        public TransitionTargetKind Kind { get; }
        public int? StateIndex { get; }
        public int? MachineIndex { get; }

        public bool Equals(TransitionTarget other)
        {
            return Kind == other.Kind && StateIndex == other.StateIndex && MachineIndex == other.MachineIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is TransitionTarget other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Kind, StateIndex, MachineIndex);
        }

        public override string ToString()
        {
            return Kind switch
            {
                TransitionTargetKind.State => $"state #{StateIndex}",
                TransitionTargetKind.Machine => $"machine #{MachineIndex}",
                _ => "exit",
            };
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
