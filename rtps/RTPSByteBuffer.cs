using System;
using System.IO;

namespace rtps
{
    public class RTPSByteBuffer
    {
        private MemoryStream stream;
        private BinaryReader reader;
        private BinaryWriter writer;

        public RTPSByteBuffer(byte[] bytes)
        {
            stream = new MemoryStream(bytes);
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);
        }

        public RTPSByteBuffer()
        {
            stream = new MemoryStream(1024);
            reader = new BinaryReader(stream);
            writer = new BinaryWriter(stream);

            IsLittleEndian = BitConverter.IsLittleEndian;
        }

        // Network byte order is big-endian; most significant byte first.
        // Byte order is checked only on reading, as this involves
        // reading of data sent by remote entities.
        // Write methods use whatever the platform gives us.
        public bool IsLittleEndian { get; internal set; }

        public long Remaining
        {
            get { return stream.Length - stream.Position; }
        }

        public long Position
        {
            get { return stream.Position; }
            internal set { stream.Position = value; }
        }

        public long Capacity
        {
            get { return stream.Length; }
        }

        internal byte read_octet()
        {
            return reader.ReadByte();
        }

        internal UInt16 read_short()
        {
			align(2);

			UInt16 i = reader.ReadUInt16();
            if (IsLittleEndian)
            {
                return i;
            }

            return (ushort)SwapBytes(i);
        }

        internal UInt32 read_long()
        {
			align(4);

			UInt32 i = reader.ReadUInt32();
			if (IsLittleEndian)
			{
				return i;
			}

            return (uint)SwapBytes((ulong)i);
		}

        internal void read(byte[] bytes)
        {
            stream.Read(bytes, 0, bytes.Length);
        }

        internal long align(int byteBoundary)
        {
            long position = stream.Position;
            long adv = (position % byteBoundary);

            if (adv != 0)
            {
                stream.Position = position + (byteBoundary - adv);
            }

            return adv;
        }


        internal void write(byte[] bytes)
        {
            stream.Write(bytes, 0, bytes.Length);
        }

        internal void write_short(UInt16 i)
		{
            align(2);
			throw new NotImplementedException();
		}
		
        internal void write_long(UInt32 i)
        {
			align(4);
			throw new NotImplementedException();
        }

        private uint SwapBytes(uint x)
        {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        private ulong SwapBytes(ulong x)
        {
            // swap adjacent 32-bit blocks
            x = (x >> 32) | (x << 32);
            // swap adjacent 16-bit blocks
            x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
        }
    }
}
