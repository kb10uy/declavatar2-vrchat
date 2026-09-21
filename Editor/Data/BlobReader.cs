using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;

namespace KusakaFactory.Declavatar2.Data
{
    internal sealed class BlobReader
    {
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        private readonly byte[] _bytes;
        private readonly int _start;
        private int _position;

        public BlobReader(byte[] bytes, int start)
        {
            _bytes = bytes;
            _start = start;
            _position = start;
        }

        public int ObjectPathCount { get; set; }
        public int ComponentTypeCount { get; set; }
        public int AssetCount { get; set; }
        public IReadOnlyDictionary<string, AnimatedValueType> Parameters { get; set; } = new Dictionary<string, AnimatedValueType>();
        public int? StateCount { get; set; }

        public int Offset => _position - _start;
        public int Remaining => _bytes.Length - _position;

        public void Finish()
        {
            if (Remaining != 0)
            {
                throw new BlobDecodeException($"{Remaining} bytes remain after the root value");
            }
        }

        public BlobDecodeException InvalidDiscriminator(string typeName, byte value)
        {
            return new BlobDecodeException($"{value} is not a discriminator of {typeName}");
        }

        private ReadOnlySpan<byte> Take(int needed)
        {
            if (needed > Remaining)
            {
                throw new BlobDecodeException($"{needed} bytes are needed at offset {Offset}, but {Remaining} remain");
            }
            var span = new ReadOnlySpan<byte>(_bytes, _position, needed);
            _position += needed;
            return span;
        }

        public byte ReadU8()
        {
            return Take(1)[0];
        }

        public ushort ReadU16()
        {
            return BinaryPrimitives.ReadUInt16LittleEndian(Take(2));
        }

        public uint ReadU32()
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(Take(4));
        }

        public int ReadI32()
        {
            return BinaryPrimitives.ReadInt32LittleEndian(Take(4));
        }

        public long ReadI64()
        {
            return BinaryPrimitives.ReadInt64LittleEndian(Take(8));
        }

        public float ReadF32()
        {
            return BitConverter.Int32BitsToSingle(ReadI32());
        }

        public double ReadF64()
        {
            return BitConverter.Int64BitsToDouble(ReadI64());
        }

        public bool ReadBool()
        {
            var value = ReadU8();
            return value switch
            {
                0 => false,
                1 => true,
                _ => throw new BlobDecodeException($"0x{value:x2} is not a bool"),
            };
        }

        public int ReadLength(string what)
        {
            var value = ReadU32();
            if (value > int.MaxValue)
            {
                throw new BlobDecodeException($"{what} {value} does not fit in this platform's size type");
            }
            return (int)value;
        }

        public string ReadString()
        {
            var length = ReadLength("string length");
            var offset = Offset;
            var bytes = Take(length);
            try
            {
                return StrictUtf8.GetString(bytes);
            }
            catch (DecoderFallbackException)
            {
                throw new BlobDecodeException($"a string at offset {offset} is not valid UTF-8");
            }
        }

        public List<T> ReadList<T>(Func<BlobReader, T> read)
        {
            var count = ReadLength("list length");
            var items = new List<T>();
            for (var i = 0; i < count; i++)
            {
                items.Add(read(this));
            }
            return items;
        }

        public T ReadOption<T>(Func<BlobReader, T> read) where T : class
        {
            return ReadOptionTag() ? read(this) : null;
        }

        public T? ReadOptionValue<T>(Func<BlobReader, T> read) where T : struct
        {
            return ReadOptionTag() ? read(this) : (T?)null;
        }

        public Dictionary<string, T> ReadMap<T>(Func<BlobReader, T> read)
        {
            var count = ReadLength("map length");
            var map = new Dictionary<string, T>();
            for (var i = 0; i < count; i++)
            {
                var key = ReadString();
                var value = read(this);
                if (!map.TryAdd(key, value))
                {
                    throw new BlobDecodeException($"map key `{key}` appears more than once");
                }
            }
            return map;
        }

        private bool ReadOptionTag()
        {
            var tag = ReadU8();
            return tag switch
            {
                0 => false,
                1 => true,
                _ => throw new BlobDecodeException($"0x{tag:x2} is not an option tag"),
            };
        }
    }
}
