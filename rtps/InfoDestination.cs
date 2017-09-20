namespace rtps {
    /// <summary>
    /// This message is sent from an RTPS Writer to an RTPS Reader to modify the
    /// GuidPrefix used to interpret the Reader entityIds appearing in the
    /// Submessages that follow it.
    /// 
    /// see 8.3.7.7 InfoDestination
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class InfoDestination : SubMessage {
        public const int KIND = 0x0e;

        private GuidPrefix guidPrefix;

        /// <summary>
        /// Sets GuidPrefix_t to UNKNOWN.
        /// </summary>
        public InfoDestination() : this(GuidPrefix.GUIDPREFIX_UNKNOWN) {
        }

        /// <summary>
        /// This constructor is used when the intention is to send data into network.
        /// </summary>
        /// <param name="guidPrefix"> GuidPrefix of InfoDestination </param>
        public InfoDestination(GuidPrefix guidPrefix) : base(new SubMessageHeader(KIND)) {
            this.guidPrefix = guidPrefix;
        }

        /// <summary>
        /// This constructor is used when receiving data from network.
        /// </summary>
        /// <param name="smh"> </param>
        /// <param name="bb"> </param>
        internal InfoDestination(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            this.guidPrefix = new GuidPrefix(bb);
        }

        /// <summary>
        /// Provides the GuidPrefix that should be used to reconstruct the GUIDs of
        /// all the RTPS Reader entities whose EntityIds appears in the Submessages
        /// that follow.
        /// </summary>
        /// <returns> GuidPrefix </returns>
        public virtual GuidPrefix GuidPrefix => guidPrefix;

        public override void WriteTo(RTPSByteBuffer bb) {
            guidPrefix.writeTo(bb);
        }

        public override string ToString() {
            return base.ToString() + ", " + guidPrefix;
        }
    }
}