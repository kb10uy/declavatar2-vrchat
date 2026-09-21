using System;
using System.Runtime.InteropServices;

namespace KusakaFactory.Declavatar2.Native
{
    internal sealed class Da2ContextHandle : SafeHandle
    {
        public Da2ContextHandle() : base(IntPtr.Zero, true)
        {
        }

        public override bool IsInvalid => handle == IntPtr.Zero;

        protected override bool ReleaseHandle()
        {
            NativeMethods.da2_context_free(handle);
            return true;
        }
    }
}
