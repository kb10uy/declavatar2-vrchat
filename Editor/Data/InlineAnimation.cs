using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public abstract class InlineAnimation
    {
        private InlineAnimation()
        {
        }

        public sealed class Fixed : InlineAnimation
        {
            public Fixed(IReadOnlyList<FixedEntry> entries)
            {
                Entries = entries;
            }

            public IReadOnlyList<FixedEntry> Entries { get; }
        }

        public sealed class Keyed : InlineAnimation
        {
            public Keyed(ClipAttributes attributes, IReadOnlyList<KeyedEntry> curves)
            {
                Attributes = attributes;
                Curves = curves;
            }

            public ClipAttributes Attributes { get; }
            public IReadOnlyList<KeyedEntry> Curves { get; }
        }
    }

    public sealed class FixedEntry
    {
        public FixedEntry(AnimatedTarget target, AnimatedValue value)
        {
            Target = target;
            Value = value;
        }

        public AnimatedTarget Target { get; }
        public AnimatedValue Value { get; }
    }

    public sealed class KeyedEntry
    {
        public KeyedEntry(AnimatedTarget target, Curve curve)
        {
            Target = target;
            Curve = curve;
        }

        public AnimatedTarget Target { get; }
        public Curve Curve { get; }
    }

    public sealed class ClipAttributes
    {
        public ClipAttributes(double length, bool loopTime, bool loopBlend, double cycleOffset)
        {
            Length = length;
            LoopTime = loopTime;
            LoopBlend = loopBlend;
            CycleOffset = cycleOffset;
        }

        public double Length { get; }
        public bool LoopTime { get; }
        public bool LoopBlend { get; }
        public double CycleOffset { get; }
    }

    public sealed class Curve
    {
        public Curve(Keyframe first, IReadOnlyList<Segment> rest)
        {
            First = first;
            Rest = rest;
        }

        public Keyframe First { get; }
        public IReadOnlyList<Segment> Rest { get; }
    }

    public sealed class Segment
    {
        public Segment(Interpolation interpolation, Keyframe keyframe)
        {
            Interpolation = interpolation;
            Keyframe = keyframe;
        }

        public Interpolation Interpolation { get; }
        public Keyframe Keyframe { get; }
    }

    public abstract class Interpolation
    {
        private Interpolation()
        {
        }

        public sealed class Constant : Interpolation
        {
        }

        public sealed class Linear : Interpolation
        {
        }

        public sealed class Bezier : Interpolation
        {
            public Bezier(double x1, double y1, double x2, double y2)
            {
                X1 = x1;
                Y1 = y1;
                X2 = x2;
                Y2 = y2;
            }

            public double X1 { get; }
            public double Y1 { get; }
            public double X2 { get; }
            public double Y2 { get; }
        }
    }

    public sealed class Keyframe
    {
        public Keyframe(double time, AnimatedValue value)
        {
            Time = time;
            Value = value;
        }

        public double Time { get; }
        public AnimatedValue Value { get; }
    }
}
