using System;
using System.Collections.Generic;

namespace rtps {
    public class GuidPrefix {
        public static readonly GuidPrefix GUIDPREFIX_UNKNOWN = new GuidPrefix();

        public GuidPrefix() {
            throw new NotImplementedException();
        }

        public GuidPrefix(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class ProtocolVersion {
        public static readonly ProtocolVersion PROTOCOLVERSION_2_1 = new ProtocolVersion();

        private byte[] bytes;

        private ProtocolVersion() {
            bytes = new byte[] {2, 1};
        }

        public ProtocolVersion(RTPSByteBuffer bb) {
            bytes = new byte[2];
            bb.read(bytes);
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class VendorId {
        public static readonly VendorId JRTPS = new VendorId(new byte[] {(byte) 0x01, (byte) 0x21});

        private byte[] bytes;

        private VendorId(byte[] bytes) {
            this.bytes = bytes;
        }

        public VendorId(RTPSByteBuffer bb) {
            bytes = new byte[2];
            bb.read(bytes);
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class EntityId {
        internal static EntityId readEntityId(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class SequenceNumberSet {
        private SequenceNumber bmbase;
        private int[] bitmaps;
        private int numBits;

        public long BitmapBase {
            get { return bmbase.asLong(); }
        }

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


        public SequenceNumberSet(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        public SequenceNumberSet(long bmBase, int[] bitMaps) {
            this.bmbase = new SequenceNumber(bmBase);
            this.bitmaps = bitMaps;
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class SequenceNumber {
        private long sn;

        public SequenceNumber(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        public SequenceNumber(long sn) {
            this.sn = sn;
        }

        public long asLong() {
            return sn;
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class Time {
        private long systemCurrentMillis;

        public Time(long systemCurrentMillis) {
            this.systemCurrentMillis = systemCurrentMillis;
        }

        public Time(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class Locator {
        public Locator(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class LocatorUDPv4_t {
        public LocatorUDPv4_t(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }

    public class ParameterList {
        private RTPSByteBuffer bb;

        public ParameterList(RTPSByteBuffer bb) {
            this.bb = bb;
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }

        internal Parameter getParameter(object pID_CONTENT_FILTER_INFO) {
            throw new NotImplementedException();
        }

        internal int size() {
            throw new NotImplementedException();
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

    public class StatusInfo : Parameter {
    }

    public class ContentFilterInfo : Parameter {
    }

    public enum ParameterId {
        PID_CONTENT_FILTER_INFO,
        PID_SENTINEL,
        PID_STATUS_INFO
    }

    public class Parameter {
        public ParameterId ParameterId { get; internal set; }

        internal void writeTo(RTPSByteBuffer buffer) {
            throw new NotImplementedException();
        }
    }

    public class ParameterFactory {
        internal static Parameter readParameter(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }
}