using System.Collections.Generic;

namespace net.sf.jrtps.rtps
{

	using SubscriptionData = net.sf.jrtps.builtin.SubscriptionData;
	using AckNack = net.sf.jrtps.message.AckNack;
	using EntityId = net.sf.jrtps.types.EntityId;
	using Locator = net.sf.jrtps.types.Locator;

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	/// <summary>
	/// ReaderProxy represents a remote reader.
	/// 
	/// @author mcr70
	/// </summary>
	public class ReaderProxy : RemoteProxy
	{
		private static readonly Logger log = LoggerFactory.getLogger(typeof(ReaderProxy));
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
		private readonly bool expectsInlineQoS_Renamed;

		private AckNack latestAckNack;
		private long readersHighestSeqNum = 0;
		private bool active = true;
		private long heartbeatSentTime = 0; // set to 0 after acknack

		private long latestAckNackReceiveTime;

		private readonly int anSuppressionDuration;
		private readonly EntityId entityId;


		internal ReaderProxy(EntityId entityId, SubscriptionData sd, IList<Locator> locators, int anSuppressionDuration) : base(sd, locators)
		{

			this.entityId = entityId;
			this.expectsInlineQoS_Renamed = sd.expectsInlineQos();
			this.anSuppressionDuration = anSuppressionDuration;
		}

		/// <summary>
		/// Gets the ReaderData associated with this ReaderProxy.
		/// </summary>
		/// <returns> ReaderData </returns>
		public virtual SubscriptionData SubscriptionData
		{
			get
			{
				return (SubscriptionData) DiscoveredData;
			}
		}




		/// <summary>
		/// Returns true if remote reader expects QoS to be sent inline with each
		/// Data submessage.
		/// </summary>
		/// <returns> true or false </returns>
		internal virtual bool expectsInlineQoS()
		{
			return expectsInlineQoS_Renamed;
		}

		internal virtual long ReadersHighestSeqNum
		{
			get
			{
				return readersHighestSeqNum;
			}
			set
			{
				this.readersHighestSeqNum = value;
			}
		}


		internal virtual bool Active
		{
			get
			{
				return active;
			}
		}

		internal virtual void heartbeatSent()
		{
			if (heartbeatSentTime != 0)
			{
				this.heartbeatSentTime = DateTimeHelperClass.CurrentUnixTimeMillis();
			}
			else
			{
				active = false;
			}
		}

		internal virtual int LatestAckNackCount
		{
			get
			{
				if (latestAckNack == null)
				{
					return 0;
				}
    
				return latestAckNack.Count;
			}
		}

		/// <summary>
		/// Updates proxys latest AckNack. Latest AckNack gets updated only if its
		/// count is greater than previously received AckNack. This ensures, that
		/// AckNack gets processed only once.
		/// </summary>
		/// <param name="ackNack"> </param>
		/// <returns> true, if AckNack was accepted </returns>
		internal virtual bool ackNackReceived(AckNack ackNack)
		{
			long anReceiveTime = DateTimeHelperClass.CurrentUnixTimeMillis();

			// First AN is always accepted
			if (latestAckNack == null)
			{
				latestAckNack = ackNack;
				latestAckNackReceiveTime = anReceiveTime;

				return true;
			}

			// Accept only if count > than previous, and enough time (suppression duration) has
			// elapsed since previous AN
			if (ackNack.Count > latestAckNack.Count && anReceiveTime > latestAckNackReceiveTime + anSuppressionDuration)
			{
				latestAckNack = ackNack;
				latestAckNackReceiveTime = anReceiveTime;

				return true;
			}

			log.debug("[{}] AckNack was not accepted; count {} < proxys count {}, or suppression duration not elapsed; {} < {}", entityId, ackNack.Count, latestAckNack.Count, anReceiveTime, latestAckNackReceiveTime + anSuppressionDuration);

			return false;
		}
	}

}