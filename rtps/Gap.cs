using System.Collections.Generic;

namespace rtps {
    /// <summary>
    /// This Submessage is sent from an RTPS Writer to an RTPS Reader and indicates
    /// to the RTPS Reader that a range of sequence numbers is no longer relevant.
    /// The set may be a contiguous range of sequence numbers or a specific set of
    /// sequence numbers.
    /// <para>
    /// 
    /// see 8.3.7.4 Gap, 9.4.5.5 Gap Submessage
    /// 
    /// @author mcr70
    /// </para>
    /// </summary>
    public class Gap : SubMessage {
        public const int KIND = 0x08;

        private EntityId readerId;
        private EntityId writerId;
        private SequenceNumber gapStart;
        private SequenceNumberSet gapList;

        internal Gap(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            this.readerId = EntityId.readEntityId(bb);
            this.writerId = EntityId.readEntityId(bb);
            this.gapStart = new SequenceNumber(bb);
            this.gapList = new SequenceNumberSet(bb);
        }

        public Gap(EntityId readerId, EntityId writerId, long gapStart, long gapEnd) :
            base(new SubMessageHeader(KIND)) {
            this.readerId = readerId;
            this.writerId = writerId;
            this.gapStart = new SequenceNumber(gapStart);
            this.gapList = new SequenceNumberSet(gapEnd + 1, new int[] {0x0});
        }

        /// <summary>
        /// Get the Reader Entity that is being informed of the irrelevance of a set
        /// of sequence numbers. </summary>
        /// <returns> EntityId of the reader </returns>
        public virtual EntityId ReaderId => readerId;

        /// <summary>
        /// Get the Writer Entity to which the range of sequence numbers applies. </summary>
        /// <returns> EntityId of the writer </returns>
        public virtual EntityId WriterId => writerId;

        /// <summary>
        /// Identifies the first sequence number in the interval of irrelevant
        /// sequence numbers.
        /// </summary>
        /// <returns> First irrelevant sequence number </returns>
        public virtual long GapStart => gapStart.asLong();

        /// <summary>
        /// Gets the last sequence number in this Gap. </summary>
        /// <returns> last sequence number </returns>
        public virtual long GapEnd {
            get {
                long bmBase = gapList.BitmapBase - 1;

                List<long> sequenceNumbers = gapList.SequenceNumbers;
                foreach (long sn in sequenceNumbers) {
                    // Check, that sequence numbers don't have gaps between.
                    // If there is a gap in seqnums, break from the loop.
                    if (sn == bmBase + 1) {
                        bmBase = sn;
                    }
                    else {
                        break;
                    }
                }

                return bmBase;
            }
        }

        /// <summary>
        /// SequenceNumberSet.bitmapBase - 1 is the last sequence number of irrelevant
        /// seq nums. SequenceNumberSet.bitmaps identifies additional irrelevant
        /// sequence numbers.
        /// </summary>
        /// <returns> SequenceNumberSet </returns>
        public virtual SequenceNumberSet GapList => gapList;


        public  override void WriteTo(RTPSByteBuffer bb) {
            readerId.WriteTo(bb);
            writerId.WriteTo(bb);
            gapStart.WriteTo(bb);
            gapList.WriteTo(bb);
        }

        public override string ToString() {
            return base.ToString() + " " + gapStart + ", " + gapList;
        }
    }
}