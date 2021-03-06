using System;

namespace rtps.message {
    /// <summary>
    /// The NackFrag Submessage is used to communicate the state of a Reader to a
    /// Writer. When a data change is sent as a series of fragments, the NackFrag
    /// Submessage allows the Reader to inform the Writer about specific fragment
    /// numbers it is still missing.
    /// 
    /// see 8.3.7.10 NackFrag, 9.4.5.13 NackFrag Submessage
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class NackFrag : SubMessage {
        public const int KIND = 0x12;

        private EntityId readerId;
        private EntityId writerId;
        private SequenceNumber writerSN;
        private SequenceNumberSet fragmentNumberState;
        private UInt32 count;

        public NackFrag(SubMessageHeader smh, RtpsByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        /// <summary>
        /// Identifies the Reader entity that requests to receive certain fragments. </summary>
        /// <returns> EntityId of the reader </returns>
        public virtual EntityId ReaderId => readerId;

        /// <summary>
        /// Identifies the Writer entity that is the target of the NackFrag message.
        /// This is the Writer Entity that is being asked to re-send some fragments.
        /// </summary>
        /// <returns> EntityId of the writer </returns>
        public virtual EntityId WriterId => writerId;

        /// <summary>
        /// The sequence number for which some fragments are missing.
        /// </summary>
        /// <returns> sequence number </returns>
        public virtual SequenceNumber WriterSequenceNumber => writerSN;

        /// <summary>
        /// Communicates the state of the reader to the writer. The fragment numbers
        /// that appear in the set indicate missing fragments on the reader side. The
        /// ones that do not appear in the set are undetermined (could have been
        /// received or not).
        /// </summary>
        /// <returns> SequenceNumberSet indicating missing fragments </returns>
        public virtual SequenceNumberSet FragmentNumberState => fragmentNumberState;

        /// <summary>
        /// A counter that is incremented each time a new NackFrag message is sent.
        /// Provides the means for a Writer to detect duplicate NackFrag messages
        /// that can result from the presence of redundant communication paths.
        /// </summary>
        /// <returns> a count </returns>
        public virtual UInt32 Count => count;

        private void readMessage(RtpsByteBuffer bb) {
            this.readerId = new EntityId(bb);
            this.writerId = new EntityId(bb);
            this.writerSN = new SequenceNumber(bb);
            this.fragmentNumberState = new SequenceNumberSet(bb);

            this.count = bb.read_long();
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            readerId.WriteTo(bb);
            writerId.WriteTo(bb);
            writerSN.WriteTo(bb);
            fragmentNumberState.WriteTo(bb);

            bb.write_long(count);
        }
    }
}