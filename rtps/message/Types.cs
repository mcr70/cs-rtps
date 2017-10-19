using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using rtps.message;
using rtps.message.builtin;

namespace rtps {
    internal static class ExtensionMethods {
        internal static int GetByteArrayHashCode(this byte[] array) {
            unchecked {
                int hash = 17;
                foreach (var value in array) {
                    hash = hash * 23 + value.GetHashCode();            
                }

                return hash;
            }
        }
    }
    
    
    public abstract class Type {
        /// <summary>
        /// Writes this type into RtpsByteBuffer
        /// </summary>
        /// <param name="bb">RtpsByteBuffer</param>
        public abstract void WriteTo(RtpsByteBuffer bb);
    }

    public class Guid : Type {
        public static readonly Guid UNKNOWN = new Guid(GuidPrefix.UNKNOWN, EntityId.UNKNOWN);
        
        public GuidPrefix Prefix { get; }
        public EntityId EntityId { get; }

        internal Guid(RtpsByteBuffer bb) {
            Prefix = new GuidPrefix(bb);
            EntityId = new EntityId(bb);
        }
        
        public Guid(GuidPrefix prefix, EntityId eid) {
            Prefix = prefix;
            EntityId = eid;
        }

        public override int GetHashCode() {
            return Prefix.GetHashCode() ^ EntityId.GetHashCode();
        }

        public override bool Equals(object other) {
            if (other != null && other.GetType() == GetType()) {
                var otherGuid = (Guid)other;
                return Prefix.Equals(otherGuid.Prefix) && EntityId.Equals(otherGuid.EntityId);
            }
            
            return false;
        }

        public override String ToString() {
            return Prefix + ", " + EntityId;
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            Prefix.WriteTo(bb);
            EntityId.WriteTo(bb);
        }
    }
    
    public class GuidPrefix : Type {
        public static readonly GuidPrefix UNKNOWN = new GuidPrefix();
        private readonly byte[] bytes;

        private GuidPrefix() : this(new byte[] {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}) {
        }
   

        public GuidPrefix(byte[] bytes) {
            if (bytes.Length != 12) {
                throw new ArgumentException("Length of byte array for GuidPrefix must be 12");
            }
            
            this.bytes = bytes;
        }
        
        public GuidPrefix(RtpsByteBuffer bb) {
            bytes = new byte[12];
            bb.read(bytes);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(bytes);
        }

        public override bool Equals(object other) {
            if (other != null && GetType() == other.GetType()) {
                GuidPrefix otherPrefix = (GuidPrefix) other;
                return bytes.SequenceEqual(otherPrefix.bytes);
            }
            return false;
        }
        
        public override int GetHashCode() {
            return bytes.GetByteArrayHashCode();
        }

        public override string ToString() {
            return bytes.ToString(",");
        }
    }

    public class ProtocolVersion : Type {
        public static readonly ProtocolVersion PROTOCOLVERSION_2_1 = new ProtocolVersion();

        private readonly byte[] bytes;

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



    public class EntityId : Type {
        public static readonly EntityId UNKNOWN = new EntityId(new byte[] {0, 0, 0}, 0);
        
        public static readonly EntityId SPDP_BUILTIN_PARTICIPANT_WRITER =
            new EntityId(new byte[] { 0, 1, 0 }, 0xc2);
        public static readonly EntityId SPDP_BUILTIN_PARTICIPANT_READER =
            new EntityId(new byte[] { 0, 1, 0 }, 0xc7);

        public static readonly EntityId BUILTIN_PARTICIPANT_MESSAGE_WRITER =
            new EntityId(new byte[] { 0, 2, 0 }, 0xc2);
        public static readonly EntityId BUILTIN_PARTICIPANT_MESSAGE_READER =
            new EntityId(new byte[] { 0, 2, 0 }, 0xc7);

        public static readonly EntityId PARTICIPANT = 
            new EntityId(new byte[] { 0, 0, 1 }, (byte) 0xc1);
        
        public static readonly EntityId SEDP_BUILTIN_TOPIC_WRITER = 
            new EntityId(new byte[] { 0, 0, 2 }, 0xc2);
        public static readonly EntityId SEDP_BUILTIN_TOPIC_READER = 
            new EntityId(new byte[] { 0, 0, 2 }, 0xc7);
        public static readonly EntityId SEDP_BUILTIN_PUBLICATIONS_WRITER =
            new EntityId(new byte[] { 0, 0, 3 }, 0xc2);
        public static readonly EntityId SEDP_BUILTIN_PUBLICATIONS_READER =
            new EntityId(new byte[] { 0, 0, 3 }, 0xc7);
        public static readonly EntityId SEDP_BUILTIN_SUBSCRIPTIONS_WRITER = 
            new EntityId(new byte[] { 0, 0, 4 }, 0xc2);
        public static readonly EntityId SEDP_BUILTIN_SUBSCRIPTIONS_READER = 
            new EntityId(new byte[] { 0, 0, 4 }, 0xc7);

        // From Security
        public static readonly EntityId BUILTIN_PARTICIPANT_STATELESS_WRITER =
            new EntityId(new byte[] { 0, 2, 1 }, 0xc2); 
        public static readonly EntityId BUILTIN_PARTICIPANT_STATELESS_READER =
            new EntityId(new byte[] { 0, 2, 1 }, 0xc7);
        public static readonly EntityId BUILTIN_PARTICIPANT_VOLATILE_MESSAGE_SECURE_WRITER =
            new EntityId(new byte[] { 0xff, 2, 2 }, 0xc2);
        public static readonly EntityId BUILTIN_PARTICIPANT_VOLATILE_MESSAGE_SECURE_READER =
            new EntityId(new byte[] { 0xff, 2, 2 }, 0xc7);

        public static readonly EntityId SEDP_BUILTIN_PUBLICATIONS_SECURE_WRITER =
            new EntityId(new byte[] { 0xff, 0, 3 }, 0xc2);
        public static readonly EntityId SEDP_BUILTIN_PUBLICATIONS_SECURE_READER =
            new EntityId(new byte[] { 0xff, 0, 3 }, 0xc7);
        public static readonly EntityId SEDP_BUILTIN_SUBSCRIPTIONS_SECURE_WRITER =
            new EntityId(new byte[] { 0xff, 0, 4 }, 0xc2);
        public static readonly EntityId SEDP_BUILTIN_SUBSCRIPTIONS_SECURE_READER =
            new EntityId(new byte[] { 0xff, 0, 4 }, 0xc7);
        public static readonly EntityId BUILTIN_PARTICIPANT_MESSAGE_SECURE_WRITER =
            new EntityId(new byte[] { 0xff, 2, 0 }, 0xc2); 
        public static readonly EntityId BUILTIN_PARTICIPANT_MESSAGE_SECURE_READER =
            new EntityId(new byte[] { 0xff, 2, 0 }, 0xc7);
        
        
        private readonly byte[] _entityKey;

        // 0x02:writer_key, 0x03:writer_no_key,
        // 0x04:reader_no_key, 0x07:reader_key
        private readonly byte _entityKind;


        public EntityId(byte[] entityKey, byte entityKind) {
            _entityKey = entityKey;
            _entityKind = entityKind;
        }

        internal EntityId(RtpsByteBuffer bb) {
            _entityKey = new byte[3];
            bb.read(_entityKey);
            _entityKind = bb.read_octet();
        }

        public bool IsBuiltinEntity() {
            return (_entityKind & 0xc0) == 0xc0; // @see 9.3.1.2
        }

        public bool IsUserDefinedEntity() {
            return (_entityKind & 0xc0) == 0x00; // @see 9.3.1.2
        }

        public bool IsVendorSpecifiEntity() {
            return (_entityKind & 0xc0) == 0x40; // @see 9.3.1.2
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(_entityKey);
            bb.write_octet(_entityKind);
        }

        public override bool Equals(object other) {
            if (other != null && typeof(EntityId) == other.GetType()) {
                var otherEntityId = (EntityId) other;
                
                return _entityKind == otherEntityId._entityKind &&
                       _entityKey.SequenceEqual(otherEntityId._entityKey);
            }

            return false;
        }
        
        public override int GetHashCode() {
            byte[] bytes = new byte[4];
            _entityKey.CopyTo(bytes, 0);
            bytes[3] = _entityKind;

            return bytes.GetByteArrayHashCode();
        }

        public override string ToString() {
            return _entityKey.ToString(",") + ":0x" + _entityKind.ToString("X2"); 
        }
    }

    public class SequenceNumberSet : Type {
        private SequenceNumber bmbase;
        private uint[] bitmaps;
        private uint numBits;

        public long BitmapBase => bmbase.asLong();

        public List<long> SequenceNumbers {
            get {
                List<long> snList = new List<long>();

                long seqNum = bmbase.asLong();
                int bitCount = 0;

                for (int i = 0; i < bitmaps.Length; i++) {
                    uint bitmap = bitmaps[i];

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

        public SequenceNumberSet(long _base, uint numBits) {
            this.bmbase = new SequenceNumber(_base);
            this.numBits = numBits;
            this.bitmaps = new uint[(numBits + 31) / 32];
        
            for (int i = 0; i < bitmaps.Length; i++) {
                bitmaps[i] = 0xff;
            }
        }


        public SequenceNumberSet(long bmBase, uint[] bitMaps) {
            bmbase = new SequenceNumber(bmBase);
            bitmaps = bitMaps;
            numBits = (uint) (bitMaps.Length * 32);
        }

        public SequenceNumberSet(RtpsByteBuffer bb) {
            bmbase = new SequenceNumber(bb);

            numBits = bb.read_long();
            uint count = (numBits + 31) / 32;
            bitmaps = new uint[count];
            bb.read(bitmaps);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bmbase.WriteTo(bb);
            bb.write_long(numBits);
            bb.write(bitmaps);
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

        private static readonly DateTime Jan1st1970 = 
            new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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

        public Time() : this((long) (DateTime.UtcNow - Jan1st1970).TotalMilliseconds) {
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
        public static readonly uint LOCATOR_KIND_UDPv4 = 1;
        public static readonly uint LOCATOR_KIND_UDPv6 = 2;

        public uint Kind { get; internal set; }
        public byte[] Address { get; internal set; }
        public uint Port { get; internal set; }

        public Locator(RtpsByteBuffer bb) {
            Kind = bb.read_long();
            Port = bb.read_long();
            Address = new byte[16];

            bb.read(Address);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long(Kind);
            bb.write_long(Port);
            bb.write(Address);
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

    public class ParameterList : Type, IEnumerable<Parameter> {
        private List<Parameter> _parameters = new List<Parameter>();
        internal int Count => _parameters.Count;

        public ParameterList() {
        }

        public ParameterList(RtpsByteBuffer bb) {
            while (true) {
                long pos1 = bb.Position;

                Parameter param = ParameterFactory.ReadParameter(bb);
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

        
        public IEnumerator<Parameter> GetEnumerator() {
            return _parameters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
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


        public class Signature {
            private byte[] bytes;

            public byte[] Bytes => bytes;

            internal Signature(RtpsByteBuffer bb) {
                bytes = new byte[15];
                bb.read(bytes);
            }
        }
    }
