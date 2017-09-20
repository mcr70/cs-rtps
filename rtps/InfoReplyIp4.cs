namespace rtps {
    /// <summary>
    /// The InfoReplyIp4 Submessage is an additional Submessage introduced by the UDP
    /// PSM. Its use and interpretation are identical to those of an InfoReply
    /// Submessage containing a single unicast and possibly a single multicast
    /// locator, both of kind LOCATOR_KIND_UDPv4. It is provided for efficiency
    /// reasons and can be used instead of the InfoReply Submessage to provide a more
    /// compact representation.
    /// 
    /// </summary>
    public class InfoReplyIp4 : SubMessage {
        public const int KIND = 0x0d;

        private LocatorUDPv4_t unicastLocator;
        private LocatorUDPv4_t multicastLocator;

        public InfoReplyIp4(LocatorUDPv4_t unicastLocator, LocatorUDPv4_t multicastLocator) : base(
            new SubMessageHeader(KIND)) {
            this.unicastLocator = unicastLocator;
            this.multicastLocator = multicastLocator;

            if (multicastLocator != null) {
                header.flags |= 2;
            }
        }

        internal InfoReplyIp4(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        /// <summary>
        /// Returns the MulticastFlag. If true, message contains MulticastLocator
        /// </summary>
        /// <returns> true if message contains MulticastLocator </returns>
        public virtual bool multicastFlag() {
            return (header.flags & 0x2) != 0;
        }

        /// <summary>
        /// Gets the unicast locator, if present
        /// </summary>
        /// <returns> <seealso cref="LocatorUDPv4_t"/> or null if no unicast locator is present </returns>
        public virtual LocatorUDPv4_t UnicastLocator => unicastLocator;

        /// <summary>
        /// Gets the multicast locator, if present.
        /// </summary>
        /// <returns> <seealso cref="LocatorUDPv4_t"/> or null if no multicast locator is present </returns>
        public virtual LocatorUDPv4_t MulticastLocator => multicastLocator;

        private void readMessage(RTPSByteBuffer bb) {
            unicastLocator = new LocatorUDPv4_t(bb);

            if (multicastFlag()) {
                multicastLocator = new LocatorUDPv4_t(bb);
            }
        }

        public override void WriteTo(RTPSByteBuffer bb) {
            unicastLocator.writeTo(bb);
            multicastLocator.writeTo(bb);
        }
    }
}