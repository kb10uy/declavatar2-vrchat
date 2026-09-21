using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Native;

namespace KusakaFactory.Declavatar2.Compilation
{
    public static class DeclarationCompiler
    {
        public static CompiledDeclaration Compile(CompilationInput input)
        {
            Da2Library.EnsureFormatVersions();
            using (var context = new Da2Context())
            {
                foreach (var symbol in input.Symbols) context.AddSymbol(symbol);
                foreach (var path in input.LibraryPaths) context.AddLibraryPath(path);

                var result = context.Compile(input.Source, input.ChunkName);
                return result.Succeeded
                    ? new CompiledDeclaration(input, BlobDecoder.DecodeAvatar(result.Blob), null)
                    : new CompiledDeclaration(input, null, BlobDecoder.DecodeDiagnostics(result.Blob));
            }
        }
    }
}
