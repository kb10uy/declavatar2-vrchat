using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public enum BlendTreeType : byte
    {
        Linear = 0,
        Simple2d = 1,
        Freeform2d = 2,
        Cartesian2d = 3,
    }

    public abstract class Motion
    {
        private Motion()
        {
        }

        public sealed class InlineClip : Motion
        {
            public InlineClip(InlineAnimation animation)
            {
                Animation = animation;
            }

            public InlineAnimation Animation { get; }
        }

        public sealed class ExternalClip : Motion
        {
            public ExternalClip(AssetIndex asset)
            {
                Asset = asset;
            }

            public AssetIndex Asset { get; }
        }

        public sealed class ParametricTree : Motion
        {
            public ParametricTree(BlendTreeType treeType, string x, string y, IReadOnlyList<ParametricField> fields)
            {
                TreeType = treeType;
                X = x;
                Y = y;
                Fields = fields;
            }

            public BlendTreeType TreeType { get; }
            public string X { get; }
            public string Y { get; }
            public IReadOnlyList<ParametricField> Fields { get; }
        }

        public sealed class DirectTree : Motion
        {
            public DirectTree(IReadOnlyList<DirectField> fields)
            {
                Fields = fields;
            }

            public IReadOnlyList<DirectField> Fields { get; }
        }
    }

    public sealed class ParametricField
    {
        public ParametricField(double positionX, double positionY, double speed, Motion motion)
        {
            PositionX = positionX;
            PositionY = positionY;
            Speed = speed;
            Motion = motion;
        }

        public double PositionX { get; }
        public double PositionY { get; }
        public double Speed { get; }
        public Motion Motion { get; }
    }

    public sealed class DirectField
    {
        public DirectField(string weightBy, double speed, Motion motion)
        {
            WeightBy = weightBy;
            Speed = speed;
            Motion = motion;
        }

        public string WeightBy { get; }
        public double Speed { get; }
        public Motion Motion { get; }
    }
}
