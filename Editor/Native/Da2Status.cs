namespace KusakaFactory.Declavatar2.Native
{
    public enum Da2Status : uint
    {
        Success = 0,
        CompileFailed = 1,
        InvalidPointer = 100,
        InvalidUtf8 = 101,
        NoResult = 102,
        EncodeFailed = 200,
        Panicked = 201,
    }
}
