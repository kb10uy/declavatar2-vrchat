using System.Runtime.InteropServices;

namespace KusakaFactory.Declavatar2.Native
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Da2FormatVersions
    {
        public ushort Schema;
        public ushort AvatarData;
        public ushort DiagnosticsData;

        public override string ToString()
        {
            return $"schema {Schema}, avatar data {AvatarData}, diagnostics data {DiagnosticsData}";
        }
    }
}
