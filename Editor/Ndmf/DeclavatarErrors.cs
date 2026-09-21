using System;
using System.Linq;
using KusakaFactory.Declavatar2.Data;
using KusakaFactory.Declavatar2.Localization;
using KusakaFactory.Declavatar2.Resolution;
using KusakaFactory.Declavatar2.Runtime;
using nadena.dev.ndmf;

namespace KusakaFactory.Declavatar2.Ndmf
{
    internal static class DeclavatarErrors
    {
        public static void Report(ErrorSeverity severity, string key, params object[] args)
        {
            ErrorReport.ReportError(DeclavatarLocalization.NdmfLocalizer, severity, key, args);
        }

        public static void ReportDiagnostics(DeclavatarDeclaration declaration, Diagnostics diagnostics)
        {
            foreach (var item in diagnostics.Items)
            {
                if (diagnostics.Stage == DiagnosticStage.Script)
                {
                    Report(ErrorSeverity.Error, "declavatar2.compile.script", item.Message, declaration, declaration.Script);
                }
                else
                {
                    Report(ErrorSeverity.Error, "declavatar2.compile.transform", item.At?.ToString() ?? "?", item.Message, declaration, declaration.Script);
                }
            }
        }

        public static void ReportResolution(DeclavatarDeclaration declaration, ResolutionError error)
        {
            string key;
            switch (error.Kind)
            {
                case ResolutionErrorKind.ObjectNotFound: key = "declavatar2.resolve.object"; break;
                case ResolutionErrorKind.ComponentTypeNotFound: key = "declavatar2.resolve.component_type"; break;
                case ResolutionErrorKind.AssetTypeUnknown: key = "declavatar2.resolve.asset_type_unknown"; break;
                case ResolutionErrorKind.AssetNotFound: key = "declavatar2.resolve.asset_not_found"; break;
                case ResolutionErrorKind.AssetAmbiguous: key = "declavatar2.resolve.asset_ambiguous"; break;
                case ResolutionErrorKind.AssetTypeMismatch: key = "declavatar2.resolve.asset_type_mismatch"; break;
                default: throw new InvalidOperationException($"unknown resolution error {error.Kind}");
            }
            var args = error.Arguments.Cast<object>().Concat(new object[] { error.Locations, declaration }).ToArray();
            Report(ErrorSeverity.Error, key, args);
        }
    }
}
