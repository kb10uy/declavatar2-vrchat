namespace KusakaFactory.Declavatar2.Data
{
    internal static class DiagnosticsDecoder
    {
        public static Diagnostics ReadDiagnostics(BlobReader reader)
        {
            var stage = ReadStage(reader);
            var items = reader.ReadList(ReadDiagnostic);
            return new Diagnostics(stage, items);
        }

        private static DiagnosticStage ReadStage(BlobReader reader)
        {
            var tag = reader.ReadU8();
            return tag switch
            {
                0 => DiagnosticStage.Script,
                1 => DiagnosticStage.Transform,
                _ => throw reader.InvalidDiscriminator("DiagnosticStage", tag),
            };
        }

        private static Diagnostic ReadDiagnostic(BlobReader reader)
        {
            var at = reader.ReadOption(ReadSourceLocation);
            var message = reader.ReadString();
            return new Diagnostic(at, message);
        }

        private static SourceLocation ReadSourceLocation(BlobReader reader)
        {
            var chunk = reader.ReadString();
            var line = reader.ReadU32();
            return new SourceLocation(chunk, line);
        }
    }
}
