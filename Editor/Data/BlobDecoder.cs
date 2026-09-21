namespace KusakaFactory.Declavatar2.Data
{
    public static class BlobDecoder
    {
        public static BlobKind ReadKind(byte[] blob)
        {
            return BlobHeader.Validate(blob, null);
        }

        public static Avatar DecodeAvatar(byte[] blob)
        {
            BlobHeader.Validate(blob, BlobKind.Avatar);
            var reader = new BlobReader(blob, BlobHeader.Length);
            var avatar = AvatarDecoder.ReadAvatar(reader);
            reader.Finish();
            return avatar;
        }

        public static Diagnostics DecodeDiagnostics(byte[] blob)
        {
            BlobHeader.Validate(blob, BlobKind.Diagnostics);
            var reader = new BlobReader(blob, BlobHeader.Length);
            var diagnostics = DiagnosticsDecoder.ReadDiagnostics(reader);
            reader.Finish();
            return diagnostics;
        }
    }
}
