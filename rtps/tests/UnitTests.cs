
using NUnit.Framework;
using rtps.message;

namespace rtps.tests
{
    [TestFixture]
    internal class RtpsByteBufferTest
    {
        [TestCase]
        public void TestReadingAndWriting() {
            var bb = new RtpsByteBuffer();
            bb.write_octet(0xab);
            Assert.AreEqual(1, bb.Position);

            bb.Position = 0;
            Assert.AreEqual(0xab, bb.read_octet());

            // Write uint16, alignment 2
            bb.write_short(0x1234);
            Assert.AreEqual(4, bb.Position);

            bb.Position = 2;
            Assert.AreEqual(0x1234, bb.read_short());

            // Write uint32, alignment 4
            bb.Position = 1;
            bb.write_long(0x01020304);
            Assert.AreEqual(8, bb.Position);

            bb.Position = 4;
            Assert.AreEqual(0x01020304, bb.read_long());
        }


        [TestCase((ushort)0x1234, true, ExpectedResult = 0x1234)] // test positive little endian
        [TestCase((ushort)0xFEDC, true, ExpectedResult = 0xFEDC)] // test negative little endian
        [TestCase((ushort)0x1234, false, ExpectedResult = 0x1234)] // test positive big endian
        [TestCase((ushort)0xFEDC, false, ExpectedResult = 0xFEDC)] // test negative big endian
        public ushort TestEndianness16(ushort i, bool littleEndian)
        {
            var bb = new RtpsByteBuffer();
            bb.IsLittleEndian = littleEndian;
            bb.write_short(i);
            bb.Position = 0;

            return bb.read_short();
        }

        [TestCase((uint)0x0a0b0c0d, true, ExpectedResult = 0x0a0b0c0d)] // test positive little endian
        [TestCase((uint)0xa0b0c0d0, true, ExpectedResult = 0xa0b0c0d0)] // test negative little endian
        [TestCase((uint)0x0a0b0c0d, false, ExpectedResult = 0x0a0b0c0d)] // test positive big endian
        [TestCase((uint)0xa0b0c0d0, false, ExpectedResult = 0xa0b0c0d0)] // test negative big endian
        public uint TestEndianness32(uint i, bool littleEndian)
        {
            var bb = new RtpsByteBuffer();
            bb.IsLittleEndian = littleEndian;
            bb.write_long(i);
            bb.Position = 0;
            return bb.read_long();
        }

    }
}
