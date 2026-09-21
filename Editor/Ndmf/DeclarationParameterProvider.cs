using System;
using System.Collections.Generic;
using System.Linq;
using KusakaFactory.Declavatar2.Compilation;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Runtime;
using nadena.dev.ndmf;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Ndmf
{
    [ParameterProviderFor(typeof(DeclavatarDeclaration))]
    internal sealed class DeclarationParameterProvider : IParameterProvider
    {
        private readonly DeclavatarDeclaration _declaration;

        public DeclarationParameterProvider(DeclavatarDeclaration declaration)
        {
            _declaration = declaration;
        }

        public IEnumerable<ProvidedParameter> GetSuppliedParameters(BuildContext context = null)
        {
            var compiled = Lookup(context);
            if (compiled == null || !compiled.Succeeded) return Enumerable.Empty<ProvidedParameter>();

            var plugin = DeclavatarPlugin.Instance;
            var result = new List<ProvidedParameter>();
            var names = new HashSet<string>();
            foreach (var parameter in compiled.Avatar.ExpressionParameters)
            {
                names.Add(parameter.Name);
                result.Add(new ProvidedParameter(parameter.Name, ParameterNamespace.Animator, _declaration, plugin, Convert(parameter.Kind.ValueType))
                {
                    WantSynced = parameter.Synced,
                    IsAnimatorOnly = false,
                    DefaultValue = DefaultOf(parameter.Kind),
                });
            }
            foreach (var controller in compiled.Avatar.Controllers)
            {
                foreach (var parameter in controller.Parameters)
                {
                    if (!names.Add(parameter.Name)) continue;
                    result.Add(new ProvidedParameter(parameter.Name, ParameterNamespace.Animator, _declaration, plugin, Convert(parameter.Kind.ValueType))
                    {
                        IsAnimatorOnly = true,
                        DefaultValue = DefaultOf(parameter.Kind),
                    });
                }
            }
            return result;
        }

        private CompiledDeclaration Lookup(BuildContext context)
        {
            if (context != null && context.GetState<DeclavatarBuildState>().Compiled.TryGetValue(_declaration, out var built)) return built;
            try
            {
                return DeclarationCache.GetOrCompile(_declaration);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static AnimatorControllerParameterType Convert(AnimatedValueType type)
        {
            switch (type)
            {
                case AnimatedValueType.Bool: return AnimatorControllerParameterType.Bool;
                case AnimatedValueType.Int: return AnimatorControllerParameterType.Int;
                default: return AnimatorControllerParameterType.Float;
            }
        }

        private static float? DefaultOf(ExpressionParameterKind kind)
        {
            switch (kind)
            {
                case ExpressionParameterKind.Bool b: return b.Default is bool value ? (value ? 1f : 0f) : (float?)null;
                case ExpressionParameterKind.Int i: return i.Default;
                case ExpressionParameterKind.Float f: return f.Default;
                default: return null;
            }
        }

        private static float? DefaultOf(AnimatorParameterKind kind)
        {
            switch (kind)
            {
                case AnimatorParameterKind.Bool b: return b.Default is bool value ? (value ? 1f : 0f) : (float?)null;
                case AnimatorParameterKind.Int i: return i.Default;
                case AnimatorParameterKind.Float f: return f.Default;
                default: return null;
            }
        }
    }
}
