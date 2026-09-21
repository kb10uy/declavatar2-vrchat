using KusakaFactory.Declavatar2.Data;

namespace KusakaFactory.Declavatar2.Compilation
{
    public sealed class CompiledDeclaration
    {
        public CompiledDeclaration(CompilationInput input, Avatar avatar, Diagnostics diagnostics)
        {
            Input = input;
            Avatar = avatar;
            Diagnostics = diagnostics;
        }

        public CompilationInput Input { get; }
        public Avatar Avatar { get; }
        public Diagnostics Diagnostics { get; }
        public bool Succeeded => Avatar != null;
    }
}
