using System;
using System.Dynamic;

namespace rtps.message.builtin {
    public abstract class Parameter {
        public ParameterId Id { get; }

        protected Parameter(ParameterId id) {
            Id = id;
        }

        public abstract void WriteTo(RtpsByteBuffer bb);
    }

    public class Sentinel : Parameter {
        public Sentinel() : base(ParameterId.PID_SENTINEL) {
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            // No Content
        }
    }

    public class ProtocolVersion : Parameter {
        private readonly byte[] _version;
        
        public byte Major => _version[0];
        public byte Minor => _version[1];
        
        public ProtocolVersion(byte major, byte minor) : base(ParameterId.PID_PROTOCOL_VERSION) {
            _version = new byte[] { major, minor };
        }
        
        internal ProtocolVersion(RtpsByteBuffer bb) : base(ParameterId.PID_PROTOCOL_VERSION) {
            _version = new byte[2];
            bb.read(_version);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(_version);
        }
    }
    
    public class VendorId : Parameter {
        public static readonly VendorId JRTPS = new VendorId(new byte[] {(byte) 0x01, (byte) 0x21});

        private readonly byte[] _bytes;

        private VendorId(byte[] bytes) : base(ParameterId.PID_VENDORID) {
            _bytes = bytes;
        }

        internal VendorId(RtpsByteBuffer bb) : base(ParameterId.PID_VENDORID) {
            _bytes = new byte[2];
            bb.read(_bytes);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(_bytes);
        }
    }


    public class ParticipantGuid : Parameter {
        public Guid Guid { get; internal set; }

        internal ParticipantGuid(RtpsByteBuffer bb) : base(ParameterId.PID_PARTICIPANT_GUID) {
            Guid = new Guid(bb);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            Guid.WriteTo(bb);
        }
    }

    public enum ChangeKind {
        Write, Dispose, Unregister
    }
    public class StatusInfo : Parameter {
        private readonly byte[] _flags = new byte[4];

        public StatusInfo(params ChangeKind[] kinds) : base(ParameterId.PID_STATUS_INFO) {
            foreach (var k in kinds) {
                switch (k) {
                    case ChangeKind.Dispose:
                        _flags[3] |= 0x1;
                        break;
                    case ChangeKind.Unregister:
                        _flags[3] |= 0x2;
                        break;
                    case ChangeKind.Write:
                        break; // Ignore
                }
            }
        }
        
        public StatusInfo(RtpsByteBuffer bb) : base(ParameterId.PID_STATUS_INFO) {
            bb.read(_flags);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(_flags);
        }
    }

    public class ContentFilterInfo : Parameter {
        private uint[] bitmaps;
        private Signature[] signatures;

        public ContentFilterInfo() : base(ParameterId.PID_CONTENT_FILTER_INFO) {
        }

        public ContentFilterInfo(RtpsByteBuffer bb) : base(ParameterId.PID_CONTENT_FILTER_INFO) {
            bitmaps = new uint[bb.read_long()];
            for (int i = 0; i < bitmaps.Length; i++) {
                bitmaps[i] = bb.read_long();
            }

            signatures = new Signature[bb.read_long()];
            for (int i = 0; i < signatures.Length; i++) {
                signatures[i] = new Signature(bb);
            }
        }


        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long((uint) bitmaps.Length);
            foreach (uint t in bitmaps) {
                bb.write_long(t);
            }

            bb.write_long((uint) signatures.Length);
            foreach (Signature t in signatures) {
                bb.write(t.Bytes);
            }
        }
    }

    public class UnknownParameter : Parameter {
        private readonly ParameterId _id;
        private readonly byte[] _bytes;

        internal UnknownParameter(ParameterId id, byte[] bytes) : base(ParameterId.PID_UNKNOWN_PARAMETER) {
            _id = id;
            _bytes = bytes;
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(_bytes);
        }
    }

    public class BuiltinTopicKey : Parameter {
        public Guid Guid { get; internal set; }

        internal BuiltinTopicKey(RtpsByteBuffer bb) : base(ParameterId.PID_BUILTIN_TOPIC_KEY) {
            Guid = new Guid(bb);
        }

        public BuiltinTopicKey(Guid guid) : base(ParameterId.PID_BUILTIN_TOPIC_KEY) {
            Guid = guid;
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            Guid.WriteTo(bb);
        }
    }

    public class LocatorParam : Parameter {
        public Locator Locator { get; internal set; }

        internal LocatorParam(RtpsByteBuffer bb, ParameterId pid) : base(pid) {
            Locator = new Locator(bb);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            Locator.WriteTo(bb);
        }
    }

    public class EndpointSet : Parameter {
        public uint endpoints { get; internal set; }

        internal EndpointSet(RtpsByteBuffer bb, ParameterId pid) : base(pid) {
            endpoints = bb.read_long();
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long(endpoints);
        }
    }
    
    public class QosUserData : Parameter {
        public byte[] user_data { get; internal set; }

        internal QosUserData(RtpsByteBuffer bb, uint lenght) : base(ParameterId.PID_USER_DATA) {
            user_data = new byte[lenght];
            bb.read(user_data);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(user_data);
        }
    }
    
    public class ParticipantLeaseDuration : Parameter {
        public Duration Duration { get; internal set; }

        internal ParticipantLeaseDuration(RtpsByteBuffer bb) : base(ParameterId.PID_USER_DATA) {
            Duration = new Duration(bb);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            Duration.WriteTo(bb);
        }
    }
    
    // ---------------------------------------------------------------------------

    public class ParameterFactory {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        internal static Parameter ReadParameter(RtpsByteBuffer bb) {
            bb.align(4);

            int paramId = bb.read_short();
            ushort paramLength = 0;

            if (paramId != 0x0001 && paramId != 0x0000) { // SENTINEL & PAD
                paramLength = bb.read_short();
            }

            Parameter param = null;
            switch ((ParameterId) paramId) {
                case ParameterId.PID_BUILTIN_TOPIC_KEY:
                    param = new BuiltinTopicKey(bb);
                    break;
                case ParameterId.PID_UNICAST_LOCATOR:
                case ParameterId.PID_MULTICAST_LOCATOR:
                case ParameterId.PID_DEFAULT_UNICAST_LOCATOR:
                case ParameterId.PID_DEFAULT_MULTICAST_LOCATOR:
                case ParameterId.PID_METATRAFFIC_UNICAST_LOCATOR:
                case ParameterId.PID_METATRAFFIC_MULTICAST_LOCATOR:
                    return new LocatorParam(bb, (ParameterId) paramId);
                case ParameterId.PID_SENTINEL:
                    return new Sentinel();
                case ParameterId.PID_VENDORID:
                    return new VendorId(bb);    
                case ParameterId.PID_PROTOCOL_VERSION:
                    return new ProtocolVersion(bb);
                case ParameterId.PID_PARTICIPANT_GUID:
                    return new ParticipantGuid(bb);
                case ParameterId.PID_PARTICIPANT_BUILTIN_ENDPOINTS:
                case ParameterId.PID_BUILTIN_ENDPOINT_SET:
                    return new EndpointSet(bb, (ParameterId)paramId);
                case ParameterId.PID_USER_DATA:
                    return new QosUserData(bb, paramLength);                    
                case ParameterId.PID_PARTICIPANT_LEASE_DURATION:
                    return new ParticipantLeaseDuration(bb);
                case ParameterId.PID_PAD:
                case ParameterId.PID_PERSISTENCE:
                case ParameterId.PID_TIME_BASED_FILTER:
                case ParameterId.PID_TOPIC_NAME:
                case ParameterId.PID_OWNERSHIP_STRENGTH:
                case ParameterId.PID_TYPE_NAME:
                case ParameterId.PID_TYPE_CHECKSUM:
                case ParameterId.PID_TYPE2_NAME:
                case ParameterId.PID_TYPE2_CHECKSUM:
                case ParameterId.PID_EXPECTS_ACK:
                case ParameterId.PID_METATRAFFIC_MULTICAST_IPADDRESS:
                case ParameterId.PID_DEFAULT_UNICAST_IPADDRESS:
                case ParameterId.PID_METATRAFFIC_UNICAST_PORT:
                case ParameterId.PID_DEFAULT_UNICAST_PORT:
                case ParameterId.PID_MULTICAST_IPADDRESS:
                case ParameterId.PID_MANAGER_KEY:
                case ParameterId.PID_SEND_QUEUE_SIZE:
                case ParameterId.PID_RELIABILITY_ENABLED:
                case ParameterId.PID_VARGAPPS_SEQUENCE_NUMBER_LAST:
                case ParameterId.PID_RECV_QUEUE_SIZE:
                case ParameterId.PID_RELIABILITY_OFFERED:
                case ParameterId.PID_RELIABILITY:
                case ParameterId.PID_LIVELINESS:
                case ParameterId.PID_DURABILITY:
                case ParameterId.PID_DURABILITY_SERVICE:
                case ParameterId.PID_OWNERSHIP:
                case ParameterId.PID_DEADLINE:
                case ParameterId.PID_PRESENTATION:
                case ParameterId.PID_DESTINATION_ORDER:
                case ParameterId.PID_LATENCY_BUDGET:
                case ParameterId.PID_PARTITION:
                case ParameterId.PID_LIFESPAN:
                case ParameterId.PID_GROUP_DATA:
                case ParameterId.PID_TOPIC_DATA:
                case ParameterId.PID_PARTICIPANT_MANUAL_LIVELINESS_COUNT:
                case ParameterId.PID_CONTENT_FILTER_PROPERTY:
                case ParameterId.PID_HISTORY:
                case ParameterId.PID_RESOURCE_LIMITS:
                case ParameterId.PID_EXPECTS_INLINE_QOS:
                case ParameterId.PID_METATRAFFIC_UNICAST_IPADDRESS:
                case ParameterId.PID_METATRAFFIC_MULTICAST_PORT:
                case ParameterId.PID_TRANSPORT_PRIORITY:
                case ParameterId.PID_PARTICIPANT_ENTITYID:
                case ParameterId.PID_GROUP_GUID:
                case ParameterId.PID_GROUP_ENTITYID:
                case ParameterId.PID_CONTENT_FILTER_INFO:
                case ParameterId.PID_COHERENT_SET:
                case ParameterId.PID_DIRECTED_WRITE:
                case ParameterId.PID_PROPERTY_LIST:
                case ParameterId.PID_TYPE_MAX_SIZE_SERIALIZED:
                case ParameterId.PID_ORIGINAL_WRITER_INFO:
                case ParameterId.PID_ENTITY_NAME:
                case ParameterId.PID_KEY_HASH:
                case ParameterId.PID_STATUS_INFO:
                case ParameterId.PID_TYPE_OBJECT:
                case ParameterId.PID_DATA_REPRESENTATION:
                case ParameterId.PID_TYPE_CONSISTENCY_ENFORCEMENT:
                case ParameterId.PID_EQUIVALENT_TYPE_NAME:
                case ParameterId.PID_BASE_TYPE_NAME:
                case ParameterId.PID_SERVICE_INSTANCE_NAME:
                case ParameterId.PID_RELATED_ENTITY_GUID:
                case ParameterId.PID_TOPIC_ALIASES:
                case ParameterId.PID_IDENTITY_TOKEN:
                case ParameterId.PID_PERMISSIONS_TOKEN:
                case ParameterId.PID_DATA_TAGS:
                case ParameterId.PID_UNKNOWN_PARAMETER:
                case ParameterId.PID_X509CERT:
                default:
                    Log.DebugFormat("Reading unknown parameter 0x{0}", paramId.ToString("X4"));
                    var bytes = new byte[paramLength];
                    bb.read(bytes);
                    param = new UnknownParameter((ParameterId) paramId, bytes);
                    break;
            }

            return param;
        }
    }


    public enum ParameterId {
        PID_PAD = 0x0000,
        PID_SENTINEL = 0x0001,
        PID_PARTICIPANT_LEASE_DURATION = 0x0002,
        PID_PERSISTENCE = 0x0003, // Table 9.17: deprecated
        PID_TIME_BASED_FILTER = 0x0004,
        PID_TOPIC_NAME = 0x0005, // string<256>
        PID_OWNERSHIP_STRENGTH = 0x0006,
        PID_TYPE_NAME = 0x0007, // string<256>
        PID_TYPE_CHECKSUM = 0x0008, // Table 9.17: deprecated
        PID_TYPE2_NAME = 0x0009, // Table 9.17: deprecated
        PID_TYPE2_CHECKSUM = 0x000a, // Table 9.17: deprecated
        PID_EXPECTS_ACK = 0x0010, // Table 9.17: deprecated
        PID_METATRAFFIC_MULTICAST_IPADDRESS = 0x000b,
        PID_DEFAULT_UNICAST_IPADDRESS = 0x000c,
        PID_METATRAFFIC_UNICAST_PORT = 0x000d,
        PID_DEFAULT_UNICAST_PORT = 0x000e,
        PID_MULTICAST_IPADDRESS = 0x0011,
        PID_MANAGER_KEY = 0x0012, // Table 9.17: deprecated
        PID_SEND_QUEUE_SIZE = 0x0013, // Table 9.17: deprecated
        PID_RELIABILITY_ENABLED = 0x0014, // Table 9.17: deprecated
        PID_PROTOCOL_VERSION = 0x0015,
        PID_VENDORID = 0x0016,
        PID_VARGAPPS_SEQUENCE_NUMBER_LAST = 0x0017, // Table 9.17: deprecated
        PID_RECV_QUEUE_SIZE = 0x0018, // Table 9.17: deprecated
        PID_RELIABILITY_OFFERED = 0x0019, // Table 9.17: deprecated
        PID_RELIABILITY = 0x001a, // ReliabilityQosPolicy
        PID_LIVELINESS = 0x001b, // LivelinessQosPolicy
        PID_DURABILITY = 0x001d, // DurabilityQosPolicy
        PID_DURABILITY_SERVICE = 0x001e, // DurabilitServiceyQosPolicy
        PID_OWNERSHIP = 0x001f,
        PID_DEADLINE = 0x0023, // DeadLineQosPolicy
        PID_PRESENTATION = 0x0021,
        PID_DESTINATION_ORDER = 0x0025,
        PID_LATENCY_BUDGET = 0x0027, // LatencyBudgetQosPolicy
        PID_PARTITION = 0x0029,
        PID_LIFESPAN = 0x002b, // LifeSpanQosPolicy
        PID_USER_DATA = 0x002c, // UserDataQosPolicy
        PID_GROUP_DATA = 0x002d, // GroupDataQosPolicy
        PID_TOPIC_DATA = 0x002e, // TopicDataQosPolicy
        PID_UNICAST_LOCATOR = 0x002f,
        PID_MULTICAST_LOCATOR = 0x0030,
        PID_DEFAULT_UNICAST_LOCATOR = 0x0031,
        PID_METATRAFFIC_UNICAST_LOCATOR = 0x0032,
        PID_METATRAFFIC_MULTICAST_LOCATOR = 0x0033,
        PID_PARTICIPANT_MANUAL_LIVELINESS_COUNT = 0x0034,
        PID_CONTENT_FILTER_PROPERTY = 0x0035,
        PID_HISTORY = 0x0040,
        PID_RESOURCE_LIMITS = 0x0041,
        PID_EXPECTS_INLINE_QOS = 0x0043,
        PID_PARTICIPANT_BUILTIN_ENDPOINTS = 0x0044,
        PID_METATRAFFIC_UNICAST_IPADDRESS = 0x0045,
        PID_METATRAFFIC_MULTICAST_PORT = 0x0046,
        PID_DEFAULT_MULTICAST_LOCATOR = 0x0048,
        PID_TRANSPORT_PRIORITY = 0x0049,
        PID_PARTICIPANT_GUID = 0x0050,
        PID_PARTICIPANT_ENTITYID = 0x0051,
        PID_GROUP_GUID = 0x0052,
        PID_GROUP_ENTITYID = 0x0053,

        PID_CONTENT_FILTER_INFO = 0x0055, // Table 9.14  
        PID_COHERENT_SET = 0x0056, // Table 9.14
        PID_DIRECTED_WRITE = 0x0057, // Table 9.14
        PID_BUILTIN_ENDPOINT_SET = 0x0058, // Table 9.14
        PID_PROPERTY_LIST = 0x0059,
        PID_BUILTIN_TOPIC_KEY = 0x005a,
        PID_TYPE_MAX_SIZE_SERIALIZED = 0x0060,
        PID_ORIGINAL_WRITER_INFO = 0x0061, // Table 9.14
        PID_ENTITY_NAME = 0x0062,
        PID_KEY_HASH = 0x0070,
        PID_STATUS_INFO = 0x0071,

        // from x-types:
        PID_TYPE_OBJECT = 0x0072,
        PID_DATA_REPRESENTATION = 0x0073,
        PID_TYPE_CONSISTENCY_ENFORCEMENT = 0x0074,
        PID_EQUIVALENT_TYPE_NAME = 0x0075,
        PID_BASE_TYPE_NAME = 0x0076,

        // from DDS RPC:
        PID_SERVICE_INSTANCE_NAME = 0x0080,
        PID_RELATED_ENTITY_GUID = 0x0081,
        PID_TOPIC_ALIASES = 0x0082,

        // from DDS Security:
        PID_IDENTITY_TOKEN = 1001,
        PID_PERMISSIONS_TOKEN = 1002,
        PID_DATA_TAGS = 1003,

        // ----  JRTPS specific parameters  -----------------
        // PID_UNKNOWN_PARAMETER, PID_VENDOR_SPECIFIC are never sent by jRTPS
        // On reception, 
        PID_UNKNOWN_PARAMETER = 0x8000, // 0x8000 is just invented, @see 9.6.2.2.1
        PID_VENDOR_SPECIFIC = 0x8000, // 0x8000 is just invented, @see 9.6.2.2.1
        PID_X509CERT = 0x8ff1
    }
}
