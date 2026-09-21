using System;
using System.Runtime.InteropServices;
using System.Text;

namespace KusakaFactory.Declavatar2.Native
{
    public sealed class Da2Context : IDisposable
    {
        private readonly Da2ContextHandle _handle;

        public Da2Context()
        {
            _handle = NativeMethods.da2_context_new();
            if (_handle.IsInvalid)
            {
                throw new Da2Exception("da2_context_new returned a null context");
            }
        }

        public void AddSymbol(string name)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }
            Check(AddSymbolNative(Encoding.UTF8.GetBytes(name)));
        }

        public void AddLibraryPath(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }
            Check(AddLibraryPathNative(Encoding.UTF8.GetBytes(path)));
        }

        public void Reset()
        {
            Check(NativeMethods.da2_context_reset(Handle));
        }

        public Da2CompileResult Compile(string source, string chunkName)
        {
            if (source is null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            if (chunkName is null)
            {
                throw new ArgumentNullException(nameof(chunkName));
            }

            var status = CompileNative(Encoding.UTF8.GetBytes(source), Encoding.UTF8.GetBytes(chunkName));
            switch (status)
            {
                case Da2Status.Success:
                case Da2Status.CompileFailed:
                    return new Da2CompileResult(status, CopyResult());
                default:
                    throw new Da2Exception(status);
            }
        }

        public void Dispose()
        {
            _handle.Dispose();
        }

        private Da2ContextHandle Handle
        {
            get
            {
                if (_handle.IsClosed)
                {
                    throw new ObjectDisposedException(nameof(Da2Context));
                }
                return _handle;
            }
        }

        private static void Check(Da2Status status)
        {
            if (status != Da2Status.Success)
            {
                throw new Da2Exception(status);
            }
        }

        private unsafe Da2Status AddSymbolNative(byte[] name)
        {
            fixed (byte* namePtr = name)
            {
                return NativeMethods.da2_context_add_symbol(Handle, new Da2Str((IntPtr)namePtr, name.Length));
            }
        }

        private unsafe Da2Status AddLibraryPathNative(byte[] path)
        {
            fixed (byte* pathPtr = path)
            {
                return NativeMethods.da2_context_add_library_path(Handle, new Da2Str((IntPtr)pathPtr, path.Length));
            }
        }

        private unsafe Da2Status CompileNative(byte[] source, byte[] chunkName)
        {
            fixed (byte* sourcePtr = source)
            fixed (byte* chunkNamePtr = chunkName)
            {
                return NativeMethods.da2_context_compile(
                    Handle,
                    new Da2Str((IntPtr)sourcePtr, source.Length),
                    new Da2Str((IntPtr)chunkNamePtr, chunkName.Length));
            }
        }

        private byte[] CopyResult()
        {
            Check(NativeMethods.da2_context_result(Handle, out var ptr, out var len));
            var length = checked((int)len);
            var bytes = new byte[length];
            if (length > 0)
            {
                Marshal.Copy(ptr, bytes, 0, length);
            }
            return bytes;
        }
    }
}
