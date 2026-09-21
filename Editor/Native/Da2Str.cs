using System;
using System.Runtime.InteropServices;

namespace KusakaFactory.Declavatar2.Native
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Da2Str
    {
        public IntPtr Ptr;
        public uint Len;

        public Da2Str(IntPtr ptr, int len)
        {
            Ptr = ptr;
            Len = checked((uint)len);
        }
    }
}
