using System.Collections.Generic;
using KusakaFactory.Declavatar2.Compilation;
using KusakaFactory.Declavatar2.Runtime;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class DeclavatarBuildState
    {
        public Dictionary<DeclavatarDeclaration, CompiledDeclaration> Compiled { get; } = new Dictionary<DeclavatarDeclaration, CompiledDeclaration>();
    }
}
