using System;
using System.Collections.Generic;

namespace KusakaFactory.Declavatar2.Data
{
    public sealed class Externals
    {
        public Externals(
            IReadOnlyList<ExternEntry<string>> objectPaths,
            IReadOnlyList<ExternEntry<string>> componentTypes,
            IReadOnlyList<ComponentTypeUsage> componentTypeUsages,
            IReadOnlyList<ExternEntry<AssetLocator>> assets,
            bool needsRelativeRoot)
        {
            ObjectPaths = objectPaths;
            ComponentTypes = componentTypes;
            ComponentTypeUsages = componentTypeUsages;
            Assets = assets;
            NeedsRelativeRoot = needsRelativeRoot;
        }

        public IReadOnlyList<ExternEntry<string>> ObjectPaths { get; }
        public IReadOnlyList<ExternEntry<string>> ComponentTypes { get; }
        public IReadOnlyList<ComponentTypeUsage> ComponentTypeUsages { get; }
        public IReadOnlyList<ExternEntry<AssetLocator>> Assets { get; }
        public bool NeedsRelativeRoot { get; }

        public ExternEntry<string> this[ObjectPathIndex index] => ObjectPaths[index.Index];
        public ExternEntry<string> this[ComponentTypeIndex index] => ComponentTypes[index.Index];
        public ExternEntry<AssetLocator> this[AssetIndex index] => Assets[index.Index];
    }

    [Flags]
    public enum ComponentTypeUsage : byte
    {
        None = 0,
        AnimatedTarget = 1,
        StateBehaviour = 2,
    }

    public sealed class ExternEntry<T>
    {
        public ExternEntry(T value, IReadOnlyList<SourceLocation> referencedAt)
        {
            Value = value;
            ReferencedAt = referencedAt;
        }

        public T Value { get; }
        public IReadOnlyList<SourceLocation> ReferencedAt { get; }
    }

    public sealed class SourceLocation : IEquatable<SourceLocation>
    {
        public SourceLocation(string chunk, uint line)
        {
            Chunk = chunk;
            Line = line;
        }

        public string Chunk { get; }
        public uint Line { get; }

        public bool Equals(SourceLocation other)
        {
            return other is not null && Chunk == other.Chunk && Line == other.Line;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as SourceLocation);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Chunk, Line);
        }

        public override string ToString()
        {
            return $"{Chunk}:{Line}";
        }
    }

    public abstract class AssetLocator : IEquatable<AssetLocator>
    {
        private AssetLocator()
        {
        }

        public abstract bool Equals(AssetLocator other);

        public override bool Equals(object obj)
        {
            return Equals(obj as AssetLocator);
        }

        public abstract override int GetHashCode();

        public sealed class Guid : AssetLocator
        {
            public Guid(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public override bool Equals(AssetLocator other)
            {
                return other is Guid guid && Value == guid.Value;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(0, Value);
            }

            public override string ToString()
            {
                return $"guid {Value}";
            }
        }

        public sealed class Path : AssetLocator
        {
            public Path(string value)
            {
                Value = value;
            }

            public string Value { get; }

            public override bool Equals(AssetLocator other)
            {
                return other is Path path && Value == path.Value;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(1, Value);
            }

            public override string ToString()
            {
                return $"path {Value}";
            }
        }

        public sealed class Named : AssetLocator
        {
            public Named(string assetType, string name)
            {
                AssetType = assetType;
                Name = name;
            }

            public string AssetType { get; }
            public string Name { get; }

            public override bool Equals(AssetLocator other)
            {
                return other is Named named && AssetType == named.AssetType && Name == named.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(2, AssetType, Name);
            }

            public override string ToString()
            {
                return $"{AssetType} named {Name}";
            }
        }
    }

    public readonly struct ObjectPathIndex : IEquatable<ObjectPathIndex>
    {
        public ObjectPathIndex(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public bool Equals(ObjectPathIndex other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is ObjectPathIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public override string ToString()
        {
            return $"object path #{Index}";
        }
    }

    public readonly struct ComponentTypeIndex : IEquatable<ComponentTypeIndex>
    {
        public ComponentTypeIndex(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public bool Equals(ComponentTypeIndex other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentTypeIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public override string ToString()
        {
            return $"component type #{Index}";
        }
    }

    public readonly struct AssetIndex : IEquatable<AssetIndex>
    {
        public AssetIndex(int index)
        {
            Index = index;
        }

        public int Index { get; }

        public bool Equals(AssetIndex other)
        {
            return Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is AssetIndex other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Index;
        }

        public override string ToString()
        {
            return $"asset #{Index}";
        }
    }
}
