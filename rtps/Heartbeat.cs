using System;

namespace rtps {
    /// <summary>
    /// This message is sent from an RTPS Writer to an RTPS Reader to communicate the
    /// sequence numbers of changes that the Writer has available.
    /// 
    /// see 8.3.7.5
    /// 
    /// @author mcr70
    /// </summary>
    public class Heartbeat : SubMessage {
        public const int KIND = 0x07;

        private EntityId readerId;
        private EntityId writerId;
        private SequenceNumber firstSN;
        private SequenceNumber lastSN;
        private UInt32 count;

        public Heartbeat(EntityId readerId, EntityId writerId, long firstSeqNum, long lastSeqNum, UInt32 count) : base(
            new SubMessageHeader(KIND)) {
            this.readerId = readerId;
            this.writerId = writerId;
            this.count = count;
            firstSN = new SequenceNumber(firstSeqNum);
            lastSN = new SequenceNumber(lastSeqNum);

            header.flags |= 2; // set FinalFlag. No response needed.
        }

        internal Heartbeat(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        /// <summary>
        /// Appears in the Submessage header flags. Indicates whether the Reader is
        /// required to respond to the Heartbeat or if it is just an advisory
        /// heartbeat. If finalFlag is set, Reader is not required to respond with
        /// AckNack.
        /// </summary>
        /// <returns> true if final flag is set and reader is not required to respond </returns>
        public bool FinalFlag {
            get {
                return (header.flags & 0x2) != 0;
            }
            internal set {
                if (value) {
                    header.flags |= 0x2;
                }
                else {
                    header.flags = (byte) (header.flags & ~0x2);
                }                
            }
        }

        /// <summary>
        /// Appears in the Submessage header flags. Indicates that the DDS DataWriter
        /// associated with the RTPS Writer of the message has manually asserted its
        /// LIVELINESS.
        /// </summary>
        /// <returns> true, if liveliness flag is set </returns>
        public virtual bool LivelinessFlag {
            get {
                return (header.flags & 0x4) != 0;
            }
            set {
                if (value) {
                    header.flags |= 0x4;
                }
                else {
                    header.flags = (byte) (header.flags & ~0x4);
                }
                
            }
        }

        /// <summary>
        /// Identifies the Reader Entity that is being informed of the availability
        /// of a set of sequence numbers. Can be set to ENTITYID_UNKNOWN to indicate
        /// all readers for the writer that sent the message.
        /// </summary>
        /// <returns> EntityId of the reader </returns>
        public virtual EntityId ReaderId => readerId;

        /// <summary>
        /// Identifies the Writer Entity to which the range of sequence numbers
        /// applies.
        /// </summary>
        /// <returns> EntityId of the writer </returns>
        public virtual EntityId WriterId => writerId;

        /// <summary>
        /// Identifies the first (lowest) sequence number that is available in the
        /// Writer. </summary>
        /// <returns> First available sequence number </returns>
        public virtual long FirstSequenceNumber => firstSN.asLong();

        /// <summary>
        /// Identifies the last (highest) sequence number that is available in the
        /// Writer.
        /// </summary>
        /// <returns> Last available sequence number </returns>
        public virtual long LastSequenceNumber => lastSN.asLong();

        /// <summary>
        /// A counter that is incremented each time a new Heartbeat message is sent.
        /// Provides the means for a Reader to detect duplicate Heartbeat messages
        /// that can result from the presence of redundant communication paths.
        /// </summary>
        /// <returns> a count </returns>
        public virtual UInt32 Count => count;

        private void readMessage(RTPSByteBuffer bb) {
            this.readerId = new EntityId(bb);
            this.writerId = new EntityId(bb);
            this.firstSN = new SequenceNumber(bb);
            this.lastSN = new SequenceNumber(bb);

            this.count = bb.read_long();
        }

        public override void WriteTo(RTPSByteBuffer bb) {
            readerId.WriteTo(bb);
            writerId.WriteTo(bb);
            firstSN.WriteTo(bb);
            lastSN.WriteTo(bb);

            bb.write_long(count);
        }

        public override string ToString() {
            return base.ToString() + " #" + count + ", " + readerId + ", " + writerId + ", " + firstSN + ", " + lastSN +
                   ", F:" + finalFlag() + ", L:" + livelinessFlag();
        }
    }
}