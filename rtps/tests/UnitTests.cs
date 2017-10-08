
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

        
        [TestCase((ushort)0x1234, ExpectedResult = 0x3412)] // test swapping of positive number
        [TestCase((ushort)0xFEDC, ExpectedResult = 0xDCFE)] // test swapping of negative number
        public ushort TestSwapBytes16(ushort i)
        {
            var bb = new RtpsByteBuffer();
            return bb.SwapBytes(i);
        }

        [TestCase((uint)0x0a0b0c0d, ExpectedResult = 0x0d0c0b0a)] // test swapping of positive number
        [TestCase((uint)0xa0b0c0d0, ExpectedResult = 0xd0c0b0a0)] // test swapping of negative number
        public uint TestSwapBytes32(uint i)
        {
            var bb = new RtpsByteBuffer();
            return bb.SwapBytes(i);
        }
    }
}
