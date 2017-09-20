using System;
using System.Text;
using System.Collections.Generic;

namespace rtps {
    /// <summary>
    /// This class represents a RTPS message.
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class Message {
        private Header header;
        private IList<SubMessage> submessages = new List<SubMessage>();

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="prefix"> GuidPrefix </param>
        public Message(GuidPrefix prefix) : this(new Header(prefix)) {
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="header"> Header </param>
        public Message(Header header) {
            this.header = header;
        }

        /// <summary>
        /// Constructs a Message from given RTPSByteBuffer.
        /// </summary>
        /// <param name="bb"> Reads Message from this RTPSByteBuffer </param>
        /// <exception cref="IllegalMessageException"> If message could not be parsed </exception>
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: public Message(net.sf.jrtps.transport.RTPSByteBuffer bb) throws IllegalMessageException
        public Message(RTPSByteBuffer bb) {
            header = new Header(bb);

            while (bb.Remaining > 0) {
                bb.align(4);
                if (!(bb.Remaining > 0)) {
                    // Data submessage may contain sentinel as last, which may cause
                    // alignement error. Break from the loop if we are at the end
                    // of buffer
                    break;
                }

                SubMessageHeader smh = new SubMessageHeader(bb);
                bb.IsLittleEndian = smh.EndiannessFlag();

                long smStart = bb.Position;

                SubMessage sm = null;

                switch (smh.kind) {
                    case Pad.KIND:
                        sm = new Pad(smh, bb);
                        break;
                    case AckNack.KIND:
                        sm = new AckNack(smh, bb);
                        break;
                    case Heartbeat.KIND:
                        sm = new Heartbeat(smh, bb);
                        break;
                    case Gap.KIND:
                        sm = new Gap(smh, bb);
                        break;
                    case InfoTimestamp.KIND:
                        sm = new InfoTimestamp(smh, bb);
                        break;
                    case InfoSource.KIND:
                        sm = new InfoSource(smh, bb);
                        break;
                    case InfoReplyIp4.KIND:
                        sm = new InfoReplyIp4(smh, bb);
                        break;
                    case InfoDestination.KIND:
                        sm = new InfoDestination(smh, bb);
                        break;
                    case InfoReply.KIND:
                        sm = new InfoReply(smh, bb);
                        break;
                    case NackFrag.KIND:
                        sm = new NackFrag(smh, bb);
                        break;
                    case HeartbeatFrag.KIND:
                        sm = new HeartbeatFrag(smh, bb);
                        break;
                    case Data.KIND:
                        sm = new Data(smh, bb);
                        break;
                    case DataFrag.KIND:
                        sm = new DataFrag(smh, bb);
                        break;
                    case SecureSubMessage.KIND:
                        sm = new SecureSubMessage(smh, bb);
                        break;

                    default:
                        sm = new UnknownSubMessage(smh, bb);
                        break;
                }

                long smEnd = bb.Position;
                long smLength = smEnd - smStart;

                submessages.Add(sm);
            }
        }


        /// <summary>
        /// Gets the Header of this Message.
        /// </summary>
        /// <returns> Header </returns>
        public virtual Header Header {
            get { return header; }
        }

        /// <summary>
        /// Gets all the SubMessages of this Message.
        /// </summary>
        /// <returns> List of SubMessages. Returned List is never null. </returns>
        public virtual IList<SubMessage> SubMessages {
            get { return submessages; }
        }

        /// <summary>
        /// Writes this Message to given RTPSByteBuffer. During writing of each
        /// SubMessage, its length is calculated and
        /// SubMessageHeader.submessageLength is updated. If an overflow occurs
        /// during writing, buffer position is set to the start of submessage that
        /// caused overflow.
        /// </summary>
        /// <param name="buffer"> RTPSByteBuffer to write to </param>
        /// <returns> true, if an overflow occured during write. </returns>
        //public virtual bool writeTo(RTPSByteBuffer buffer)
        //{
        //	header.writeTo(buffer);
        //	bool overFlowed = false;

        //	int position = 0;
        //	int subMessageCount = 0;
        //	foreach (SubMessage msg in submessages)
        //	{
        //		int subMsgStartPosition = buffer.position();

        //		try
        //		{
        //			SubMessageHeader hdr = msg.Header;
        //			buffer.align(4);
        //			buffer.Endianess = hdr.endiannessFlag(); // Set the endianess
        //			hdr.writeTo(buffer);

        //			position = buffer.position();
        //			msg.writeTo(buffer);
        //			int subMessageLength = buffer.position() - position;

        //			// Position to 'submessageLength' -2 is for short (2 bytes)
        //			// buffers current position is not changed
        //			buffer.Buffer.putShort(position - 2, (short) subMessageLength);

        //			subMessageCount++;

        //			log.trace("SubMsg out: {}", msg);
        //		}
        //		catch (BufferOverflowException)
        //		{
        //			log.warn("Buffer overflow occured after {} succesful sub-message writes, dropping rest of the sub messages", subMessageCount);
        //			buffer.Buffer.position(subMsgStartPosition);
        //			overFlowed = true;
        //			break;
        //		}
        //	}

        //	// Length of last submessage is 0, @see 8.3.3.2.3 submessageLength
        //	if (subMessageCount > 0)
        //	{
        //		buffer.Buffer.putShort(position - 2, (short) 0);
        //	}

        //	return overFlowed;
        //}
        /// <summary>
        /// Adds a new SubMessage to this Message. SubMessage must well formed.
        /// </summary>
        /// <param name="sm"> SubMessage to add </param>
        public virtual void addSubMessage(SubMessage sm) {
            submessages.Add(sm);
        }

        public override string ToString() {
            return Header + ", " + SubMessages;
        }
    }


    /// <summary>
    /// The Header identifies the message as belonging to the RTPS protocol. The
    /// Header identifies the version of the protocol and the vendor that sent the
    /// message.
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class Header {
        private static readonly byte[] HDR_START = new byte[] {(byte) 'R', (byte) 'T', (byte) 'P', (byte) 'S'};

        // private ProtocolId_t protocol;
        private byte[] hdrStart;

        private ProtocolVersion version;
        private VendorId vendorId;
        private GuidPrefix guidPrefix;


        /// <summary>
        /// Defines a default prefix to use for all GUIDs that appear within the
        /// Submessages contained in the message. </summary>
        /// <returns> GuidPrefix associated with submessages </returns>
        public virtual GuidPrefix GuidPrefix => guidPrefix;

        /// <summary>
        /// Indicates the vendor that provides the implementation of the RTPS
        /// protocol. </summary>
        /// <returns> VendorId </returns>
        public virtual VendorId VendorId => vendorId;

        /// <summary>
        /// Identifies the version of the RTPS protocol. </summary>
        /// <returns> ProtocolVersion </returns>
        public virtual ProtocolVersion Version => version;


        /// <summary>
        /// Constructor for Header. ProtocolVersion is set to 2.1 and VendorId is set
        /// to jRTPS.
        /// </summary>
        /// <param name="prefix"> GuidPrefix of the header </param>
        public Header(GuidPrefix prefix) : this(prefix, ProtocolVersion.PROTOCOLVERSION_2_1, VendorId.JRTPS) {
        }

        /// <summary>
        /// Constructor with given values. </summary>
        /// <param name="prefix"> GuidPrefix </param>
        /// <param name="version"> Version of the RTPS protocol </param>
        /// <param name="vendorId"> VendorId </param>
        public Header(GuidPrefix prefix, ProtocolVersion version, VendorId vendorId) {
            this.hdrStart = HDR_START;
            this.guidPrefix = prefix;
            this.version = version;
            this.vendorId = vendorId;
        }

        /// <summary>
        /// Constructs Header from given RTPSByteBuffer.
        /// </summary>
        /// <param name="bb"> </param>
        /// <exception cref="IllegalMessageException">  </exception>
        internal Header(RTPSByteBuffer bb) {
            if (bb.Remaining < 20) {
                throw new IllegalMessageException("Message length must be at least 20 bytes, was " + bb.Position);
            }

            hdrStart = new byte[4];
            bb.read(hdrStart);
            if (!Array.Equals(HDR_START, hdrStart)) {
                throw new IllegalMessageException("Illegal message header start bytes: " + string.Join(",", hdrStart) +
                                                  ", expected " + string.Join(",", HDR_START));
            }

            version = new ProtocolVersion(bb);
            vendorId = new VendorId(bb);
            guidPrefix = new GuidPrefix(bb);
        }

        /// <summary>
        /// Writer this Header to given RTPSByteBuffer.
        /// </summary>
        /// <param name="bb"> RTPSByteBuffer to write to </param>
        public virtual void writeTo(RTPSByteBuffer bb) {
            bb.write(hdrStart);
            version.writeTo(bb);
            vendorId.writeTo(bb);
            guidPrefix.writeTo(bb);
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append(version.ToString());
            sb.Append(", ");
            sb.Append(vendorId.ToString());
            sb.Append(", ");
            sb.Append(guidPrefix.ToString());

            return sb.ToString();
        }
    }


    /// <summary>
    /// An abstract Base class for known sub-messages.
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public abstract class SubMessage {
        protected internal readonly SubMessageHeader header;

        /// <summary>
        /// Different kinds of SubMessages
        /// </summary>
        public enum Kind {
            PAD,
            ACKNACK,
            HEARTBEAT,
            GAP,
            INFOTIMESTAMP,
            INFOSOURCE,
            INFOREPLYIP4,
            INFODESTINATION,
            INFOREPLY,
            NACKFRAG,
            HEARTBEATFRAG,
            DATA,
            DATAFRAG,
            SECURESUBMSG,
            UNKNOWN
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="header"> SubMessageHeader </param>
        protected internal SubMessage(SubMessageHeader header) {
            this.header = header;
        }

        /// <summary>
        /// Gets the SubMessageHeader.
        /// </summary>
        /// <returns> SubMessageHeader </returns>
        public virtual SubMessageHeader Header => header;

        /// <summary>
        /// Gets the Kind of this SubMessage
        /// </summary>
        /// <returns> Kind </returns>
        public virtual Kind getKind() {
            switch (header.kind) {
                case 0x01:
                    return Kind.PAD;
                case 0x06:
                    return Kind.ACKNACK;
                case 0x07:
                    return Kind.HEARTBEAT;
                case 0x08:
                    return Kind.GAP;
                case 0x09:
                    return Kind.INFOTIMESTAMP;
                case 0x0c:
                    return Kind.INFOSOURCE;
                case 0x0d:
                    return Kind.INFOREPLYIP4;
                case 0x0e:
                    return Kind.INFODESTINATION;
                case 0x0f:
                    return Kind.INFOREPLY;
                case 0x12:
                    return Kind.NACKFRAG;
                case 0x13:
                    return Kind.HEARTBEATFRAG;
                case 0x15:
                    return Kind.DATA;
                case 0x16:
                    return Kind.DATAFRAG;
                case 0x30:
                    return Kind.SECURESUBMSG;
                default:
                    return Kind.UNKNOWN;
            }
        }

        /// <summary>
        /// Writes This SubMessage into RTPSByteBuffer
        /// </summary>
        /// <param name="bb">RTPSByteBuffer</param>
        public abstract void WriteTo(RTPSByteBuffer bb);

        /// <summary>
        /// Writes this SubMessage into given RTPSByteBuffer.
        /// </summary>
        /// <param name="bb"> RTPSByteBuffer </param>
        //public abstract void writeTo(RTPSByteBuffer bb);
        public override string ToString() {
            return this.GetType().Name + ":" + header.ToString();
        }
    }


    /// <summary>
    /// A Header of the SubMessage. see 8.3.3.2 Submessage structure
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class SubMessageHeader {
        /// <summary>
        /// Default value for endianness in sub messages.
        /// </summary>
        private const byte DEFAULT_ENDIANNESS_FLAG = 0x00;

        internal byte kind;
        internal byte flags; // 8 flags
        internal ushort submessageLength; // ushort

        /// <summary>
        /// Constructs this SubMessageHeader with given kind and
        /// DEFAULT_ENDIANESS_FLAG. Length of the SubMessage is set to 0. Length will
        /// be calculated during marshalling of the Message.
        /// </summary>
        /// <param name="kind"> Kind of SubMessage </param>
        public SubMessageHeader(byte kind) : this(kind, DEFAULT_ENDIANNESS_FLAG) {
        }

        /// <summary>
        /// Constructs this SubMessageHeader with given kind and flags. Length of the
        /// SubMessage is set to 0. Length will be calculated during marshalling of
        /// the Message.
        /// </summary>
        /// <param name="kind"> Kind of SubMessage </param>
        /// <param name="flags"> flags of this SubMessageHeader </param>
        public SubMessageHeader(byte kind, byte flags) {
            this.kind = kind;
            this.flags = flags;
            this.submessageLength = 0; // Length will be calculated during
            // Data.writeTo(...);
        }

        /// <summary>
        /// Constructs SubMessageHeader by reading from RTPSByteBuffer.
        /// </summary>
        /// <param name="bb"> </param>
        internal SubMessageHeader(RTPSByteBuffer bb) {
            kind = bb.read_octet();
            flags = bb.read_octet();
            bb.IsLittleEndian = EndiannessFlag();

            submessageLength = (ushort) (bb.read_short() & 0xffff);
        }

        /// <summary>
        /// Writes this SubMessageHeader into RTPSByteBuffer
        /// </summary>
        /// <param name="bb"> RTPSByteBuffer to write to </param>
        public virtual void writeTo(RTPSByteBuffer bb) {
            bb.write_octet(kind);
            bb.write_octet(flags);
            bb.write_short(submessageLength);
        }

        /// <summary>
        /// Get the endianness for SubMessage. If endianness flag is set,
        /// little-endian is used by SubMessage, otherwise big-endian is used.
        /// </summary>
        /// <returns> true, if endianness flag is set </returns>
        public bool EndiannessFlag() => (flags & 0x1) == 0x1;

        /// <summary>
        /// Get the length of the sub message.
        /// </summary>
        /// <returns> length of the sub message </returns>
        public virtual int SubMessageLength {
            get { return submessageLength; }
        }

        /// <summary>
        /// Get the kind of SubMessage
        /// </summary>
        /// <returns> kind </returns>
        public virtual byte SubMessageKind => kind;

        public override string ToString() {
            StringBuilder sb = new StringBuilder("header[");
            sb.Append("0x");
            sb.Append(string.Format("{0:x2}", kind));
            sb.Append(",0x");
            sb.Append(string.Format("{0:x2}", flags));
            sb.Append(',');
            sb.Append(((int) submessageLength) & 0xffff);
            sb.Append(']');

            return sb.ToString();
        }
    }
}