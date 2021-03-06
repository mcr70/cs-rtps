namespace rtps.message
{
	/// <summary>
	/// Provides a source timestamp for subsequent Entity Submessages. In order to
	/// implement the DDS_BY_SOURCE_TIMESTAMP_DESTINATIONORDER_QOS policy,
	/// implementations must include an InfoTimestamp Submessage with every update
	/// from a Writer.
	/// 
	/// see 8.3.7.9.6 InfoTimestamp
	/// 
	/// @author mcr70
	/// 
	/// </summary>
	public class InfoTimestamp : SubMessage
	{
		public const int KIND = 0x09;

		/// <summary>
		/// Present only if the InvalidateFlag is not set in the header. Contains the
		/// timestamp that should be used to interpret the subsequent Submessages.
		/// </summary>
		private Time timestamp;

		public InfoTimestamp(SubMessageHeader smh, RtpsByteBuffer bb) : base(smh)
		{
			readMessage(bb);
		}

        public InfoTimestamp(Time t = null) : base(new SubMessageHeader(KIND))
        {
            if (t == null)
            {
                t = new Time();
            }
            this.timestamp = t;
            header.flags = (byte)(header.flags & ~0x2); // Unset the bit
        }

        public InfoTimestamp(long systemCurrentMillis) : this(new Time(systemCurrentMillis))
		{
		}

		/// <summary>
		/// Indicates whether subsequent Submessages should be considered as having a
		/// timestamp or not. Timestamp is present in _this_ submessage only if the
		/// InvalidateFlag is not set in the header.
		/// </summary>
		/// <returns> true, if invalidateFlag is set </returns>
		public bool invalidateFlag => (header.flags & 0x2) != 0;

		private void readMessage(RtpsByteBuffer bb)
		{
			if (!invalidateFlag)
			{
				this.timestamp = new Time(bb);
			}
		}

		public override void WriteTo(RtpsByteBuffer bb)
		{
			if (!invalidateFlag)
			{
				timestamp.WriteTo(bb);
			}
		}

		/// <summary>
		/// Gets the timestamp
		/// </summary>
		/// <returns> Time </returns>
		public virtual Time TimeStamp
		{
			get
			{
				return timestamp;
			}
		}

		public override string ToString()
		{
			return base.ToString() + ", " + timestamp;
		}
	}

}