using System;
using System.Text;
using rtps.message.builtin;

namespace rtps.message {
    /// <summary>
    /// This Submessage notifies the RTPS Reader of a change to a data-object
    /// belonging to the RTPS Writer. The possible changes include both changes in
    /// value as well as changes to the lifecycle of the data-object.
    /// 
    /// see 8.3.7.2
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class Data : SubMessage {
        public const int KIND = 0x15;

        private UInt16 extraFlags = 0;
        private EntityId readerId;
        private EntityId writerId;
        private SequenceNumber writerSN;
        private ParameterList inlineQosParams;
        private DataEncapsulation dataEncapsulation;

        /// <summary>
        /// Constructor for creating a Data message.
        /// </summary>
        /// <param name="readerId"> EntityId of the reader </param>
        /// <param name="writerId"> EntityId of the writer </param>
        /// <param name="seqNum"> Sequence number of the Data submessage </param>
        /// <param name="inlineQosParams"> Inline QoS parameters. May be null. </param>
        /// <param name="dEnc"> If null, neither dataFlag or keyFlag is set </param>
        public Data(EntityId readerId, EntityId writerId, long seqNum, ParameterList inlineQosParams,
                    DataEncapsulation dEnc) : base(new SubMessageHeader(KIND)) {
            this.readerId = readerId;
            this.writerId = writerId;
            this.writerSN = new SequenceNumber(seqNum);

            if (inlineQosParams != null && inlineQosParams.Count > 0) {
                header.flags |= 0x2;
                this.inlineQosParams = inlineQosParams;
            }

            if (dEnc != null) {
                if (dEnc.containsData()) {
                    header.flags |= 0x4; // dataFlag
                }
                else {
                    header.flags |= 0x8; // keyFlag
                }
            }

            this.dataEncapsulation = dEnc;
        }

        /// <summary>
        /// Constructor to read Data sub-message from RTPSByteBuffer.
        /// </summary>
        /// <param name="smh"> </param>
        /// <param name="bb"> </param>
        internal Data(SubMessageHeader smh, RtpsByteBuffer bb) : base(smh) {
            if (DataFlag && KeyFlag) {
                // Should we just ignore this message instead
                throw new System.InvalidOperationException(
                    "This version of protocol does not allow Data submessage to contain both serialized data and serialized key (9.4.5.3.1)");
            }

            long start_count = bb.Position; // start of bytes read so far from the
            // beginning

            this.extraFlags = bb.read_short();
            int octetsToInlineQos = bb.read_short() & 0xffff;

            long currentCount = bb.Position; // count bytes to inline qos

            this.readerId = new EntityId(bb);
            this.writerId = new EntityId(bb);
            this.writerSN = new SequenceNumber(bb);

            long bytesRead = bb.Position - currentCount;
            long unknownOctets = octetsToInlineQos - bytesRead;

            for (int i = 0; i < unknownOctets; i++) {
                // TODO: Instead of looping, we should do just
                // newPos = bb.getBuffer.position() + unknownOctets or something
                // like that
                bb.read_octet(); // Skip unknown octets, @see 9.4.5.3.3
                // octetsToInlineQos
            }

            if (InlineQosFlag) {
                this.inlineQosParams = new ParameterList(bb);
            }

            if (DataFlag || KeyFlag) {
                bb.align(4); // Each submessage is aligned on 32-bit boundary, @see
                // 9.4.1 Overall Structure
                long end_count = bb.Position; // end of bytes read so far from the
                // beginning

                byte[] serializedPayload = null;
                if (header.submessageLength != 0) {
                    serializedPayload = new byte[header.submessageLength - (end_count - start_count)];
                }
                else { // SubMessage is the last one. Rest of the bytes are read.
                    // @see 8.3.3.2.3
                    serializedPayload = new byte[bb.Capacity - bb.Position];
                }

                bb.read(serializedPayload);
                dataEncapsulation = DataEncapsulation.createInstance(serializedPayload);
            }
        }

        /// <summary>
        /// Indicates to the Reader the presence of a ParameterList containing QoS
        /// parameters that should be used to interpret the message.
        /// </summary>
        /// <returns> true, if inlineQos flag is set </returns>
        public bool InlineQosFlag => (header.flags & 0x2) != 0;
        

        /// <summary>
        /// Gets the inlineQos parameters if present. Inline QoS parameters are
        /// present, if inlineQosFlag() returns true.
        /// </summary>
        /// <seealso cref= #inlineQosFlag() </seealso>
        /// <returns> InlineQos parameters, or null if not present </returns>
        public virtual ParameterList InlineQos => inlineQosParams;


        /// <summary>
        /// Indicates to the Reader that the dataPayload submessage element contains
        /// the serialized value of the data-object.
        /// </summary>
        /// <returns> true, data flag is set </returns>
        public bool DataFlag => (header.flags & 0x4) != 0;
        

        /// <summary>
        /// Indicates to the Reader that the dataPayload submessage element contains
        /// the serialized value of the key of the data-object.
        /// </summary>
        /// <returns> true, if key flag is set </returns>
        public bool KeyFlag => (header.flags & 0x8) != 0;
        

        /// <summary>
        /// Identifies the RTPS Reader entity that is being informed of the change to
        /// the data-object.
        /// </summary>
        /// <returns> EntityId_t of the reader </returns>
        public virtual EntityId ReaderId => readerId;

        /// <summary>
        /// Identifies the RTPS Writer entity that made the change to the
        /// data-object.
        /// </summary>
        /// <returns> EntityId_t of the writer </returns>
        public virtual EntityId WriterId => writerId;

        /// <summary>
        /// Uniquely identifies the change and the relative order for all changes
        /// made by the RTPS Writer identified by the writerGuid. Each change gets a
        /// consecutive sequence number. Each RTPS Writer maintains is own sequence
        /// number.
        /// </summary>
        /// <returns> sequence number </returns>
        public virtual long WriterSequenceNumber => writerSN.asLong();

        public virtual UInt16 ExtraFlags => extraFlags;

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_short(extraFlags);

            UInt16 octets_to_inline_qos = 4 + 4 + 8; // EntityId.LENGTH + EntityId.LENGTH + SequenceNumber.LENGTH;
            bb.write_short(octets_to_inline_qos);

            readerId.WriteTo(bb);
            writerId.WriteTo(bb);
            writerSN.WriteTo(bb);

            if (InlineQosFlag) {
                inlineQosParams.WriteTo(bb);
            }

            if (DataFlag || KeyFlag) {
                bb.align(4);
                bb.write(dataEncapsulation.SerializedPayload);
            }
        }

        /// <summary>
        /// Gets the DataEncapsulation. </summary>
        /// <returns> DataEncapsulation </returns>
        public virtual DataEncapsulation DataEncapsulation => dataEncapsulation;

        /// <summary>
        /// Get the StatusInfo (PID 0x0071) inline QoS parameter if it is present. If inline Qos
        /// is not present, an empty(default) StatusInfo is returned
        /// </summary>
        /// <returns> StatusInfo </returns>
        public virtual StatusInfo StatusInfo {
            get {
                StatusInfo sInfo = null;
                if (InlineQosFlag) {
                    sInfo = (StatusInfo) inlineQosParams.getParameter(ParameterId.PID_STATUS_INFO);
                }

                if (sInfo == null) {
                    sInfo = new StatusInfo(); // return empty StatusInfo (WRITE)
                }

                return sInfo;
            }
        }

        /// <summary>
        /// Gets the ContentFilterInfo (PID 0x0055) inline qos parameter if present. </summary>
        /// <returns> ContentFilterInfo, or null if one was not present </returns>
        public virtual ContentFilterInfo ContentFilterInfo {
            get {
                ContentFilterInfo cfi = null;
                if (InlineQosFlag) {
                    cfi = (ContentFilterInfo) inlineQosParams.getParameter(ParameterId.PID_CONTENT_FILTER_INFO);
                }

                return cfi;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder(base.ToString());
            sb.Append(", readerId: ").Append(ReaderId);
            sb.Append(", writerId: ").Append(WriterId);
            sb.Append(", writerSN: ").Append(writerSN);

            if (InlineQosFlag) {
                sb.Append(", inline QoS: ").Append(inlineQosParams);
            }

            return sb.ToString();
        }
    }
}