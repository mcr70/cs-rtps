using System;
namespace rtps
{
    public class RTPSByteBuffer
    {
        public RTPSByteBuffer()
        {
        }

        public int Remaining { get; internal set;  }
        public int Position { get; internal set; }
		public int Capacity { get; internal set; }
		public bool Endianess { get; internal set; }

        internal sbyte read_octet()
        {
            throw new NotImplementedException();
        }

        internal int read_short()
        {
            throw new NotImplementedException();
        }


        internal void read(byte[] bytes) {
            throw new NotImplementedException();
        }

        internal void write(byte[] bytes)
        {
            throw new NotImplementedException();
        }

        internal void align(int v)
        {
            throw new NotImplementedException();
        }

        internal int read_long()
        {
            throw new NotImplementedException();
        }

        internal void write_long(int count)
        {
            throw new NotImplementedException();
        }

        internal void write_short(short extraFlags)
        {
            throw new NotImplementedException();
        }
    }
}
