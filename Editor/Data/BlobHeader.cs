using System;
using System.Buffers.Binary;

namespace KusakaFactory.Declavatar2.Data
{
    public static class BlobHeader
    {
        public const int Length = 16;
        public const ushort SchemaVersion = 1;
        public const ushort AvatarDataVersion = 3;
        public const ushort DiagnosticsDataVersion = 1;

        private const uint AvatarMagic = 0x61324144;
        private const uint DiagnosticsMagic = 0x64324144;

        public static ushort DataVersionOf(BlobKind kind)
        {
            return kind switch
            {
                BlobKind.Avatar => AvatarDataVersion,
                BlobKind.Diagnostics => DiagnosticsDataVersion,
                _ => throw new ArgumentOutOfRangeException(nameof(kind)),
            };
        }

        public static BlobKind Validate(byte[] blob)
        {
            return Validate(blob, null);
        }

        internal static BlobKind Validate(byte[] blob, BlobKind? expected)
        {
            if (blob is null)
            {
                throw new ArgumentNullException(nameof(blob));
            }
            if (blob.Length < Length)
            {
                throw new BlobDecodeException($"the blob is {blob.Length} bytes long, shorter than the {Length}-byte header");
            }

            var magic = BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(0, 4));
            var found = magic switch
            {
                AvatarMagic => BlobKind.Avatar,
                DiagnosticsMagic => BlobKind.Diagnostics,
                _ => throw new BlobDecodeException($"magic {BitConverter.ToString(blob, 0, 4)} does not belong to a declavatar blob"),
            };
            if (expected is BlobKind expectedKind && found != expectedKind)
            {
                throw new BlobDecodeException($"expected {Describe(expectedKind)} blob but found {Describe(found)} blob");
            }

            var schemaVersion = BinaryPrimitives.ReadUInt16LittleEndian(blob.AsSpan(4, 2));
            if (schemaVersion != SchemaVersion)
            {
                throw new BlobDecodeException($"schema version {schemaVersion} does not match the supported version {SchemaVersion}; the native library and the client do not match");
            }
            var dataVersion = BinaryPrimitives.ReadUInt16LittleEndian(blob.AsSpan(6, 2));
            if (dataVersion != DataVersionOf(found))
            {
                throw new BlobDecodeException($"{Describe(found)} data version {dataVersion} does not match the supported version {DataVersionOf(found)}; the native library and the client do not match");
            }
            var reserved = BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(8, 4));
            if (reserved != 0)
            {
                throw new BlobDecodeException($"the reserved header field is 0x{reserved:x}, not zero");
            }
            var payloadLength = BinaryPrimitives.ReadUInt32LittleEndian(blob.AsSpan(12, 4));
            var actualLength = blob.Length - Length;
            if (payloadLength != (uint)actualLength)
            {
                throw new BlobDecodeException($"the header declares {payloadLength} payload bytes but {actualLength} follow it");
            }
            return found;
        }

        internal static string Describe(BlobKind kind)
        {
            return kind switch
            {
                BlobKind.Avatar => "an avatar",
                BlobKind.Diagnostics => "a diagnostics",
                _ => throw new ArgumentOutOfRangeException(nameof(kind)),
            };
        }
    }
}
