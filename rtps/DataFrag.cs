using System.Collections.Generic;

namespace rtps 
{
	/// <summary>
	/// see 8.3.7.3 DataFrag
	/// 
	/// @author mcr70
	/// 
	/// </summary>
	public class DataFrag : SubMessage
	{
		public const int KIND = 0x16;

		private short extraFlags;
		private EntityId readerId;
		private EntityId writerId;
		private SequenceNumber writerSN;
		private int fragmentStartingNum;
		private int fragmentsInSubmessage;
		private int fragmentSize;
		private int sampleSize;

		private IList<Parameter> parameterList = new List<Parameter>();
		private byte[] serializedPayload;

		public DataFrag(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh)
		{

			readMessage(bb);
		}

		public virtual bool inlineQosFlag()
		{
			return (header.flags & 0x2) != 0;
		}

		public virtual bool keyFlag()
		{
			return (header.flags & 0x4) != 0;
		}

		public virtual EntityId ReaderId
		{
			get
			{
				return readerId;
			}
		}

		public virtual EntityId WriterId
		{
			get
			{
				return writerId;
			}
		}

		public virtual SequenceNumber WriterSequenceNumber
		{
			get
			{
				return writerSN;
			}
		}

		public virtual int FragmentStartingNumber
		{
			get
			{
				return fragmentStartingNum;
			}
		}

		public virtual int FragmentsInSubmessage
		{
			get
			{
				return fragmentsInSubmessage;
			}
		}

		public virtual int FragmentSize
		{
			get
			{
				return fragmentSize;
			}
		}

		public virtual int SampleSize
		{
			get
			{ // getDataSize()
				return sampleSize;
			}
		}

		public virtual IList<Parameter> ParameterList
		{
			get
			{
				return parameterList;
			}
		}

		public virtual byte[] SerializedPayload
		{
			get
			{
				return serializedPayload;
			}
		}

		private void readMessage(RTPSByteBuffer bb)
		{
            int start_count = bb.Position; // start of bytes read so far from the
											 // beginning

			this.extraFlags = (short) bb.read_short();
			int octetsToInlineQos = bb.read_short() & 0xffff;

            int currentCount = bb.Position; // count bytes to inline qos

			this.readerId = EntityId.readEntityId(bb);
			this.writerId = EntityId.readEntityId(bb);
			this.writerSN = new SequenceNumber(bb);

			this.fragmentStartingNum = bb.read_long(); // ulong
			this.fragmentsInSubmessage = bb.read_short(); // ushort
			this.fragmentSize = bb.read_short(); // ushort
			this.sampleSize = bb.read_long(); // ulong

            int bytesRead = bb.Position - currentCount;
			int unknownOctets = octetsToInlineQos - bytesRead;

			for (int i = 0; i < unknownOctets; i++)
			{
				bb.read_octet(); // Skip unknown octets, @see 9.4.5.3.3 octetsToInlineQos
			}

			if (inlineQosFlag())
			{
				readParameterList(bb);
			}

            int end_count = bb.Position; // end of bytes read so far from the beginning

			this.serializedPayload = new byte[header.submessageLength - (end_count - start_count)];
			bb.read(serializedPayload);
		}

		/// 
		/// <param name="bb"> </param>
		/// <exception cref="IOException"> </exception>
		/// <seealso cref= 9.4.2.11 ParameterList </seealso>
		private void readParameterList(RTPSByteBuffer bb)
		{
			while (true)
			{
				bb.align(4);
				Parameter param = ParameterFactory.readParameter(bb);
				parameterList.Add(param);
				if (param.ParameterId == ParameterId.PID_SENTINEL)
				{
					break;
				}
			}
		}

		public void writeTo(RTPSByteBuffer bb)
		{
			bb.write_short(extraFlags);

			short octets_to_inline_qos = 4 + 4 + 8 + 4 + 2 + 2 + 4;
			bb.write_short(octets_to_inline_qos);

			readerId.writeTo(bb);
			writerId.writeTo(bb);
			writerSN.writeTo(bb);

			bb.write_long(fragmentStartingNum);
			bb.write_short((short) fragmentsInSubmessage);
			bb.write_short((short) fragmentSize);
			bb.write_long(sampleSize);

			if (inlineQosFlag())
			{
				writeParameterList(bb);
			}

			bb.write(serializedPayload); // TODO: check this
		}

		private void writeParameterList(RTPSByteBuffer buffer)
		{
			foreach (Parameter param in parameterList)
			{
				param.writeTo(buffer);
			}

			// TODO: last Parameter must be PID_SENTINEL
		}
	}

}