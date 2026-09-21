using System;
using System.Runtime.InteropServices;

namespace KusakaFactory.Declavatar2.Native
{
    internal static class NativeMethods
    {
        private const string Library = "da2";

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Da2FormatVersions da2_format_versions();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Da2ContextHandle da2_context_new();

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern void da2_context_free(IntPtr context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Da2Status da2_context_add_symbol(Da2ContextHandle context, Da2Str name);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Da2Status da2_context_add_library_path(Da2ContextHandle context, Da2Str path);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Da2Status da2_context_reset(Da2ContextHandle context);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Da2Status da2_context_compile(Da2ContextHandle context, Da2Str source, Da2Str chunkName);

        [DllImport(Library, CallingConvention = CallingConvention.Cdecl)]
        public static extern Da2Status da2_context_result(Da2ContextHandle context, out IntPtr ptr, out uint len);
    }
}
