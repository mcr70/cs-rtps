using System;

using NUnit.Framework;

namespace rtps
{
    [TestFixture]
    internal class RTPSByteBufferTest
    {
        [TestCase]
        public void testReadingAndWriting() {
            RTPSByteBuffer bb = new RTPSByteBuffer();
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


        [TestCase]
        public void testSwapBytes()
        {
            RTPSByteBuffer bb = new RTPSByteBuffer();

            // test byte swapping of positive number
            UInt16 i16Swapped = bb.SwapBytes(0x1234);
            Assert.AreEqual(0x3412, i16Swapped, "Got 0x" + i16Swapped.ToString("x4"));

            // test byte swapping of negative number
            i16Swapped = bb.SwapBytes(0xFEDC);
            Assert.AreEqual(0xDCFE, i16Swapped, "Got 0x" + i16Swapped.ToString("x4"));

            // test byte swapping of positive number
            UInt32 i32Swapped = bb.SwapBytes(0x0a0b0c0d);
            Assert.AreEqual(0x0d0c0b0a, i32Swapped, "Got 0x" + i32Swapped.ToString("x8"));

            // test byte swapping of negative number
            i32Swapped = bb.SwapBytes(0xa0b0c0d0);
            Assert.AreEqual(0xd0c0b0a0, i32Swapped, "Got 0x" + i32Swapped.ToString("x8"));
        }
    }
}
