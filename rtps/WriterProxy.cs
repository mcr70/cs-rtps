using System.Collections.Generic;

namespace net.sf.jrtps.rtps
{

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	using PublicationData = net.sf.jrtps.builtin.PublicationData;
	using Gap = net.sf.jrtps.message.Gap;
	using Heartbeat = net.sf.jrtps.message.Heartbeat;
	using QosReliability = net.sf.jrtps.message.parameter.QosReliability;
	using EntityId = net.sf.jrtps.types.EntityId;
	using Locator = net.sf.jrtps.types.Locator;
	using SequenceNumberSet = net.sf.jrtps.types.SequenceNumberSet;
	using Task = net.sf.jrtps.util.Watchdog.Task;

	/// <summary>
	/// WriterProxy represents a remote writer.
	/// 
	/// @author mcr70
	/// 
	/// </summary>
	public class WriterProxy : RemoteProxy
	{
		private static readonly Logger log = LoggerFactory.getLogger(typeof(WriterProxy));

//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: private final RTPSReader<?> reader;
		private readonly RTPSReader<object> reader;
		private readonly int hbSuppressionDuration;
		private readonly EntityId entityId;
		private readonly bool isReliable;

		private Heartbeat latestHeartbeat;
		private long latestHBReceiveTime;

		private volatile long seqNumMax = 0;
		private Task livelinessTask;

//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
		private bool isAlive_Renamed = true; // reflects status of liveliness

		private int strength;


		internal WriterProxy<T1>(RTPSReader<T1> reader, PublicationData wd, IList<Locator> locators, int heartbeatSuppressionDuration) : base(wd, locators)
		{
			this.reader = reader;
			this.entityId = reader.Guid.EntityId;
			this.strength = wd.QualityOfService.OwnershipStrength.Strength;
			this.hbSuppressionDuration = heartbeatSuppressionDuration;

			this.isReliable = reader.QualityOfService.Reliability.Kind.Equals(QosReliability.Kind.RELIABLE);
		}


		internal virtual Task LivelinessTask
		{
			set
			{
				this.livelinessTask = value;
			}
		}

		/// <summary>
		/// Gets the max Data seqnum that has been received.
		/// </summary>
		/// <returns> max Data seqnum that has been received. </returns>
		internal virtual long GreatestDataSeqNum
		{
			get
			{
				return seqNumMax;
			}
		}

		/// <summary>
		/// Checks, if all the samples from remote writer is already received or not. </summary>
		/// <returns> true, if every sample is received </returns>
		internal virtual bool AllReceived
		{
			get
			{
				if (latestHeartbeat == null)
				{
					return false;
				}
    
				return latestHeartbeat.LastSequenceNumber == GreatestDataSeqNum;
			}
		}

		/// <summary>
		/// Gets the PublicationData associated with this WriterProxy.
		/// </summary>
		/// <returns> PublicationData </returns>
		public virtual PublicationData PublicationData
		{
			get
			{
				return (PublicationData) DiscoveredData;
			}
		}

		/// <summary>
		/// Determines if incoming Data should be accepted or not.
		/// </summary>
		/// <param name="sequenceNumber"> </param>
		/// <returns> true, if data was added to cache </returns>
		internal virtual bool acceptData(long sequenceNumber)
		{
			// TODO:
			// Data for reliable readers must come in order. If not, drop it. 
			// Manage out-of-order data with HeartBeat & AckNack & Gap messages

			if (sequenceNumber > seqNumMax)
			{
				if (isReliable && sequenceNumber > seqNumMax + 1 && seqNumMax != 0)
				{
					log.warn("[{}] Accepting data even though some data has been missed: offered seq-num {}, my received seq-num {}", entityId, sequenceNumber, seqNumMax);
				}

				seqNumMax = sequenceNumber;

				return true;
			}

			return false;
		}


		/// <summary>
		/// Asserts liveliness of a writer represented by this WriterProxy. Asserting
		/// a liveliness marks remote writer as being 'alive'. This method should not
		/// be called by user applications.
		/// </summary>
		public virtual void assertLiveliness()
		{
			if (!isAlive_Renamed)
			{
				reader.notifyLivelinessRestored(PublicationData);
			}

			isAlive_Renamed = true;
			if (livelinessTask != null)
			{
				livelinessTask.reset();
			}
		}

		/// <summary>
		/// Updates proxys latest Heartbeat. Latest Heartbeat gets updated only if
		/// its count is greater than previously received Heartbeat. This ensures,
		/// that Heartbeat gets processed only once.
		/// </summary>
		/// <param name="hb"> </param>
		/// <returns> true, if Heartbeat was accepted </returns>
		internal virtual bool heartbeatReceived(Heartbeat hb)
		{
			long hbReceiveTime = DateTimeHelperClass.CurrentUnixTimeMillis();

			// First HB is always accepted
			if (latestHeartbeat == null)
			{
				latestHeartbeat = hb;
				latestHBReceiveTime = hbReceiveTime;
				return true;
			}

			// Accept only if count > than previous, and enough time (suppression duration) has
			// elapsed since previous HB
			if (hb.Count > latestHeartbeat.Count && hbReceiveTime > latestHBReceiveTime + hbSuppressionDuration)
			{
				latestHeartbeat = hb;
				latestHBReceiveTime = hbReceiveTime;
				return true;
			}

			log.debug("[{}] Heartbeat was not accepted; count {} < proxys count {}, or suppression duration not elapsed; {} < {}", entityId, hb.Count, latestHeartbeat.Count, hbReceiveTime, latestHBReceiveTime + hbSuppressionDuration);

			return false;
		}

		internal virtual void applyGap(Gap gap)
		{
			// If the gap start is smaller than or equal to current seqNum + 1 (I.e. next seqNum)...
			if (gap.GapStart <= seqNumMax + 1)
			{
				long gapEnd = gap.GapEnd;
				// ...and gap end is greater than current seqNum...
				if (gapEnd > seqNumMax)
				{
					seqNumMax = gapEnd; // ...then mark current seqNum to be gap end.
				}
			}
		}

		internal virtual SequenceNumberSet SequenceNumberSet
		{
			get
			{
				long @base = GreatestDataSeqNum + 1;
				long firstSN = latestHeartbeat.FirstSequenceNumber;
				long lastSN = latestHeartbeat.LastSequenceNumber;
				int numBits;
    
				if (@base < firstSN)
				{
					@base = firstSN;
					numBits = (int)(lastSN - firstSN + 1);
				}
				else
				{
					numBits = (int)(lastSN - @base + 1);
				}
    
				if (numBits > 256)
				{
					numBits = 256;
				}
    
				return new SequenceNumberSet(@base, numBits);
			}
		}


		/// <summary>
		/// Marks writer represented by this proxy as being alive or not. </summary>
		/// <param name="b"> true if this writer is considered alive </param>
		public virtual void isAlive(bool b)
		{
			this.isAlive_Renamed = b;
		}

		public virtual bool Alive
		{
			get
			{
				return isAlive_Renamed;
			}
		}

		public virtual int Strength
		{
			get
			{
				return strength;
			}
		}

		public override string ToString()
		{
			return Guid.ToString();
		}
	}

}