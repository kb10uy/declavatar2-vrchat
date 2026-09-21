using System.Collections.Generic;
using KusakaFactory.Declavatar2.Runtime;

namespace KusakaFactory.Declavatar2.Compilation
{
    public static class DeclarationCache
    {
        private static readonly Dictionary<string, CompiledDeclaration> Entries = new Dictionary<string, CompiledDeclaration>();

        public static CompiledDeclaration GetOrCompile(DeclavatarDeclaration declaration)
        {
            var input = CompilationInput.From(declaration);
            if (input == null) return null;
            if (TryGet(input, out var cached)) return cached;

            var compiled = DeclarationCompiler.Compile(input);
            Store(compiled);
            return compiled;
        }

        public static bool TryGet(CompilationInput input, out CompiledDeclaration compiled)
        {
            if (Entries.TryGetValue(input.CacheKey, out compiled) && IsCurrent(compiled.Input, input)) return true;
            compiled = null;
            return false;
        }

        public static void Store(CompiledDeclaration compiled)
        {
            Entries[compiled.Input.CacheKey] = compiled;
        }

        public static void Invalidate(DeclavatarDeclaration declaration)
        {
            var input = CompilationInput.From(declaration);
            if (input != null) Entries.Remove(input.CacheKey);
        }

        public static void Clear()
        {
            Entries.Clear();
        }

        private static bool IsCurrent(CompilationInput cached, CompilationInput current)
        {
            return cached.SourceHash == current.SourceHash && cached.LibraryFingerprint == current.LibraryFingerprint;
        }
    }
}
