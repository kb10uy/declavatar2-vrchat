using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using KusakaFactory.Declavatar2.Data;
using nadena.dev.ndmf.animator;
using UnityEditor;
using UnityEngine;
using Keyframe = UnityEngine.Keyframe;
using Motion = KusakaFactory.Declavatar2.Data.Motion;
using UnityBlendTreeType = UnityEditor.Animations.BlendTreeType;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class MotionGenerator
    {
        private const float FixedLength = 1f / 60f;

        private readonly GenerationContext _context;
        private readonly CurveBindings _bindings;
        private readonly CloneContext _assets;

        public MotionGenerator(GenerationContext context, PathMode pathMode, CloneContext assets)
        {
            _context = context;
            _bindings = new CurveBindings(context, pathMode);
            _assets = assets;
        }

        public VirtualMotion Generate(Motion motion, string name)
        {
            switch (motion)
            {
                case null:
                    return null;
                case Motion.InlineClip inline:
                    return GenerateClip(inline.Animation, name);
                case Motion.ExternalClip external:
                {
                    return _assets.Clone(_context.Resolver.Asset<AnimationClip>(external.Asset));
                }
                case Motion.ParametricTree tree:
                    return GenerateParametric(tree, name);
                case Motion.DirectTree tree:
                    return GenerateDirect(tree, name);
                default:
                    throw new InvalidOperationException($"unknown motion {motion}");
            }
        }

        private VirtualBlendTree GenerateParametric(Motion.ParametricTree tree, string name)
        {
            var result = VirtualBlendTree.Create(name);
            result.BlendType = Convert(tree.TreeType);
            result.BlendParameter = tree.X;
            if (tree.Y != null) result.BlendParameterY = tree.Y;
            result.UseAutomaticThresholds = false;
            result.Children = tree.Fields
                .Select((field, index) => new VirtualBlendTree.VirtualChildMotion
                {
                    Motion = Generate(field.Motion, $"{name}/{index}"),
                    Threshold = (float)field.PositionX,
                    Position = new Vector2((float)field.PositionX, (float)field.PositionY),
                    TimeScale = (float)field.Speed,
                })
                .ToImmutableList();
            return result;
        }

        private VirtualBlendTree GenerateDirect(Motion.DirectTree tree, string name)
        {
            var result = VirtualBlendTree.Create(name);
            result.BlendType = UnityBlendTreeType.Direct;
            result.NormalizedBlendValues = false;
            result.Children = tree.Fields
                .Select((field, index) => new VirtualBlendTree.VirtualChildMotion
                {
                    Motion = Generate(field.Motion, $"{name}/{index}"),
                    DirectBlendParameter = field.WeightBy,
                    TimeScale = (float)field.Speed,
                })
                .ToImmutableList();
            return result;
        }

        private VirtualClip GenerateClip(InlineAnimation animation, string name)
        {
            var clip = VirtualClip.Create(name);
            switch (animation)
            {
                case InlineAnimation.Fixed fixedClip:
                    foreach (var entry in fixedClip.Entries) WriteFixed(clip, entry);
                    break;
                case InlineAnimation.Keyed keyed:
                    foreach (var entry in keyed.Curves) WriteKeyed(clip, entry, keyed.Attributes);
                    var settings = clip.Settings;
                    settings.loopTime = keyed.Attributes.LoopTime;
                    settings.loopBlend = keyed.Attributes.LoopBlend;
                    settings.cycleOffset = (float)keyed.Attributes.CycleOffset;
                    clip.Settings = settings;
                    break;
                default:
                    throw new InvalidOperationException($"unknown inline animation {animation}");
            }
            return clip;
        }

        private void WriteFixed(VirtualClip clip, FixedEntry entry)
        {
            var bindings = _bindings.Resolve(entry.Target, entry.Value.Type, out var valueScale);
            if (bindings == null) return;

            if (entry.Value is AnimatedValue.ObjectReference reference)
            {
                var asset = _context.Resolver.Asset(reference.Asset);
                clip.SetObjectCurve(bindings[0], new[]
                {
                    new ObjectReferenceKeyframe { time = 0f, value = asset },
                    new ObjectReferenceKeyframe { time = FixedLength, value = asset },
                });
                return;
            }

            var components = Values.Components(entry.Value);
            for (var i = 0; i < bindings.Count; i++)
            {
                var value = components[i] * valueScale;
                clip.SetFloatCurve(bindings[i], new AnimationCurve(new Keyframe(0f, value), new Keyframe(FixedLength, value)));
            }
        }

        private void WriteKeyed(VirtualClip clip, KeyedEntry entry, ClipAttributes attributes)
        {
            var curve = entry.Curve;
            var valueType = curve.First.Value.Type;
            var bindings = _bindings.Resolve(entry.Target, valueType, out var valueScale);
            if (bindings == null) return;

            var length = (float)attributes.Length;
            var times = new List<float> { (float)(curve.First.Time * length) };
            var values = new List<AnimatedValue> { curve.First.Value };
            foreach (var segment in curve.Rest)
            {
                times.Add((float)(segment.Keyframe.Time * length));
                values.Add(segment.Keyframe.Value);
            }

            if (valueType == AnimatedValueType.ObjectReference)
            {
                var keys = new ObjectReferenceKeyframe[times.Count];
                for (var i = 0; i < keys.Length; i++)
                {
                    keys[i] = new ObjectReferenceKeyframe
                    {
                        time = times[i],
                        value = _context.Resolver.Asset(((AnimatedValue.ObjectReference)values[i]).Asset),
                    };
                }
                clip.SetObjectCurve(bindings[0], SpanLength(keys, length, static (key, time) => new ObjectReferenceKeyframe { time = time, value = key.value }, static key => key.time));
                return;
            }

            var components = values.Select(Values.Components).ToList();
            for (var component = 0; component < bindings.Count; component++)
            {
                var keys = new Keyframe[times.Count];
                for (var i = 0; i < keys.Length; i++) keys[i] = new Keyframe(times[i], components[i][component] * valueScale);
                for (var i = 0; i < curve.Rest.Count; i++) ApplyInterpolation(keys, i, curve.Rest[i].Interpolation);
                clip.SetFloatCurve(bindings[component], new AnimationCurve(SpanLength(keys, length, static (key, time) => new Keyframe(time, key.value), static key => key.time)));
            }
        }

        private static T[] SpanLength<T>(T[] keys, float length, Func<T, float, T> hold, Func<T, float> timeOf)
        {
            var result = new List<T>(keys.Length + 2);
            if (timeOf(keys[0]) > 0f) result.Add(hold(keys[0], 0f));
            result.AddRange(keys);
            if (timeOf(keys[keys.Length - 1]) < length) result.Add(hold(keys[keys.Length - 1], length));
            return result.ToArray();
        }

        private static void ApplyInterpolation(Keyframe[] keys, int index, Interpolation interpolation)
        {
            var from = keys[index];
            var to = keys[index + 1];
            var dt = to.time - from.time;
            var dv = to.value - from.value;
            switch (interpolation)
            {
                case Interpolation.Constant _:
                    from.outTangent = float.PositiveInfinity;
                    to.inTangent = float.PositiveInfinity;
                    break;
                case Interpolation.Linear _:
                    var slope = Tangent(dv, dt);
                    from.outTangent = slope;
                    to.inTangent = slope;
                    break;
                case Interpolation.Bezier bezier:
                    from.outTangent = Tangent((float)bezier.Y1 * dv, (float)bezier.X1 * dt);
                    from.outWeight = (float)bezier.X1;
                    from.weightedMode |= WeightedMode.Out;
                    to.inTangent = Tangent((1f - (float)bezier.Y2) * dv, (1f - (float)bezier.X2) * dt);
                    to.inWeight = 1f - (float)bezier.X2;
                    to.weightedMode |= WeightedMode.In;
                    break;
                default:
                    throw new InvalidOperationException($"unknown interpolation {interpolation}");
            }
            keys[index] = from;
            keys[index + 1] = to;
        }

        private static float Tangent(float rise, float run)
        {
            if (run > 0f) return rise / run;
            if (rise == 0f) return 0f;
            return rise > 0f ? float.PositiveInfinity : float.NegativeInfinity;
        }

        private static UnityBlendTreeType Convert(BlendTreeType type)
        {
            switch (type)
            {
                case BlendTreeType.Linear: return UnityBlendTreeType.Simple1D;
                case BlendTreeType.Simple2d: return UnityBlendTreeType.SimpleDirectional2D;
                case BlendTreeType.Freeform2d: return UnityBlendTreeType.FreeformDirectional2D;
                case BlendTreeType.Cartesian2d: return UnityBlendTreeType.FreeformCartesian2D;
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
