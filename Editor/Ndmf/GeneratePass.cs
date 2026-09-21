using nadena.dev.ndmf;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class GeneratePass : Pass<GeneratePass>
    {
        public override string DisplayName => "Generate declared assets";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<DeclavatarBuildState>();
            foreach (var pair in state.Compiled)
            {
                if (pair.Key == null) continue;

                var generation = new GenerationContext(context, pair.Key, pair.Value.Avatar);
                new ParameterGenerator(generation).Generate();
                new ControllerGenerator(generation).Generate();
                new MenuGenerator(generation).Generate();
                generation.FlushResolutionErrors();
            }
        }
    }
}
