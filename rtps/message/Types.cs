using System;
using System.Collections.Generic;

namespace rtps
{
    public abstract class Type
    {
        /// <summary>
        /// Writes this type into RTPSByteBuffer
        /// </summary>
        /// <param name="bb">RTPSByteBuffer</param>
        public abstract void WriteTo(RTPSByteBuffer bb);
    }

    public class GuidPrefix : Type
    {
        public static readonly GuidPrefix GUIDPREFIX_UNKNOWN = new GuidPrefix();

        public GuidPrefix()
        {
            throw new NotImplementedException();
        }

        public GuidPrefix(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }
    }

    public class ProtocolVersion : Type
    {
        public static readonly ProtocolVersion PROTOCOLVERSION_2_1 = new ProtocolVersion();

        private byte[] bytes;

        private ProtocolVersion()
        {
            bytes = new byte[] { 2, 1 };
        }

        public ProtocolVersion(RTPSByteBuffer bb)
        {
            bytes = new byte[2];
            bb.read(bytes);
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            bb.write(bytes);
        }
    }


    public sealed class VendorId : Parameter
    {
        public static readonly VendorId JRTPS = new VendorId(new byte[] { (byte)0x01, (byte)0x21 });

        private byte[] bytes;

        private VendorId(byte[] bytes) : base(ParameterId.PID_VENDOR_ID)
        {
            this.bytes = bytes;
        }

        public VendorId(RTPSByteBuffer bb) : base(ParameterId.PID_VENDOR_ID) {
            ReadFrom(bb);
        }


        public override void ReadFrom(RTPSByteBuffer bb)
        {
            bytes = new byte[2];
            bb.read(bytes);
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            bb.write(bytes);
        }
    }

    public class EntityId : Type {
        private byte[] entityKey;

        // 0x02:writer_key, 0x03:writer_no_key,
        // 0x04:reader_no_key, 0x07:reader_key
        private byte entityKind;

        private EntityId(byte[] entityKey, byte entityKind) {
            this.entityKey = entityKey;
            this.entityKind = entityKind;
        }

        internal EntityId(RTPSByteBuffer bb) {
            byte[] eKey = new byte[3];
            bb.read(eKey);
            int kind = bb.read_octet();
        }

        public bool IsBuiltinEntity() {
            return (entityKind & 0xc0) == 0xc0; // @see 9.3.1.2
        }

        public bool IsUserDefinedEntity()
        {
            return (entityKind & 0xc0) == 0x00; // @see 9.3.1.2
        }

        public bool IsVendorSpecifiEntity()
        {
            return (entityKind & 0xc0) == 0x40; // @see 9.3.1.2
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            bb.write(entityKey);
            bb.write_octet(entityKind);
        }
    }

    public class SequenceNumberSet : Type
    {
        private SequenceNumber bmbase;
        private int[] bitmaps;
        private int numBits;

        public long BitmapBase => bmbase.asLong();

        public List<long> SequenceNumbers
        {
            get
            {
                List<long> snList = new List<long>();

                long seqNum = bmbase.asLong();
                int bitCount = 0;

                for (int i = 0; i < bitmaps.Length; i++)
                {
                    int bitmap = bitmaps[i];

                    for (int j = 0; j < 32 && bitCount < numBits; j++)
                    {
                        if ((bitmap & 0x80000000) == 0x80000000)
                        { // Compare MSB to 0x80000000 or 0x0
                            snList.Add(seqNum);
                        }

                        seqNum++;
                        bitCount++;
                        bitmap = bitmap << 1;
                    }
                }

                return snList;
            }
        }


        public SequenceNumberSet(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        public SequenceNumberSet(long bmBase, int[] bitMaps)
        {
            this.bmbase = new SequenceNumber(bmBase);
            this.bitmaps = bitMaps;
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }
    }

    public class SequenceNumber : Type
    {
        private long sn;

        public SequenceNumber(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        public SequenceNumber(long sn)
        {
            this.sn = sn;
        }

        public long asLong()
        {
            return sn;
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }
    }

    public class Time : Type
    {
        private long systemCurrentMillis;

        public Time(long systemCurrentMillis)
        {
            this.systemCurrentMillis = systemCurrentMillis;
        }

        public Time(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }
    }

    public class Locator : Type
    {
        public Locator(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }
    }

    public class LocatorUDPv4_t : Type
    {
        public LocatorUDPv4_t(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }
    }

    public class ParameterList : Type
    {
        private RTPSByteBuffer bb;

        public ParameterList(RTPSByteBuffer bb)
        {
            this.bb = bb;
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        internal Parameter getParameter(object pID_CONTENT_FILTER_INFO)
        {
            throw new NotImplementedException();
        }

        internal int size()
        {
            throw new NotImplementedException();
        }
    }

    public class DataEncapsulation
    {
        public byte[] SerializedPayload { get; internal set; }

        internal bool containsData()
        {
            throw new NotImplementedException();
        }

        internal DataEncapsulation createInstance(byte[] serializedPayload)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusInfo : Parameter
    {
        public StatusInfo() : base(ParameterId.PID_STATUS_INFO) { }

        public override void ReadFrom(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(RTPSByteBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }

    public class ContentFilterInfo : Parameter
    {
        private uint[] bitmaps;
        private Signature[] signatures;

        public ContentFilterInfo() : base(ParameterId.PID_CONTENT_FILTER_INFO) { }

        public override void ReadFrom(RTPSByteBuffer bb)
        {
            this.bitmaps = new uint[bb.read_long()];
            for (int i = 0; i < bitmaps.Length; i++)
            {
                bitmaps[i] = bb.read_long();
            }

            this.signatures = new Signature[bb.read_long()];
            for (int i = 0; i < signatures.Length; i++)
            {
                signatures[i] = new Signature(bb);
            }
        }

        public override void WriteTo(RTPSByteBuffer bb)
        {
            bb.write_long((uint)bitmaps.Length);
            for (int i = 0; i < bitmaps.Length; i++)
            {
                bb.write_long(bitmaps[i]);
            }

            bb.write_long((uint)signatures.Length);
            for (int i = 0; i < signatures.Length; i++)
            {
                bb.write(signatures[i].Bytes);
            }
        }

        public class Signature {
            private byte[] bytes;

            public byte[] Bytes => bytes;

            internal Signature(RTPSByteBuffer bb) {
                bytes = new byte[15];
                bb.read(bytes);
            }
        }
    }

    public enum ParameterId
    {
        PID_SENTINEL = 0x0001,
        PID_VENDOR_ID = 0x0016,
        PID_CONTENT_FILTER_INFO = 0x0055,
        PID_STATUS_INFO = 0x0071,
    }

    public abstract class Parameter
    {
        public ParameterId Id { get; }

        protected Parameter(ParameterId id)
        {
            Id = id;
        }

        public abstract void ReadFrom(RTPSByteBuffer bb);
        public abstract void WriteTo(RTPSByteBuffer buffer);
    }

    public class ParameterFactory
    {
        internal static Parameter readParameter(RTPSByteBuffer bb)
        {
            throw new NotImplementedException();
        }
    }
}