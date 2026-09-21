namespace KusakaFactory.Declavatar2.Native
{
    public sealed class Da2CompileResult
    {
        public Da2CompileResult(Da2Status status, byte[] blob)
        {
            Status = status;
            Blob = blob;
        }

        public Da2Status Status { get; }
        public byte[] Blob { get; }

        public bool Succeeded => Status == Da2Status.Success;
    }
}
