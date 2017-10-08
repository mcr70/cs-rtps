using System;
using System.Collections.Generic;

namespace rtps.message {
    public abstract class Type {
        /// <summary>
        /// Writes this type into RTPSByteBuffer
        /// </summary>
        /// <param name="bb">RTPSByteBuffer</param>
        public abstract void WriteTo(RtpsByteBuffer bb);
    }

    public class Guid {
        public EntityId EntityId { get; set; }
        public GuidPrefix Prefix { get; set; }
    }
    
    public class GuidPrefix : Type {
        public static readonly GuidPrefix GUIDPREFIX_UNKNOWN = new GuidPrefix();

        public GuidPrefix() {
            throw new NotImplementedException();
        }

        public GuidPrefix(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class ProtocolVersion : Type {
        public static readonly ProtocolVersion PROTOCOLVERSION_2_1 = new ProtocolVersion();

        private byte[] bytes;

        private ProtocolVersion() {
            bytes = new byte[] {2, 1};
        }

        public ProtocolVersion(RtpsByteBuffer bb) {
            bytes = new byte[2];
            bb.read(bytes);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(bytes);
        }
    }


    public sealed class VendorId : Parameter {
        public static readonly VendorId JRTPS = new VendorId(new byte[] {(byte) 0x01, (byte) 0x21});

        private byte[] bytes;

        private VendorId(byte[] bytes) : base(ParameterId.PID_VENDOR_ID) {
            this.bytes = bytes;
        }

        public VendorId(RtpsByteBuffer bb) : base(ParameterId.PID_VENDOR_ID) {
            ReadFrom(bb);
        }


        public override void ReadFrom(RtpsByteBuffer bb) {
            bytes = new byte[2];
            bb.read(bytes);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
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

        internal EntityId(RtpsByteBuffer bb) {
            byte[] eKey = new byte[3];
            bb.read(eKey);
            int kind = bb.read_octet();
        }

        public bool IsBuiltinEntity() {
            return (entityKind & 0xc0) == 0xc0; // @see 9.3.1.2
        }

        public bool IsUserDefinedEntity() {
            return (entityKind & 0xc0) == 0x00; // @see 9.3.1.2
        }

        public bool IsVendorSpecifiEntity() {
            return (entityKind & 0xc0) == 0x40; // @see 9.3.1.2
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(entityKey);
            bb.write_octet(entityKind);
        }
    }

    public class SequenceNumberSet : Type {
        private SequenceNumber bmbase;
        private int[] bitmaps;
        private int numBits;

        public long BitmapBase => bmbase.asLong();

        public List<long> SequenceNumbers {
            get {
                List<long> snList = new List<long>();

                long seqNum = bmbase.asLong();
                int bitCount = 0;

                for (int i = 0; i < bitmaps.Length; i++) {
                    int bitmap = bitmaps[i];

                    for (int j = 0; j < 32 && bitCount < numBits; j++) {
                        if ((bitmap & 0x80000000) == 0x80000000) { // Compare MSB to 0x80000000 or 0x0
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


        public SequenceNumberSet(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }

        public SequenceNumberSet(long bmBase, int[] bitMaps) {
            this.bmbase = new SequenceNumber(bmBase);
            this.bitmaps = bitMaps;
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class SequenceNumber : Type {
        private long sn;

        public SequenceNumber(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }

        public SequenceNumber(long sn) {
            this.sn = sn;
        }

        public long asLong() {
            return sn;
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class Time : Type {
        public static readonly Time TIME_ZERO = new Time(0, 0);
        public static readonly Time TIME_INVALID = new Time(0xffffffff, 0xffffffff);
        public static readonly Time TIME_INFINITE = new Time(0x7fffffff, 0xffffffff);

        private uint seconds;
        private uint fraction;
        
        public Time(RtpsByteBuffer bb) {
            seconds = bb.read_long() & 0x7fffffff; // long
            fraction = bb.read_long(); // ulong
        }

        internal Time(uint sec, uint frac) {
            seconds = sec;
            fraction = frac;
        }
        
        public Time(long systemCurrentMillis) {
            seconds = (uint) (systemCurrentMillis / 1000);
        
            long scm = this.seconds * 1000; 
        
            fraction = (uint) (systemCurrentMillis - scm);
        }


        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long(seconds);
            bb.write_long(fraction);
        }
    }

    public class Locator : Type {
        private uint kind;
        private uint port;
        private byte[] address;

        public Locator(RtpsByteBuffer bb) {
            kind = bb.read_long();
            port = bb.read_long();
            address = new byte[16];

            bb.read(address);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long(kind);
            bb.write_long(port);
            bb.write(address);
        }
    }

    public class LocatorUDPv4_t : Type {
        private uint address;
        private uint port;

        public LocatorUDPv4_t(RtpsByteBuffer bb) {
            address = bb.read_long();
            port = bb.read_long();
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long(address);
            bb.write_long(port);
        }
    }

    public class ParameterList : Type {
        private List<Parameter> _parameters = new List<Parameter>();

        public ParameterList(RtpsByteBuffer bb) {
            while (true) {
                long pos1 = bb.Position;

                Parameter param = ParameterFactory.readParameter(bb);
                _parameters.Add(param);
                long length = bb.Position - pos1;

                if (param.Id == ParameterId.PID_SENTINEL) {
                    break;
                }
            }
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.align(4); // @see 9.4.2.11

            _parameters.Add(new Sentinel()); // Sentinel must be the last Parameter
            foreach (Parameter param in _parameters) {
                bb.write_short((ushort)param.Id);
                bb.write_short(0); // length will be calculated

                long pos = bb.Position;
                param.WriteTo(bb);

                bb.align(4); // Make sure length is multiple of 4 & align for
                // next param

                long paramLength = bb.Position - pos;
                long nextPos = bb.Position;
                bb.Position = pos - 2;
                bb.write_short((ushort)paramLength);
                bb.Position = nextPos;
            }
        }

        internal Parameter getParameter(ParameterId id) {
            foreach (var p in _parameters) {
                if (p.Id.Equals(id)) {
                    return p;
                }
            }

            return null;
        }

        internal int Count => _parameters.Count;
    }

    public class DataEncapsulation {
        public byte[] SerializedPayload { get; internal set; }

        internal bool containsData() {
            throw new NotImplementedException();
        }

        internal DataEncapsulation createInstance(byte[] serializedPayload) {
            throw new NotImplementedException();
        }
    }

    public class Sentinel : Parameter {
        public Sentinel() : base(ParameterId.PID_SENTINEL) {
        }

        public override void ReadFrom(RtpsByteBuffer bb) {
            // No Content
        }

        public override void WriteTo(RtpsByteBuffer buffer) {
            // No Content
        }
    }
    
    public class StatusInfo : Parameter {
        public StatusInfo() : base(ParameterId.PID_STATUS_INFO) {
        }

        public override void ReadFrom(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }

        public override void WriteTo(RtpsByteBuffer buffer) {
            throw new NotImplementedException();
        }
    }

    public class ContentFilterInfo : Parameter {
        private uint[] bitmaps;
        private Signature[] signatures;

        public ContentFilterInfo() : base(ParameterId.PID_CONTENT_FILTER_INFO) {
        }

        public override void ReadFrom(RtpsByteBuffer bb) {
            this.bitmaps = new uint[bb.read_long()];
            for (int i = 0; i < bitmaps.Length; i++) {
                bitmaps[i] = bb.read_long();
            }

            this.signatures = new Signature[bb.read_long()];
            for (int i = 0; i < signatures.Length; i++) {
                signatures[i] = new Signature(bb);
            }
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long((uint) bitmaps.Length);
            for (int i = 0; i < bitmaps.Length; i++) {
                bb.write_long(bitmaps[i]);
            }

            bb.write_long((uint) signatures.Length);
            for (int i = 0; i < signatures.Length; i++) {
                bb.write(signatures[i].Bytes);
            }
        }

        public class Signature {
            private byte[] bytes;

            public byte[] Bytes => bytes;

            internal Signature(RtpsByteBuffer bb) {
                bytes = new byte[15];
                bb.read(bytes);
            }
        }
    }

    public enum ParameterId {
        PID_SENTINEL = 0x0001,
        PID_VENDOR_ID = 0x0016,
        PID_CONTENT_FILTER_INFO = 0x0055,
        PID_STATUS_INFO = 0x0071,
    }

    public abstract class Parameter {
        public ParameterId Id { get; }

        protected Parameter(ParameterId id) {
            Id = id;
        }

        public abstract void ReadFrom(RtpsByteBuffer bb);
        public abstract void WriteTo(RtpsByteBuffer buffer);
    }

    public class ParameterFactory {
        internal static Parameter readParameter(RtpsByteBuffer bb) {
            throw new NotImplementedException();
        }
    }
}