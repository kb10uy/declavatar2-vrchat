using System;

namespace KusakaFactory.Declavatar2.Data
{
    public abstract class AnimatedTarget : IEquatable<AnimatedTarget>
    {
        private AnimatedTarget()
        {
        }

        public abstract bool Equals(AnimatedTarget other);

        public override bool Equals(object obj)
        {
            return Equals(obj as AnimatedTarget);
        }

        public abstract override int GetHashCode();

        public sealed class AnimatorSelf : AnimatedTarget
        {
            public AnimatorSelf(AnimatorProperty property)
            {
                Property = property;
            }

            public AnimatorProperty Property { get; }

            public override bool Equals(AnimatedTarget other)
            {
                return other is AnimatorSelf target && Property.Equals(target.Property);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(0, Property);
            }

            public override string ToString()
            {
                return $"animator {Property}";
            }
        }

        public sealed class GameObject : AnimatedTarget
        {
            public GameObject(ObjectPathIndex path, GameObjectProperty property)
            {
                Path = path;
                Property = property;
            }

            public ObjectPathIndex Path { get; }
            public GameObjectProperty Property { get; }

            public override bool Equals(AnimatedTarget other)
            {
                return other is GameObject target && Path.Equals(target.Path) && Property == target.Property;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(1, Path, Property);
            }

            public override string ToString()
            {
                return $"{Path} {Property}";
            }
        }

        public sealed class Renderer : AnimatedTarget
        {
            public Renderer(ObjectPathIndex path, string rendererType, RendererProperty property)
            {
                Path = path;
                RendererType = rendererType;
                Property = property;
            }

            public ObjectPathIndex Path { get; }
            public string RendererType { get; }
            public RendererProperty Property { get; }

            public override bool Equals(AnimatedTarget other)
            {
                return other is Renderer target
                    && Path.Equals(target.Path)
                    && RendererType == target.RendererType
                    && Property.Equals(target.Property);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(2, Path, RendererType, Property);
            }

            public override string ToString()
            {
                return $"{Path} {RendererType} {Property}";
            }
        }

        public sealed class Component : AnimatedTarget
        {
            public Component(ObjectPathIndex path, ComponentTypeIndex componentType, ComponentProperty property, AnimatedValueType valueType)
            {
                Path = path;
                ComponentType = componentType;
                Property = property;
                ValueType = valueType;
            }

            public ObjectPathIndex Path { get; }
            public ComponentTypeIndex ComponentType { get; }
            public ComponentProperty Property { get; }
            public AnimatedValueType ValueType { get; }

            public override bool Equals(AnimatedTarget other)
            {
                return other is Component target
                    && Path.Equals(target.Path)
                    && ComponentType.Equals(target.ComponentType)
                    && Property.Equals(target.Property)
                    && ValueType == target.ValueType;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(3, Path, ComponentType, Property, ValueType);
            }

            public override string ToString()
            {
                return $"{Path} {ComponentType} {Property} ({ValueType})";
            }
        }
    }

    public abstract class AnimatorProperty : IEquatable<AnimatorProperty>
    {
        private AnimatorProperty()
        {
        }

        public abstract bool Equals(AnimatorProperty other);

        public override bool Equals(object obj)
        {
            return Equals(obj as AnimatorProperty);
        }

        public abstract override int GetHashCode();

        public sealed class ParameterFloatValue : AnimatorProperty
        {
            public ParameterFloatValue(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override bool Equals(AnimatorProperty other)
            {
                return other is ParameterFloatValue property && Name == property.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(0, Name);
            }

            public override string ToString()
            {
                return $"parameter {Name}";
            }
        }
    }

    public enum GameObjectProperty : byte
    {
        Active = 0,
        TransformPosition = 1,
        TransformRotationQuaternion = 2,
        TransformRotationEuler = 3,
        TransformScale = 4,
    }

    public abstract class RendererProperty : IEquatable<RendererProperty>
    {
        private RendererProperty()
        {
        }

        public abstract bool Equals(RendererProperty other);

        public override bool Equals(object obj)
        {
            return Equals(obj as RendererProperty);
        }

        public abstract override int GetHashCode();

        public sealed class Enabled : RendererProperty
        {
            public override bool Equals(RendererProperty other)
            {
                return other is Enabled;
            }

            public override int GetHashCode()
            {
                return 0;
            }

            public override string ToString()
            {
                return "enabled";
            }
        }

        public sealed class BlendShape : RendererProperty
        {
            public BlendShape(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override bool Equals(RendererProperty other)
            {
                return other is BlendShape property && Name == property.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(1, Name);
            }

            public override string ToString()
            {
                return $"blend shape {Name}";
            }
        }

        public sealed class Material : RendererProperty
        {
            public Material(uint slot)
            {
                Slot = slot;
            }

            public uint Slot { get; }

            public override bool Equals(RendererProperty other)
            {
                return other is Material property && Slot == property.Slot;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(2, Slot);
            }

            public override string ToString()
            {
                return $"material slot {Slot}";
            }
        }

        public sealed class MaterialProperty : RendererProperty
        {
            public MaterialProperty(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override bool Equals(RendererProperty other)
            {
                return other is MaterialProperty property && Name == property.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(3, Name);
            }

            public override string ToString()
            {
                return $"material property {Name}";
            }
        }

        public sealed class Serialized : RendererProperty
        {
            public Serialized(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override bool Equals(RendererProperty other)
            {
                return other is Serialized property && Name == property.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(4, Name);
            }

            public override string ToString()
            {
                return $"field {Name}";
            }
        }
    }

    public abstract class ComponentProperty : IEquatable<ComponentProperty>
    {
        private ComponentProperty()
        {
        }

        public abstract bool Equals(ComponentProperty other);

        public override bool Equals(object obj)
        {
            return Equals(obj as ComponentProperty);
        }

        public abstract override int GetHashCode();

        public sealed class Enabled : ComponentProperty
        {
            public override bool Equals(ComponentProperty other)
            {
                return other is Enabled;
            }

            public override int GetHashCode()
            {
                return 0;
            }

            public override string ToString()
            {
                return "enabled";
            }
        }

        public sealed class Serialized : ComponentProperty
        {
            public Serialized(string name)
            {
                Name = name;
            }

            public string Name { get; }

            public override bool Equals(ComponentProperty other)
            {
                return other is Serialized property && Name == property.Name;
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(1, Name);
            }

            public override string ToString()
            {
                return $"field {Name}";
            }
        }
    }
}
