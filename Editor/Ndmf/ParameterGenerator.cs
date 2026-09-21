using KusakaFactory.Declavatar2.Data;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class ParameterGenerator
    {
        private readonly GenerationContext _context;

        public ParameterGenerator(GenerationContext context)
        {
            _context = context;
        }

        public void Generate()
        {
            var parameters = _context.Avatar.ExpressionParameters;
            if (parameters.Count == 0) return;

            var target = _context.Declaration.gameObject;
            var component = target.GetComponent<ModularAvatarParameters>();
            if (component == null) component = target.AddComponent<ModularAvatarParameters>();
            foreach (var parameter in parameters) component.parameters.Add(Convert(parameter));
        }

        private ParameterConfig Convert(ExpressionParameter parameter)
        {
            var config = new ParameterConfig
            {
                nameOrPrefix = parameter.Name,
                remapTo = "",
                internalParameter = false,
                isPrefix = false,
                localOnly = !parameter.Synced,
                saved = parameter.Saved,
            };
            switch (parameter.Kind)
            {
                case ExpressionParameterKind.Bool b:
                    config.syncType = ParameterSyncType.Bool;
                    if (b.Default is bool boolDefault) SetDefault(ref config, boolDefault ? 1f : 0f);
                    break;
                case ExpressionParameterKind.Int i:
                    CheckWidth(parameter.Name, i.Width);
                    config.syncType = ParameterSyncType.Int;
                    if (i.Default is int intDefault) SetDefault(ref config, intDefault);
                    break;
                case ExpressionParameterKind.Float f:
                    CheckWidth(parameter.Name, f.Width);
                    config.syncType = ParameterSyncType.Float;
                    if (f.Default is float floatDefault) SetDefault(ref config, floatDefault);
                    break;
            }
            return config;
        }

        private static void SetDefault(ref ParameterConfig config, float value)
        {
            config.defaultValue = value;
            config.hasExplicitDefaultValue = true;
        }

        private void CheckWidth(string name, byte? width)
        {
            if (width is byte w && w != 8)
            {
                _context.Report(ErrorSeverity.Error, "declavatar2.generate.width", name, w.ToString());
            }
        }
    }
}
