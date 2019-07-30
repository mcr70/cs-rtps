using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace rtps.message.builtin {
    /// <summary>
    /// A Base class for all the builtin parameters
    /// </summary>
    public abstract class Parameter {
        public ParameterId Id { get; }

        protected Parameter(ParameterId id) {
            Id = id;
        }

        /// <summary>
        /// Reads the Parameter from given RtpsByteBuffer. The length of the Parameter is given as
        /// an argument to this method. Some Parameters might make use of the length parameter, other might not.
        /// For example, QosUserData parameter provides an arbitrary long length, which is needed byt the
        /// implementation to read correct amount of octets from the stream.
        /// On the other hand, when reading ParticipantGuid the length can be ignored, as it is already known
        /// by the implementation (is constant).
        /// </summary>
        /// <param name="bb">RtpsByteBuffer</param>
        /// <param name="length">Length of the parameter extracted from stream</param>
        public abstract void ReadFrom(RtpsByteBuffer bb, ushort length);

        /// <summary>
        /// Writes the Parameter into given RtpsByteBuffer. Writing should not write the length of
        /// Parameter into stream. Is is calculated and stored into stream into correct position.
        /// </summary>
        /// <param name="bb">RtpsByteBuffer</param>
        public abstract void WriteTo(RtpsByteBuffer bb);
    }

    public class ParameterFactory
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<ushort, System.Type> parameterTypes = new Dictionary<ushort, System.Type>();

        /// <summary>
        /// Static Constructor that loads all the Parameters from _this_ Assebly
        /// </summary>
        static ParameterFactory()
        {
            ScanAssemblies(new Assembly[]{ Assembly.GetAssembly(typeof(Parameter)) });
        }

        /// <summary>
        /// Scans all the Parameters defined in all the assemblies given. By default, only internal
        /// Parameters are read. This method provides a way to add other Parameters from other assemblies
        /// into csrtps
        /// </summary>
        /// <param name="assemblies"></param>
        public static void ScanAssemblies(Assembly[] assemblies)
        {
            foreach (System.Type type in assemblies
                .SelectMany(a => a.GetTypes())
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(Parameter))))
            {
                Parameter p = (Parameter)Activator.CreateInstance(type, true);
                if (!parameterTypes.ContainsKey((ushort)p.Id))
                {
                    parameterTypes[(ushort)p.Id] = type;
                }
            }
        }

        /// <summary>
        /// Scans all the Parameters defined in all the assemblies defined in this AppDomain.
        /// </summary>
        public static void ScanAllAssemblies()
        {
            ScanAssemblies(AppDomain.CurrentDomain.GetAssemblies());
        }

        public override string ToString()
        {
            return "Known parameters: " + string.Join(",", parameterTypes.Keys);
        }

        static internal Parameter ReadParameter(RtpsByteBuffer bb)
        {
            bb.align(4);

            ushort paramId = bb.read_short();
            ushort paramLength = bb.read_short();

            Parameter param;
            if (parameterTypes.ContainsKey(paramId))
            {
                param = (Parameter)Activator.CreateInstance(parameterTypes[paramId]);
            }
            else
            {
                param = new UnknownParameter(paramId, paramLength);
            }

            param.ReadFrom(bb, paramLength);
            return param;
        }
    }


    public class Sentinel : Parameter {
        internal Sentinel() : base(ParameterId.PID_SENTINEL) {
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            // No Content
        }
        public override void WriteTo(RtpsByteBuffer bb) {
            // No Content
        }
    }


    
    public class ParticipantGuid : Parameter {
        public Guid Guid { get; internal set; }

        public ParticipantGuid(Guid guid) : base(ParameterId.PID_PARTICIPANT_GUID) {
            this.Guid = guid;
        }

        internal ParticipantGuid() : base(ParameterId.PID_PARTICIPANT_GUID) { }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
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
        private byte[] _flags = new byte[4];

        public bool IsDispose => (_flags[3] & 0x1) != 0;
        public bool IsUnregister => (_flags[3] & 0x2) != 0;
        public bool IsWrite => !(IsDispose && IsUnregister);

        internal StatusInfo() : base(ParameterId.PID_STATUS_INFO) {}

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

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            _flags = new byte[length];
            bb.read(_flags);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(_flags);
        }
    }

    public class ContentFilterInfo : Parameter {
        private uint[] bitmaps;
        private Signature[] signatures;

        internal ContentFilterInfo() : base(ParameterId.PID_CONTENT_FILTER_INFO) {
        }

        public ContentFilterInfo(uint[] bitmaps, Signature[] signatures) : base(ParameterId.PID_CONTENT_FILTER_INFO)
        {
            this.bitmaps = bitmaps;
            this.signatures = signatures;
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
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
                t.WriteTo(bb);
            }
        }
    }


    public class BuiltinTopicKey : Parameter {
        public Guid Guid { get; internal set; }

        internal BuiltinTopicKey() : base(ParameterId.PID_BUILTIN_TOPIC_KEY){ }
        public BuiltinTopicKey(Guid guid) : base(ParameterId.PID_BUILTIN_TOPIC_KEY) {
            Guid = guid;
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            Guid = new Guid(bb);
        }
        public override void WriteTo(RtpsByteBuffer bb) {
            Guid.WriteTo(bb);
        }
    }

    public class TopicName : Parameter
    {
        public string Name { get; internal set; }

        internal TopicName() : base(ParameterId.PID_TOPIC_NAME) { }
        public TopicName(string name) : base(ParameterId.PID_TOPIC_NAME)
        {
            Name = name;
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            Name = bb.read_string();
        }
        public override void WriteTo(RtpsByteBuffer bb)
        {
            bb.write_string(Name);
        }
    }

    public class TypeName : Parameter
    {
        public string Name { get; internal set; }

        internal TypeName() : base(ParameterId.PID_TYPE_NAME) { }
        public TypeName(string name) : base(ParameterId.PID_TYPE_NAME)
        {
            Name = name;
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            Name = bb.read_string();
        }
        public override void WriteTo(RtpsByteBuffer bb)
        {
            bb.write_string(Name);
        }
    }

    public abstract class LocatorParam : Parameter {
        public Locator Locator { get; internal set; }

        protected LocatorParam(ParameterId pid) : base(pid) {
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            Locator = new Locator(bb);
        }
        public override void WriteTo(RtpsByteBuffer bb) {
            Locator.WriteTo(bb);
        }
    }

    public class EndpointSet : Parameter {
        public uint endpoints { get; internal set; }

        // TODO: PID_BUILTIN_ENDPOINT_SET  _and_  PID_PARTICIPANT_BUILTIN_ENDPOINTS

        internal EndpointSet() : base(ParameterId.PID_BUILTIN_ENDPOINT_SET) {}

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            endpoints = bb.read_long();
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long(endpoints);
        }
    }
    
    public class QosUserData : Parameter {
        public byte[] user_data { get; internal set; }

        internal QosUserData() : base(ParameterId.PID_USER_DATA) { }
        public QosUserData(byte[] bytes) : base(ParameterId.PID_USER_DATA)
        {
            user_data = bytes;
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length) { 
            user_data = new byte[length];
            bb.read(user_data);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(user_data);
        }
    }
    
    public class ParticipantLeaseDuration : Parameter {
        public Duration Duration { get; internal set; }

        internal ParticipantLeaseDuration() : base(ParameterId.PID_PARTICIPANT_LEASE_DURATION) { }
        public ParticipantLeaseDuration(Duration d) : base(ParameterId.PID_PARTICIPANT_LEASE_DURATION) {
            Duration = d;
        }
        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            Duration = new Duration(bb);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            Duration.WriteTo(bb);
        }
    }

    /// Unknown parameter is not used by csrtps. It is only created when 3rd party RTPS participant
    /// sends a parameter that is not recognized by csrtps.
    /// It is passed to upper layer if implementing class wishes to make use of it.
    public class UnknownParameter : Parameter
    {
        private readonly ushort _length;
        public ushort UnknownId { get; }
        public byte[] Bytes { get; internal set; }

        public UnknownParameter() : base(ParameterId.PID_UNKNOWN_PARAMETER) {  } // Constructor is NOT USED byt csrtps
        internal UnknownParameter(ushort id, ushort length) : base(ParameterId.PID_UNKNOWN_PARAMETER)
        {
            UnknownId = id;
            _length = length;
        }

        public override void ReadFrom(RtpsByteBuffer bb, ushort length)
        {
            Bytes = new byte[_length];
            bb.read(Bytes);
        }

        public override void WriteTo(RtpsByteBuffer bb)
        {
            bb.write(Bytes);
        }
    }


    // ---------------------------------------------------------------------------


    public enum ParameterId : ushort {
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
