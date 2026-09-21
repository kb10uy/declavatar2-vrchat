using KusakaFactory.Declavatar2.Data;

namespace KusakaFactory.Declavatar2.Native
{
    public static class Da2Library
    {
        public static Da2FormatVersions FormatVersions()
        {
            return NativeMethods.da2_format_versions();
        }

        public static bool SupportsFormatVersions(Da2FormatVersions versions)
        {
            return versions.Schema == BlobHeader.SchemaVersion
                && versions.AvatarData == BlobHeader.AvatarDataVersion
                && versions.DiagnosticsData == BlobHeader.DiagnosticsDataVersion;
        }

        public static void EnsureFormatVersions()
        {
            var versions = FormatVersions();
            if (!SupportsFormatVersions(versions))
            {
                throw new Da2Exception(
                    $"the native library writes interop format {versions} but this client reads schema {BlobHeader.SchemaVersion}, avatar data {BlobHeader.AvatarDataVersion}, diagnostics data {BlobHeader.DiagnosticsDataVersion}");
            }
        }
    }
}
