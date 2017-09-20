using System;

namespace rtps {
    /// <summary>
    /// This Submessage is used to communicate the state of a Reader to a Writer. The
    /// Submessage allows the Reader to inform the Writer about the sequence numbers
    /// it has received and which ones it is still missing. This Submessage can be
    /// used to do both positive and negative acknowledgments.
    /// <para>
    /// 
    /// see 8.3.7.1 AckNack
    /// 
    /// @author mcr70
    /// 
    /// </para>
    /// </summary>
    public class AckNack : SubMessage {
        public const int KIND = 0x06;

        private EntityId readerId;
        private EntityId writerId;
        private SequenceNumberSet readerSNState;
        private UInt32 count;

        public AckNack(EntityId readerId, EntityId writerId, SequenceNumberSet readerSnSet, UInt32 count) : base(
            new SubMessageHeader(KIND)) {
            this.readerId = readerId;
            this.writerId = writerId;
            this.readerSNState = readerSnSet;
            this.count = count;
        }

        internal AckNack(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        /// <summary>
        /// Final flag indicates to the Writer whether a response is mandatory.
        /// </summary>
        /// <value> true, if response is NOT mandatory </value>
        public bool FinalFlag {
            get { return (header.flags & 0x2) != 0; }
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
        /// Identifies the Reader entity that acknowledges receipt of certain
        /// sequence numbers and/or requests to receive certain sequence numbers. </summary>
        /// <returns> reader id </returns>
        public EntityId ReaderId {
            get { return readerId; }
            internal set { readerId = value; }
        }

        /// <summary>
        /// Identifies the Writer entity that is the target of the AckNack message.
        /// This is the Writer Entity that is being asked to re-send some sequence
        /// numbers or is being informed of the reception of certain sequence
        /// numbers. </summary>
        /// <returns> writer id </returns>
        public EntityId WriterId {
            get { return writerId; }
            internal set { writerId = value; }
        }

        /// <summary>
        /// Communicates the state of the reader to the writer. All sequence numbers
        /// up to the one prior to readerSNState.base are confirmed as received by
        /// the reader. The sequence numbers that appear in the set indicate missing
        /// sequence numbers on the reader side. The ones that do not appear in the
        /// set are undetermined (could be received or not).
        /// </summary>
        /// <returns> readerSNState </returns>
        public SequenceNumberSet ReaderSNState {
            get { return readerSNState; }
            internal set { readerSNState = value; }
        }

        /// <summary>
        /// A counter that is incremented each time a new AckNack message is sent.
        /// Provides the means for a Writer to detect duplicate AckNack messages that
        /// can result from the presence of redundant communication paths.
        /// </summary>
        /// <returns> count </returns>
        public UInt32 Count {
            get { return count; }
            internal set { count = value; }
        }

        private void readMessage(RTPSByteBuffer bb) {
            this.readerId = EntityId.readEntityId(bb);
            this.writerId = EntityId.readEntityId(bb);
            this.readerSNState = new SequenceNumberSet(bb);
            this.count = bb.read_long();
        }

        public override void WriteTo(RTPSByteBuffer bb) {
            readerId.WriteTo(bb);
            writerId.WriteTo(bb);
            readerSNState.WriteTo(bb);
            bb.write_long(count);
        }

        public override string ToString() {
            return base.ToString() + " #" + count + ", " + readerId + ", " + writerId + ", " +
                   readerSNState + ", F:" + FinalFlag;
        }
    }
}