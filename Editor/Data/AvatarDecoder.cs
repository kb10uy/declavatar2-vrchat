using System;
using System.Collections.Generic;
using System.Linq;

namespace KusakaFactory.Declavatar2.Data
{
    internal static class AvatarDecoder
    {
        public static Avatar ReadAvatar(BlobReader reader)
        {
            var objectPaths = ReadExternTable(reader, "object path", static r => r.ReadString());
            var componentTypes = ReadExternTable(reader, "component type", static r => r.ReadString());
            var assets = ReadExternTable(reader, "asset", ReadAssetLocator);
            var needsRelativeRoot = reader.ReadBool();
            reader.ObjectPathCount = objectPaths.Count;
            reader.ComponentTypeUsages = new ComponentTypeUsage[componentTypes.Count];
            reader.AssetCount = assets.Count;

            var expressionParameters = reader.ReadList(ReadExpressionParameter);
            var controllers = reader.ReadList(ReadPlayableController);

            var scope = new Dictionary<string, AnimatedValueType>();
            foreach (var parameter in expressionParameters)
            {
                AddParameter(scope, parameter.Name, parameter.Kind.ValueType);
            }
            foreach (var controller in controllers)
            {
                foreach (var parameter in controller.Parameters)
                {
                    AddParameter(scope, parameter.Name, parameter.Kind.ValueType);
                }
            }
            reader.Parameters = scope;
            var menu = reader.ReadList(ReadMenuItem);
            CheckLayerControls(controllers);

            var externals = new Externals(objectPaths, componentTypes, reader.ComponentTypeUsages, assets, needsRelativeRoot);
            return new Avatar(externals, expressionParameters, controllers, menu);
        }

        private static void CheckLayerControls(IReadOnlyList<PlayableController> controllers)
        {
            foreach (var holder in controllers)
            {
                var controls = holder.Layers
                    .SelectMany(layer => layer.States)
                    .SelectMany(state => state.Behaviors)
                    .OfType<Behavior.LayerControl>();
                foreach (var control in controls)
                {
                    if (!holder.Playable.IsBlendable())
                    {
                        throw new BlobDecodeException($"a layer control is held by a {holder.Playable} controller, whose layers cannot be controlled");
                    }
                    var reference = control.Layer;
                    if (reference.Controller >= controllers.Count)
                    {
                        throw new BlobDecodeException($"controller index {reference.Controller} is out of range for an avatar of {controllers.Count} controllers");
                    }
                    var target = controllers[reference.Controller];
                    if (target.Playable != holder.Playable)
                    {
                        throw new BlobDecodeException($"a layer control in a {holder.Playable} controller refers to controller {reference.Controller}, which is {target.Playable}");
                    }
                    if (reference.Layer >= target.Layers.Count)
                    {
                        throw new BlobDecodeException($"layer index {reference.Layer} is out of range for controller {reference.Controller} of {target.Layers.Count} layers");
                    }
                }
            }
        }

        private static byte ReadDiscriminator(BlobReader reader, string typeName, int variantCount)
        {
            var value = reader.ReadU8();
            if (value >= variantCount)
            {
                throw reader.InvalidDiscriminator(typeName, value);
            }
            return value;
        }

        private static List<ExternEntry<T>> ReadExternTable<T>(BlobReader reader, string kind, Func<BlobReader, T> readValue)
        {
            var entries = reader.ReadList(r =>
            {
                var value = readValue(r);
                var referencedAt = r.ReadList(ReadSourceLocation);
                return new ExternEntry<T>(value, referencedAt);
            });
            var seen = new HashSet<T>();
            foreach (var entry in entries)
            {
                if (!seen.Add(entry.Value))
                {
                    throw new BlobDecodeException($"{kind} {entry.Value} appears more than once");
                }
            }
            return entries;
        }

        private static SourceLocation ReadSourceLocation(BlobReader reader)
        {
            var chunk = reader.ReadString();
            var line = reader.ReadU32();
            return new SourceLocation(chunk, line);
        }

        private static AssetLocator ReadAssetLocator(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new AssetLocator.Guid(reader.ReadString());
                case 1:
                    return new AssetLocator.Path(reader.ReadString());
                case 2:
                {
                    var assetType = reader.ReadString();
                    var name = reader.ReadString();
                    return new AssetLocator.Named(assetType, name);
                }
                default:
                    throw reader.InvalidDiscriminator("AssetLocator", tag);
            }
        }

        private static int ReadExternIndex(BlobReader reader, string kind, int count)
        {
            var index = reader.ReadU32();
            if (index >= (uint)count)
            {
                throw new BlobDecodeException($"{kind} index {index} is out of range for a table of {count} entries");
            }
            return (int)index;
        }

        private static ObjectPathIndex ReadObjectPath(BlobReader reader)
        {
            return new ObjectPathIndex(ReadExternIndex(reader, "object path", reader.ObjectPathCount));
        }

        private static ComponentTypeIndex ReadComponentType(BlobReader reader, ComponentTypeUsage usage)
        {
            var index = ReadExternIndex(reader, "component type", reader.ComponentTypeUsages.Length);
            reader.ComponentTypeUsages[index] |= usage;
            return new ComponentTypeIndex(index);
        }

        private static AssetIndex ReadAsset(BlobReader reader)
        {
            return new AssetIndex(ReadExternIndex(reader, "asset", reader.AssetCount));
        }

        private static string ReadParameter(BlobReader reader)
        {
            var name = reader.ReadString();
            if (!reader.Parameters.ContainsKey(name))
            {
                throw new BlobDecodeException($"parameter `{name}` is not in the animator parameter list");
            }
            return name;
        }

        private static void AddParameter(Dictionary<string, AnimatedValueType> scope, string name, AnimatedValueType valueType)
        {
            if (scope.TryGetValue(name, out var first))
            {
                if (first != valueType)
                {
                    throw new BlobDecodeException($"parameter `{name}` is declared as both {first} and {valueType}");
                }
                return;
            }
            scope.Add(name, valueType);
        }

        private static Dictionary<string, AnimatedValueType> ControllerScope(IReadOnlyList<AnimatorParameter> parameters)
        {
            var scope = new Dictionary<string, AnimatedValueType>();
            foreach (var parameter in parameters)
            {
                if (!scope.TryAdd(parameter.Name, parameter.Kind.ValueType))
                {
                    throw new BlobDecodeException($"animator parameter `{parameter.Name}` appears more than once");
                }
            }
            return scope;
        }

        private static ExpressionParameter ReadExpressionParameter(BlobReader reader)
        {
            var name = reader.ReadString();
            var kind = ReadExpressionParameterKind(reader);
            var saved = reader.ReadBool();
            var synced = reader.ReadBool();
            return new ExpressionParameter(name, kind, saved, synced);
        }

        private static ExpressionParameterKind ReadExpressionParameterKind(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new ExpressionParameterKind.Bool(reader.ReadOptionValue(static r => r.ReadBool()));
                case 1:
                {
                    var width = ReadWidth(reader);
                    var defaultValue = reader.ReadOptionValue(static r => r.ReadI32());
                    return new ExpressionParameterKind.Int(width, defaultValue);
                }
                case 2:
                {
                    var width = ReadWidth(reader);
                    var defaultValue = reader.ReadOptionValue(static r => r.ReadF32());
                    return new ExpressionParameterKind.Float(width, defaultValue);
                }
                default:
                    throw reader.InvalidDiscriminator("ExpressionParameterKind", tag);
            }
        }

        private static byte? ReadWidth(BlobReader reader)
        {
            var width = reader.ReadU8();
            return width == 0 ? (byte?)null : width;
        }

        private static PlayableController ReadPlayableController(BlobReader reader)
        {
            var playable = (PlayableLayer)ReadDiscriminator(reader, "PlayableLayer", 8);
            var mode = (MergeMode)ReadDiscriminator(reader, "MergeMode", 2);
            var priority = reader.ReadI32();
            var pathMode = (PathMode)ReadDiscriminator(reader, "PathMode", 2);
            var mask = reader.ReadOptionValue(ReadAsset);
            var parameters = reader.ReadList(ReadAnimatorParameter);
            reader.Parameters = ControllerScope(parameters);
            var layers = reader.ReadList(ReadAnimatorLayer);
            return new PlayableController(playable, mode, priority, pathMode, mask, parameters, layers);
        }

        private static AnimatorParameter ReadAnimatorParameter(BlobReader reader)
        {
            var name = reader.ReadString();
            var kind = ReadAnimatorParameterKind(reader);
            var origin = ReadAnimatorParameterOrigin(reader);
            return new AnimatorParameter(name, kind, origin);
        }

        private static AnimatorParameterKind ReadAnimatorParameterKind(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => new AnimatorParameterKind.Bool(reader.ReadOptionValue(static r => r.ReadBool())),
                1 => new AnimatorParameterKind.Int(reader.ReadOptionValue(static r => r.ReadI32())),
                2 => new AnimatorParameterKind.Float(reader.ReadOptionValue(static r => r.ReadF32())),
                _ => throw reader.InvalidDiscriminator("AnimatorParameterKind", tag),
            };
        }

        private static AnimatorParameterOrigin ReadAnimatorParameterOrigin(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => new AnimatorParameterOrigin.Declared(),
                1 => new AnimatorParameterOrigin.Generated(),
                2 => new AnimatorParameterOrigin.Provided((ProvidedParameterGroup)ReadDiscriminator(reader, "ProvidedParameterGroup", 1)),
                _ => throw reader.InvalidDiscriminator("AnimatorParameterOrigin", tag),
            };
        }

        private static AnimatorLayer ReadAnimatorLayer(BlobReader reader)
        {
            var name = reader.ReadString();
            var defaultStateIndex = reader.ReadOptionValue(static r => r.ReadU32());
            var states = reader.ReadList(ReadAnimatorState);
            int? defaultState = defaultStateIndex is uint index ? CheckStateIndex(index, states.Count) : null;
            reader.StateCount = states.Count;
            var transitions = reader.ReadList(ReadAnimatorTransition);
            reader.StateCount = null;
            return new AnimatorLayer(name, defaultState, states, transitions);
        }

        private static int CheckStateIndex(uint index, int count)
        {
            if (index >= (uint)count)
            {
                throw new BlobDecodeException($"state index {index} is out of range for a layer of {count} states");
            }
            return (int)index;
        }

        private static int ReadStateIndex(BlobReader reader)
        {
            return CheckStateIndex(reader.ReadU32(), reader.StateCount ?? 0);
        }

        private static AnimatorState ReadAnimatorState(BlobReader reader)
        {
            var name = reader.ReadString();
            var motion = reader.ReadOption(ReadMotion);
            var speed = reader.ReadF64();
            var speedBy = reader.ReadOption(ReadParameter);
            var timeBy = reader.ReadOption(ReadParameter);
            var writeDefaults = reader.ReadBool();
            var behaviors = reader.ReadList(ReadBehavior);
            return new AnimatorState(name, motion, speed, speedBy, timeBy, writeDefaults, behaviors);
        }

        private static AnimatorTransition ReadAnimatorTransition(BlobReader reader)
        {
            var from = ReadTransitionSource(reader);
            var to = ReadTransitionTarget(reader);
            var duration = reader.ReadF64();
            var conditions = reader.ReadList(ReadAnimatorCondition);
            return new AnimatorTransition(from, to, duration, conditions);
        }

        private static TransitionSource ReadTransitionSource(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => TransitionSource.Entry,
                1 => TransitionSource.State(ReadStateIndex(reader)),
                _ => throw reader.InvalidDiscriminator("TransitionSource", tag),
            };
        }

        private static TransitionTarget ReadTransitionTarget(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => TransitionTarget.State(ReadStateIndex(reader)),
                1 => TransitionTarget.Exit,
                _ => throw reader.InvalidDiscriminator("TransitionTarget", tag),
            };
        }

        private static AnimatorCondition ReadAnimatorCondition(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new AnimatorCondition.If(ReadParameter(reader));
                case 1:
                    return new AnimatorCondition.IfNot(ReadParameter(reader));
                case 2:
                {
                    var parameter = ReadParameter(reader);
                    var value = reader.ReadI64();
                    return new AnimatorCondition.Equal(parameter, value);
                }
                case 3:
                {
                    var parameter = ReadParameter(reader);
                    var value = reader.ReadI64();
                    return new AnimatorCondition.NotEqual(parameter, value);
                }
                case 4:
                {
                    var parameter = ReadParameter(reader);
                    var value = reader.ReadF64();
                    return new AnimatorCondition.Greater(parameter, value);
                }
                case 5:
                {
                    var parameter = ReadParameter(reader);
                    var value = reader.ReadF64();
                    return new AnimatorCondition.Less(parameter, value);
                }
                default:
                    throw reader.InvalidDiscriminator("AnimatorCondition", tag);
            }
        }

        private static Motion ReadMotion(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new Motion.InlineClip(ReadInlineAnimation(reader));
                case 1:
                    return new Motion.ExternalClip(ReadAsset(reader));
                case 2:
                {
                    var treeType = (BlendTreeType)ReadDiscriminator(reader, "BlendTreeType", 4);
                    var x = ReadParameter(reader);
                    var y = reader.ReadOption(ReadParameter);
                    var fields = reader.ReadList(ReadParametricField);
                    return new Motion.ParametricTree(treeType, x, y, fields);
                }
                case 3:
                    return new Motion.DirectTree(reader.ReadList(ReadDirectField));
                default:
                    throw reader.InvalidDiscriminator("Motion", tag);
            }
        }

        private static ParametricField ReadParametricField(BlobReader reader)
        {
            var positionX = reader.ReadF64();
            var positionY = reader.ReadF64();
            var speed = reader.ReadF64();
            var motion = ReadMotion(reader);
            return new ParametricField(positionX, positionY, speed, motion);
        }

        private static DirectField ReadDirectField(BlobReader reader)
        {
            var weightBy = ReadParameter(reader);
            var speed = reader.ReadF64();
            var motion = ReadMotion(reader);
            return new DirectField(weightBy, speed, motion);
        }

        private static InlineAnimation ReadInlineAnimation(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new InlineAnimation.Fixed(ReadUniqueTargets(reader, ReadFixedEntry, static e => e.Target));
                case 1:
                {
                    var attributes = ReadClipAttributes(reader);
                    var curves = ReadUniqueTargets(reader, ReadKeyedEntry, static e => e.Target);
                    return new InlineAnimation.Keyed(attributes, curves);
                }
                default:
                    throw reader.InvalidDiscriminator("InlineAnimation", tag);
            }
        }

        private static List<T> ReadUniqueTargets<T>(BlobReader reader, Func<BlobReader, T> read, Func<T, AnimatedTarget> target)
        {
            var entries = reader.ReadList(read);
            var seen = new HashSet<AnimatedTarget>();
            foreach (var entry in entries)
            {
                if (!seen.Add(target(entry)))
                {
                    throw new BlobDecodeException($"animation target {target(entry)} appears more than once");
                }
            }
            return entries;
        }

        private static FixedEntry ReadFixedEntry(BlobReader reader)
        {
            var target = ReadAnimatedTarget(reader);
            var value = ReadAnimatedValue(reader, true);
            return new FixedEntry(target, value);
        }

        private static KeyedEntry ReadKeyedEntry(BlobReader reader)
        {
            var target = ReadAnimatedTarget(reader);
            var curve = ReadCurve(reader);
            return new KeyedEntry(target, curve);
        }

        private static ClipAttributes ReadClipAttributes(BlobReader reader)
        {
            var length = reader.ReadF64();
            var loopTime = reader.ReadBool();
            var loopBlend = reader.ReadBool();
            var cycleOffset = reader.ReadF64();
            return new ClipAttributes(length, loopTime, loopBlend, cycleOffset);
        }

        private static Curve ReadCurve(BlobReader reader)
        {
            var first = ReadKeyframe(reader);
            var rest = reader.ReadList(ReadSegment);
            return new Curve(first, rest);
        }

        private static Segment ReadSegment(BlobReader reader)
        {
            var interpolation = ReadInterpolation(reader);
            var keyframe = ReadKeyframe(reader);
            return new Segment(interpolation, keyframe);
        }

        private static Interpolation ReadInterpolation(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new Interpolation.Constant();
                case 1:
                    return new Interpolation.Linear();
                case 2:
                {
                    var x1 = reader.ReadF64();
                    var y1 = reader.ReadF64();
                    var x2 = reader.ReadF64();
                    var y2 = reader.ReadF64();
                    return new Interpolation.Bezier(x1, y1, x2, y2);
                }
                default:
                    throw reader.InvalidDiscriminator("Interpolation", tag);
            }
        }

        private static Keyframe ReadKeyframe(BlobReader reader)
        {
            var time = reader.ReadF64();
            var value = ReadAnimatedValue(reader, true);
            return new Keyframe(time, value);
        }

        private static AnimatedTarget ReadAnimatedTarget(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new AnimatedTarget.AnimatorSelf(ReadAnimatorProperty(reader));
                case 1:
                {
                    var path = ReadObjectPath(reader);
                    var property = (GameObjectProperty)ReadDiscriminator(reader, "GameObjectProperty", 5);
                    return new AnimatedTarget.GameObject(path, property);
                }
                case 2:
                {
                    var path = ReadObjectPath(reader);
                    var rendererType = reader.ReadString();
                    var property = ReadRendererProperty(reader);
                    return new AnimatedTarget.Renderer(path, rendererType, property);
                }
                case 3:
                {
                    var path = ReadObjectPath(reader);
                    var componentType = ReadComponentType(reader, ComponentTypeUsage.AnimatedTarget);
                    var property = ReadComponentProperty(reader);
                    var valueType = (AnimatedValueType)ReadDiscriminator(reader, "AnimatedValueType", 9);
                    return new AnimatedTarget.Component(path, componentType, property, valueType);
                }
                default:
                    throw reader.InvalidDiscriminator("AnimatedTarget", tag);
            }
        }

        private static AnimatorProperty ReadAnimatorProperty(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => new AnimatorProperty.ParameterFloatValue(ReadParameter(reader)),
                _ => throw reader.InvalidDiscriminator("AnimatorProperty", tag),
            };
        }

        private static RendererProperty ReadRendererProperty(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => new RendererProperty.Enabled(),
                1 => new RendererProperty.BlendShape(reader.ReadString()),
                2 => new RendererProperty.Material(reader.ReadU32()),
                3 => new RendererProperty.MaterialProperty(reader.ReadString()),
                4 => new RendererProperty.Serialized(reader.ReadString()),
                _ => throw reader.InvalidDiscriminator("RendererProperty", tag),
            };
        }

        private static ComponentProperty ReadComponentProperty(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => new ComponentProperty.Enabled(),
                1 => new ComponentProperty.Serialized(reader.ReadString()),
                _ => throw reader.InvalidDiscriminator("ComponentProperty", tag),
            };
        }

        private static AnimatedValue ReadAnimatedValue(BlobReader reader, bool allowObjectReference)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new AnimatedValue.Float(reader.ReadF64());
                case 1:
                    return new AnimatedValue.Int(reader.ReadI64());
                case 2:
                    return new AnimatedValue.Bool(reader.ReadBool());
                case 3:
                {
                    var x = reader.ReadF64();
                    var y = reader.ReadF64();
                    return new AnimatedValue.Vector2(x, y);
                }
                case 4:
                {
                    var x = reader.ReadF64();
                    var y = reader.ReadF64();
                    var z = reader.ReadF64();
                    return new AnimatedValue.Vector3(x, y, z);
                }
                case 5:
                {
                    var x = reader.ReadF64();
                    var y = reader.ReadF64();
                    var z = reader.ReadF64();
                    var w = reader.ReadF64();
                    return new AnimatedValue.Vector4(x, y, z, w);
                }
                case 6:
                {
                    var x = reader.ReadF64();
                    var y = reader.ReadF64();
                    var z = reader.ReadF64();
                    var w = reader.ReadF64();
                    return new AnimatedValue.Quaternion(x, y, z, w);
                }
                case 7:
                {
                    var r = reader.ReadF64();
                    var g = reader.ReadF64();
                    var b = reader.ReadF64();
                    var a = reader.ReadF64();
                    return new AnimatedValue.Color(r, g, b, a);
                }
                case 8 when allowObjectReference:
                    return new AnimatedValue.ObjectReference(ReadAsset(reader));
                default:
                    throw reader.InvalidDiscriminator(allowObjectReference ? "AnimatedValue" : "AnimatedValue without object references", tag);
            }
        }

        private static Behavior ReadBehavior(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                    return new Behavior.ParameterDrive(ReadParameterDriveTarget(reader));
                case 1:
                {
                    var modes = new TrackingControlMode[Behavior.TrackingControl.TargetCount];
                    for (var i = 0; i < modes.Length; i++)
                    {
                        modes[i] = (TrackingControlMode)ReadDiscriminator(reader, "TrackingControlMode", 3);
                    }
                    return new Behavior.TrackingControl(modes);
                }
                case 2:
                {
                    var type = ReadComponentType(reader, ComponentTypeUsage.StateBehaviour);
                    var fields = reader.ReadMap(ReadGenericValue);
                    return new Behavior.Generic(type, fields);
                }
                case 3:
                {
                    var controller = reader.ReadLength("controller index");
                    var layer = reader.ReadLength("layer index");
                    var goalWeight = reader.ReadF64();
                    var blendDuration = reader.ReadF64();
                    return new Behavior.LayerControl(new LayerRef(controller, layer), goalWeight, blendDuration);
                }
                default:
                    throw reader.InvalidDiscriminator("Behavior", tag);
            }
        }

        private static ParameterDriveTarget ReadParameterDriveTarget(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                {
                    var parameter = ReadParameter(reader);
                    var value = ReadAnimatedValue(reader, false);
                    return new ParameterDriveTarget.Set(parameter, value);
                }
                case 1:
                {
                    var parameter = ReadParameter(reader);
                    var value = ReadAnimatedValue(reader, false);
                    return new ParameterDriveTarget.Add(parameter, value);
                }
                case 2:
                {
                    var parameter = ReadParameter(reader);
                    var min = reader.ReadI64();
                    var max = reader.ReadI64();
                    return new ParameterDriveTarget.RandomInt(parameter, min, max);
                }
                case 3:
                {
                    var parameter = ReadParameter(reader);
                    var chance = reader.ReadF64();
                    return new ParameterDriveTarget.RandomBool(parameter, chance);
                }
                case 4:
                {
                    var parameter = ReadParameter(reader);
                    var min = reader.ReadF64();
                    var max = reader.ReadF64();
                    return new ParameterDriveTarget.RandomFloat(parameter, min, max);
                }
                case 5:
                {
                    var from = ReadParameter(reader);
                    var to = ReadParameter(reader);
                    return new ParameterDriveTarget.Copy(from, to);
                }
                case 6:
                {
                    var from = ReadParameter(reader);
                    var fromMin = reader.ReadF64();
                    var fromMax = reader.ReadF64();
                    var to = ReadParameter(reader);
                    var toMin = reader.ReadF64();
                    var toMax = reader.ReadF64();
                    return new ParameterDriveTarget.RangedCopy(from, fromMin, fromMax, to, toMin, toMax);
                }
                default:
                    throw reader.InvalidDiscriminator("ParameterDriveTarget", tag);
            }
        }

        private static GenericValue ReadGenericValue(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => new GenericValue.Bool(reader.ReadBool()),
                1 => new GenericValue.Int(reader.ReadI64()),
                2 => new GenericValue.Float(reader.ReadF64()),
                3 => new GenericValue.String(reader.ReadString()),
                4 => new GenericValue.List(reader.ReadList(ReadGenericValue)),
                5 => new GenericValue.Map(reader.ReadMap(ReadGenericValue)),
                _ => throw reader.InvalidDiscriminator("GenericValue", tag),
            };
        }

        private static MenuItem ReadMenuItem(BlobReader reader)
        {
            var tag = reader.ReadU8();
            switch (tag)
            {
                case 0:
                {
                    var name = reader.ReadString();
                    var items = reader.ReadList(ReadMenuItem);
                    return new MenuItem.SubMenu(name, items);
                }
                case 1:
                {
                    var name = reader.ReadString();
                    var parameter = ReadParameter(reader);
                    var value = ReadAnimatedValue(reader, false);
                    return new MenuItem.Toggle(name, parameter, value);
                }
                case 2:
                {
                    var name = reader.ReadString();
                    var parameter = ReadParameter(reader);
                    var value = ReadAnimatedValue(reader, false);
                    return new MenuItem.Button(name, parameter, value);
                }
                case 3:
                {
                    var name = reader.ReadString();
                    var parameter = ReadParameter(reader);
                    return new MenuItem.Radial(name, parameter);
                }
                case 4:
                {
                    var name = reader.ReadString();
                    var horizontal = ReadMenuAxis(reader);
                    var vertical = ReadMenuAxis(reader);
                    return new MenuItem.TwoAxis(name, horizontal, vertical);
                }
                case 5:
                {
                    var name = reader.ReadString();
                    var up = ReadMenuDirection(reader);
                    var down = ReadMenuDirection(reader);
                    var left = ReadMenuDirection(reader);
                    var right = ReadMenuDirection(reader);
                    return new MenuItem.FourAxis(name, up, down, left, right);
                }
                default:
                    throw reader.InvalidDiscriminator("MenuItem", tag);
            }
        }

        private static MenuAxis ReadMenuAxis(BlobReader reader)
        {
            var parameter = ReadParameter(reader);
            var positive = reader.ReadOption(static r => r.ReadString());
            var negative = reader.ReadOption(static r => r.ReadString());
            return new MenuAxis(parameter, positive, negative);
        }

        private static MenuDirection ReadMenuDirection(BlobReader reader)
        {
            var parameter = ReadParameter(reader);
            var label = reader.ReadOption(static r => r.ReadString());
            return new MenuDirection(parameter, label);
        }
    }
}
