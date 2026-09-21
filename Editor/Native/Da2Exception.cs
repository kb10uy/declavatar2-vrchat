using System;

namespace KusakaFactory.Declavatar2.Native
{
    public sealed class Da2Exception : Exception
    {
        public Da2Exception(Da2Status status) : base(Describe(status))
        {
            Status = status;
        }

        public Da2Exception(string message) : base(message)
        {
            Status = null;
        }

        public Da2Status? Status { get; }

        public static string Describe(Da2Status status)
        {
            return status switch
            {
                Da2Status.Success => "the call succeeded",
                Da2Status.CompileFailed => "the script was rejected",
                Da2Status.InvalidPointer => "a required pointer was null",
                Da2Status.InvalidUtf8 => "a string was not valid UTF-8",
                Da2Status.NoResult => "nothing has been compiled since the context was created or reset",
                Da2Status.EncodeFailed => "the compiled data could not be encoded; this is a bug in the native library",
                Da2Status.Panicked => "the native library panicked; the context should be freed",
                _ => $"the native library returned unknown status {(uint)status}",
            };
        }
    }
}
