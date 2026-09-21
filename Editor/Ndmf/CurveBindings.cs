using System;
using System.Collections.Generic;
using System.Linq;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Resolution;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class CurveBindings
    {
        private readonly GenerationContext _context;
        private readonly PathMode _pathMode;
        private readonly HashSet<string> _reportedRendererTypes = new HashSet<string>();
        private readonly Dictionary<(int, Type, string), float?> _blendShapeWeights = new Dictionary<(int, Type, string), float?>();

        public CurveBindings(GenerationContext context, PathMode pathMode)
        {
            _context = context;
            _pathMode = pathMode;
        }

        public IReadOnlyList<EditorCurveBinding> Resolve(AnimatedTarget target, AnimatedValueType valueType, out float valueScale)
        {
            valueScale = 1f;
            switch (target)
            {
                case AnimatedTarget.AnimatorSelf self:
                    return Expand("", typeof(Animator), ((AnimatorProperty.ParameterFloatValue)self.Property).Name, valueType);
                case AnimatedTarget.GameObject gameObject:
                    return ResolveGameObject(gameObject, valueType);
                case AnimatedTarget.Renderer renderer:
                    return ResolveRenderer(renderer, valueType, out valueScale);
                case AnimatedTarget.Component component:
                    return ResolveComponent(component, valueType);
                default:
                    throw new InvalidOperationException($"unknown animated target {target}");
            }
        }

        private IReadOnlyList<EditorCurveBinding> ResolveGameObject(AnimatedTarget.GameObject target, AnimatedValueType valueType)
        {
            var path = _context.ObjectPath(_pathMode, target.Path);
            if (path == null) return null;

            switch (target.Property)
            {
                case GameObjectProperty.Active: return Expand(path, typeof(GameObject), "m_IsActive", valueType);
                case GameObjectProperty.TransformPosition: return Expand(path, typeof(Transform), "m_LocalPosition", valueType);
                case GameObjectProperty.TransformRotationQuaternion: return Expand(path, typeof(Transform), "m_LocalRotation", valueType);
                case GameObjectProperty.TransformRotationEuler: return Expand(path, typeof(Transform), "localEulerAnglesRaw", valueType);
                case GameObjectProperty.TransformScale: return Expand(path, typeof(Transform), "m_LocalScale", valueType);
                default: throw new ArgumentOutOfRangeException(nameof(target), target.Property, null);
            }
        }

        private IReadOnlyList<EditorCurveBinding> ResolveRenderer(AnimatedTarget.Renderer target, AnimatedValueType valueType, out float valueScale)
        {
            valueScale = 1f;
            var path = _context.ObjectPath(_pathMode, target.Path);
            var type = TypeIndex.FindComponent(target.RendererType);
            if (type == null || !typeof(Renderer).IsAssignableFrom(type))
            {
                if (_reportedRendererTypes.Add(target.RendererType))
                {
                    _context.Report(ErrorSeverity.Error, "declavatar2.generate.renderer_type", target.RendererType);
                }
                return null;
            }
            if (path == null) return null;

            switch (target.Property)
            {
                case RendererProperty.Enabled _: return Expand(path, type, "m_Enabled", valueType);
                case RendererProperty.BlendShape blendShape:
                    var weight = BlendShapeWeight(target, type, blendShape.Name);
                    if (!weight.HasValue) return null;
                    valueScale = weight.Value;
                    return Expand(path, type, "blendShape." + blendShape.Name, valueType);
                case RendererProperty.Material material: return Expand(path, type, $"m_Materials.Array.data[{material.Slot}]", valueType);
                case RendererProperty.MaterialProperty property: return Expand(path, type, "material." + property.Name, valueType);
                case RendererProperty.Serialized serialized: return Expand(path, type, serialized.Name, valueType);
                default: throw new InvalidOperationException($"unknown renderer property {target.Property}");
            }
        }

        private float? BlendShapeWeight(AnimatedTarget.Renderer target, Type type, string name)
        {
            var key = (target.Path.Index, type, name);
            if (_blendShapeWeights.TryGetValue(key, out var cached)) return cached;

            var obj = _context.Resolver.Object(_pathMode, target.Path);
            var renderer = obj.GetComponent(type) as SkinnedMeshRenderer;
            var mesh = renderer != null ? renderer.sharedMesh : null;
            var index = mesh != null ? mesh.GetBlendShapeIndex(name) : -1;
            var count = index >= 0 ? mesh.GetBlendShapeFrameCount(index) : 0;
            float? weight = null;
            if (count > 0)
            {
                var maximum = mesh.GetBlendShapeFrameWeight(index, 0);
                for (var i = 1; i < count; i++) maximum = Math.Max(maximum, mesh.GetBlendShapeFrameWeight(index, i));
                weight = maximum;
            }
            else
            {
                _context.Report(ErrorSeverity.Error, "declavatar2.generate.blend_shape", _context.Avatar.Externals[target.Path].Value, name);
            }
            _blendShapeWeights[key] = weight;
            return weight;
        }

        private IReadOnlyList<EditorCurveBinding> ResolveComponent(AnimatedTarget.Component target, AnimatedValueType valueType)
        {
            var path = _context.ObjectPath(_pathMode, target.Path);
            var type = _context.Resolver.ComponentType(target.ComponentType);
            if (path == null || type == null) return null;

            switch (target.Property)
            {
                case ComponentProperty.Enabled _: return Expand(path, type, "m_Enabled", valueType);
                case ComponentProperty.Serialized serialized: return Expand(path, type, serialized.Name, valueType);
                default: throw new InvalidOperationException($"unknown component property {target.Property}");
            }
        }

        private static IReadOnlyList<EditorCurveBinding> Expand(string path, Type type, string property, AnimatedValueType valueType)
        {
            if (valueType == AnimatedValueType.ObjectReference)
            {
                return new[] { EditorCurveBinding.PPtrCurve(path, type, property) };
            }
            return Values.Suffixes(valueType).Select(suffix => EditorCurveBinding.FloatCurve(path, type, property + suffix)).ToArray();
        }
    }
}
