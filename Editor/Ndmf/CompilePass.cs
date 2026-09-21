using System;
using KusakaFactory.Declavatar2.Compilation;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Native;
using KusakaFactory.Declavatar2.Runtime;
using nadena.dev.ndmf;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal sealed class CompilePass : Pass<CompilePass>
    {
        public override string DisplayName => "Compile declarations";

        protected override void Execute(BuildContext context)
        {
            var state = context.GetState<DeclavatarBuildState>();
            foreach (var declaration in context.AvatarRootObject.GetComponentsInChildren<DeclavatarDeclaration>(true))
            {
                var input = CompilationInput.From(declaration);
                if (input == null)
                {
                    DeclavatarErrors.Report(ErrorSeverity.Error, "declavatar2.declaration.no_script", declaration);
                    continue;
                }

                CompiledDeclaration compiled;
                try
                {
                    compiled = DeclarationCompiler.Compile(input);
                }
                catch (Exception e) when (e is Da2Exception || e is BlobDecodeException || e is DllNotFoundException || e is EntryPointNotFoundException)
                {
                    DeclavatarErrors.Report(ErrorSeverity.Error, "declavatar2.native.failed", e.Message, declaration);
                    continue;
                }

                DeclarationCache.Store(compiled);
                if (compiled.Succeeded)
                {
                    state.Compiled[declaration] = compiled;
                }
                else
                {
                    DeclavatarErrors.ReportDiagnostics(declaration, compiled.Diagnostics);
                }
            }
        }
    }
}
