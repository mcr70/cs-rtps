using System;
using System.IO;

namespace rtps.message {
    public class RtpsByteBuffer {
        private readonly MemoryStream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;

        public RtpsByteBuffer(byte[] bytes) {
            _stream = new MemoryStream(bytes);
            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);
        }

        public RtpsByteBuffer() {
            _stream = new MemoryStream(1024);
            _reader = new BinaryReader(_stream);
            _writer = new BinaryWriter(_stream);

            IsLittleEndian = BitConverter.IsLittleEndian;
        }

        // Network byte order is big-endian; most significant byte first.
        // Byte order is checked only on reading, as this involves
        // reading of data sent by remote entities.
        // Write methods use whatever the platform gives us.
        public bool IsLittleEndian { get; internal set; }

        public long Remaining => _stream.Length - _stream.Position;

        public long Position {
            get { return _stream.Position; }
            internal set { _stream.Position = value; }
        }

        public long Capacity => _stream.Length;


        public byte read_octet() {
            return _reader.ReadByte();
        }

        public UInt16 read_short() {
            align(2);

            UInt16 i = _reader.ReadUInt16();
            return IsLittleEndian ? i : SwapBytes(i);
        }

        public UInt32 read_long() {
            align(4);

            UInt32 i = _reader.ReadUInt32();
            return IsLittleEndian ? i : SwapBytes(i);
        }

        public void read(byte[] bytes) {
            _stream.Read(bytes, 0, bytes.Length);
        }

        public void write(byte[] bytes) {
            _stream.Write(bytes, 0, bytes.Length);
        }

        public void write_octet(byte i) {
            _writer.Write(i);
        }

        public void write_short(UInt16 i) {
            align(2);
            _writer.Write(i);
        }

        public void put_short(long position, UInt16 i) {
            long currentPosition = Position;
            Position = position;
            write_short(i);
            Position = currentPosition;
        }

        public void write_long(UInt32 i) {
            align(4);
            _writer.Write(i);
        }


        internal long align(int byteBoundary) {
            long position = _stream.Position;
            long adv = (position % byteBoundary);

            if (adv != 0) {
                _stream.Position = position + (byteBoundary - adv);
            }

            return adv;
        }

        internal UInt16 SwapBytes(UInt16 x) {
            return (UInt16) ((x >> 8) | (x << 8));
        }

        internal UInt32 SwapBytes(UInt32 x) {
            // swap adjacent 16-bit blocks
            x = (x >> 16) | (x << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00) >> 8) | ((x & 0x00FF00FF) << 8);
        }

        private ulong SwapBytesXX(ulong x) { // NOT USED ATM
            // swap adjacent 32-bit blocks
            x = (x >> 32) | (x << 32);
            // swap adjacent 16-bit blocks
            x = ((x & 0xFFFF0000FFFF0000) >> 16) | ((x & 0x0000FFFF0000FFFF) << 16);
            // swap adjacent 8-bit blocks
            return ((x & 0xFF00FF00FF00FF00) >> 8) | ((x & 0x00FF00FF00FF00FF) << 8);
        }
    }
}