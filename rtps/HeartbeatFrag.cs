using System;

namespace rtps
{
	/// <summary>
	/// When fragmenting data and until all fragments are available, the
	/// HeartbeatFrag Submessage is sent from an RTPS Writer to an RTPS Reader to
	/// communicate which fragments the Writer has available. This enables reliable
	/// communication at the fragment level.<br>
	/// 
	/// Once all fragments are available, a regular Heartbeat message is used.
	/// 
	/// see 9.4.5.7 HeartBeatFrag Submessage
	/// 
	/// @author mcr70
	/// 
	/// </summary>
	public class HeartbeatFrag : SubMessage
	{
		public const int KIND = 0x13;

		/// <summary>
		/// Identifies the Reader Entity that is being informed of the availability
		/// of fragments. Can be set to ENTITYID_UNKNOWN to indicate all readers for
		/// the writer that sent the message.
		/// </summary>
		private EntityId readerId;
		
        /// <summary>
		/// Identifies the Writer Entity that sent the Submessage.
		/// </summary>
		private EntityId writerId;
		
        /// <summary>
		/// Identifies the sequence number of the data change for which fragments are
		/// available.
		/// </summary>
		private SequenceNumber writerSN;
		
        /// <summary>
		/// All fragments up to and including this last (highest) fragment are
		/// available on the Writer for the change identified by writerSN.
		/// </summary>
        private UInt32 lastFragmentNum;

        /// <summary>
		/// A counter that is incremented each time a new HeartbeatFrag message is
		/// sent. Provides the means for a Reader to detect duplicate HeartbeatFrag
		/// messages that can result from the presence of redundant communication
		/// paths.
		/// </summary>
		private UInt32 count;

		public HeartbeatFrag(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh)
		{
			readMessage(bb);
		}

		public virtual EntityId ReaderId => readerId;

		public virtual EntityId WriterId => writerId;

		public virtual SequenceNumber WriterSequenceNumber => writerSN;

		public virtual UInt32 LastFragmentNumber => lastFragmentNum;

		public virtual UInt32 Count => count;

		private void readMessage(RTPSByteBuffer bb)
		{
			this.readerId = new EntityId(bb);
			this.writerId = new EntityId(bb);
			this.writerSN = new SequenceNumber(bb);
			this.lastFragmentNum = bb.read_long(); // ulong
			this.count = bb.read_long();
		}

		public override void WriteTo(RTPSByteBuffer bb)
		{
			readerId.WriteTo(bb);
			writerId.WriteTo(bb);
			writerSN.WriteTo(bb);

			bb.write_long(lastFragmentNum);
			bb.write_long(count);
		}
	}

}