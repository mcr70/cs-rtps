namespace rtps {
    /// <summary>
    /// This message modifies the logical source of the Submessages that follow.
    /// 
    /// see 9.4.5.10 InfoSource Submessage, 8.3.7.9 InfoSource
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class InfoSource : SubMessage {
        public const int KIND = 0x0c;

        private ProtocolVersion protocolVersion;
        private VendorId vendorId;
        private GuidPrefix guidPrefix;

        public InfoSource(GuidPrefix guidPrefix) : base(new SubMessageHeader(KIND)) {
            this.protocolVersion = ProtocolVersion.PROTOCOLVERSION_2_1;
            this.vendorId = VendorId.JRTPS;
            this.guidPrefix = guidPrefix;
        }

        internal InfoSource(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        /// <summary>
        /// Indicates the protocol used to encapsulate subsequent Submessages.
        /// </summary>
        /// <returns> ProtocolVersion </returns>
        public virtual ProtocolVersion ProtocolVersion => protocolVersion;

        /// <summary>
        /// Indicates the VendorId of the vendor that encapsulated subsequent
        /// Submessages.
        /// </summary>
        /// <returns> VendorId </returns>
        public virtual VendorId VendorId => vendorId;

        /// <summary>
        /// Identifies the Participant that is the container of the RTPS Writer
        /// entities that are the source of the Submessages that follow.
        /// </summary>
        /// <returns> GuidPrefix </returns>
        public virtual GuidPrefix GuidPrefix => guidPrefix;

        private void readMessage(RTPSByteBuffer bb) {
            bb.read_long(); // unused

            protocolVersion = new ProtocolVersion(bb);
            vendorId = new VendorId(bb);
            guidPrefix = new GuidPrefix(bb);
        }

        public override void WriteTo(RTPSByteBuffer bb) {
            bb.write_long(0);
            protocolVersion.WriteTo(bb);
            vendorId.WriteTo(bb);
            guidPrefix.WriteTo(bb);
        }

        public override string ToString() {
            return base.ToString() + ", " + guidPrefix;
        }
    }
}