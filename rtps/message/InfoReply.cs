using System.Collections.Generic;

namespace rtps.message {
    /// <summary>
    /// This message is sent from an RTPS Reader to an RTPS Writer. It contains
    /// explicit information on where to send a reply to the Submessages that follow
    /// it within the same message.
    /// 
    /// see 9.4.5.9 InfoReply Submessage, 8.3.7.8 InfoReply
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class InfoReply : SubMessage {
        public const int KIND = 0x0f;

        private readonly IList<Locator> unicastLocatorList = new List<Locator>();
        private readonly IList<Locator> multicastLocatorList = new List<Locator>();

        public InfoReply(IList<Locator> unicastLocators, IList<Locator> multicastLocators) : base(
            new SubMessageHeader(KIND)) {
            this.unicastLocatorList = unicastLocators;
            this.multicastLocatorList = multicastLocators;

            if (multicastLocatorList != null && multicastLocatorList.Count > 0) {
                header.flags |= 0x2;
            }
        }

        internal InfoReply(SubMessageHeader smh, RtpsByteBuffer bb) : base(smh) {
            long numLocators = bb.read_long(); // ulong
            for (int i = 0; i < numLocators; i++) {
                Locator loc = new Locator(bb);

                unicastLocatorList.Add(loc);
            }

            if (MulticastFlag) {
                numLocators = bb.read_long(); // ulong
                for (int i = 0; i < numLocators; i++) {
                    Locator loc = new Locator(bb);

                    multicastLocatorList.Add(loc);
                }
            }
        }

        /// <summary>
        /// Returns the MulticastFlag. If true, message contains MulticastLocatorList
        /// </summary>
        /// <returns> true, if message contains multicast locator </returns>
        public bool MulticastFlag => (header.flags & 0x2) != 0;
        

        /// <summary>
        /// Indicates an alternative set of unicast addresses that the Writer should
        /// use to reach the Readers when replying to the Submessages that follow.
        /// </summary>
        /// <returns> a List of Locators </returns>
        public virtual IList<Locator> UnicastLocatorList => unicastLocatorList;

        /// <summary>
        /// Indicates an alternative set of multicast addresses that the Writer
        /// should use to reach the Readers when replying to the Submessages that
        /// follow. Only present when the MulticastFlag is set.
        /// </summary>
        /// <returns> a List of Locators </returns>
        public virtual IList<Locator> MulticastLocatorList => multicastLocatorList;

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write_long((uint) unicastLocatorList.Count);
            foreach (Locator loc in unicastLocatorList) {
                loc.WriteTo(bb);
            }

            if (MulticastFlag) {
                bb.write_long((uint) multicastLocatorList.Count);
                foreach (Locator loc in multicastLocatorList) {
                    loc.WriteTo(bb);
                }
            }
        }

        public override string ToString() {
            return base.ToString() + ", " + unicastLocatorList + ", " + multicastLocatorList;
        }
    }
}