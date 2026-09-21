using KusakaFactory.Declavatar2.Ndmf;
using nadena.dev.ndmf;
using nadena.dev.ndmf.animator;

[assembly: ExportsPlugin(typeof(DeclavatarPlugin))]

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class DeclavatarPlugin : Plugin<DeclavatarPlugin>
    {
        public override string QualifiedName => "org.kb10uy.declavatar2";
        public override string DisplayName => "Declavatar 2";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).Run(CompilePass.Instance);
            InPhase(BuildPhase.Generating).WithRequiredExtension(typeof(AnimatorServicesContext), sequence => sequence.Run(GeneratePass.Instance));
            InPhase(BuildPhase.Transforming).Run(CleanupPass.Instance);
        }
    }
}
