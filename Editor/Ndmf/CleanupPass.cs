using KusakaFactory.Declavatar2.Runtime;
using nadena.dev.ndmf;
using UnityEngine;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class CleanupPass : Pass<CleanupPass>
    {
        public override string DisplayName => "Remove declaration components";

        protected override void Execute(BuildContext context)
        {
            foreach (var declaration in context.AvatarRootObject.GetComponentsInChildren<DeclavatarDeclaration>(true))
            {
                Object.DestroyImmediate(declaration);
            }
        }
    }
}
