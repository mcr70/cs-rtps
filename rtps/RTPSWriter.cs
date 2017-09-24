using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace net.sf.jrtps.rtps
{

	using SubscriptionData = net.sf.jrtps.builtin.SubscriptionData;
	using AckNack = net.sf.jrtps.message.AckNack;
	using Data = net.sf.jrtps.message.Data;
	using DataEncapsulation = net.sf.jrtps.message.DataEncapsulation;
	using Gap = net.sf.jrtps.message.Gap;
	using Heartbeat = net.sf.jrtps.message.Heartbeat;
	using InfoDestination = net.sf.jrtps.message.InfoDestination;
	using InfoTimestamp = net.sf.jrtps.message.InfoTimestamp;
	using Message = net.sf.jrtps.message.Message;
	using CoherentSet = net.sf.jrtps.message.parameter.CoherentSet;
	using ContentFilterProperty = net.sf.jrtps.message.parameter.ContentFilterProperty;
	using DataWriterPolicy = net.sf.jrtps.message.parameter.DataWriterPolicy;
	using KeyHash = net.sf.jrtps.message.parameter.KeyHash;
	using Parameter = net.sf.jrtps.message.parameter.Parameter;
	using ParameterList = net.sf.jrtps.message.parameter.ParameterList;
	using QosDurability = net.sf.jrtps.message.parameter.QosDurability;
	using QosPolicy = net.sf.jrtps.message.parameter.QosPolicy;
	using QosReliability = net.sf.jrtps.message.parameter.QosReliability;
	using StatusInfo = net.sf.jrtps.message.parameter.StatusInfo;
	using EntityId = net.sf.jrtps.types.EntityId;
	using Guid = net.sf.jrtps.types.Guid;
	using GuidPrefix = net.sf.jrtps.types.GuidPrefix;
	using Locator = net.sf.jrtps.types.Locator;
	using ContentFilter = net.sf.jrtps.udds.ContentFilter;

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	/// <summary>
	/// RTPSWriter implements RTPS writer endpoint. RTPSWriter will not communicate
	/// with unknown readers. It is expected that DDS implementation explicitly call
	/// addMatchedReader(SubscriptionData) and removeMatchedReader(SubscriptionData).
	/// 
	/// Samples are written through an implementation of WriterCache, which will be
	/// given when creating RTPSWriter with RTPSParticipant. When RTPSWriter needs to
	/// write samples to RTPSReader, it will query WriterCache for the Samples.
	/// </summary>
	/// <seealso cref= WriterCache </seealso>
	/// <seealso cref= RTPSParticipant#createWriter(EntityId, String, WriterCache, QualityOfService)
	/// 
	/// @author mcr70 </seealso>
	public class RTPSWriter<T> : Endpoint
	{
		private static readonly Logger logger = LoggerFactory.getLogger(typeof(RTPSWriter));

		private readonly IDictionary<Guid, ReaderProxy> readerProxies = new ConcurrentDictionary<Guid, ReaderProxy>();
		/// <summary>
		/// ContentFilters, key is hex representation of filters signature
		/// </summary>
		private readonly IDictionary<string, ContentFilter<T>> contentFilters = new ConcurrentDictionary<string, ContentFilter<T>>();

		private readonly WriterCache<T> writer_cache;
		private readonly int nackResponseDelay;
		private readonly int heartbeatPeriod;
		private readonly bool pushMode;

		private int hbCount; // heartbeat counter. incremented each time hb is sent

//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: private java.util.concurrent.ScheduledFuture<?> hbAnnounceTask;
		private ScheduledFuture<object> hbAnnounceTask;



		internal RTPSWriter(RTPSParticipant participant, EntityId entityId, string topicName, WriterCache<T> wCache, QualityOfService qos, Configuration configuration) : base(participant, entityId, topicName, qos, configuration)
		{

			this.writer_cache = wCache;
			this.nackResponseDelay = configuration.NackResponseDelay;
			this.heartbeatPeriod = configuration.HeartbeatPeriod;
			this.pushMode = configuration.PushMode;

			if (Reliable)
			{
				ThreadStart r = () =>
			{
				logger.debug("[{}] Starting periodical notification", EntityId);
				try
				{
					// periodical notification is handled always as pushMode == false
					notifyReaders(false);
				}
				catch (Exception e)
				{
					logger.error("Got exception while doing periodical notification", e);
				}
			};

				hbAnnounceTask = participant.scheduleAtFixedRate(r, heartbeatPeriod);
			}
		}

		/// <summary>
		/// Get the BuiltinEndpointSet ID of this RTPSWriter.
		/// </summary>
		/// <returns> 0, if this RTPSWriter is not builtin endpoint </returns>
		public virtual int endpointSetId()
		{
			return EntityId.EndpointSetId;
		}

		/// <summary>
		/// Notify every matched RTPSReader. For reliable readers, a Heartbeat is
		/// sent. For best effort readers Data is sent. This provides means to create
		/// multiple changes, before announcing the state to readers.
		/// </summary>
		public virtual void notifyReaders()
		{
			notifyReaders(this.pushMode);
		}

		/// <summary>
		/// Notify readers. Heartbeat announce thread calls this method always with 'false' as pushMode. </summary>
		/// <param name="pushMode">  </param>
		private void notifyReaders(bool pushMode)
		{
			if (readerProxies.Count > 0)
			{
				logger.debug("[{}] Notifying {} matched readers of changes in history cache", EntityId, readerProxies.Count);

				foreach (ReaderProxy proxy in readerProxies.Values)
				{
					Guid guid = proxy.SubscriptionData.BuiltinTopicKey;
					notifyReader(guid, pushMode);
				}
			}
		}

		/// <summary>
		/// Notifies a remote reader with given Guid of the changes available in this writer.
		/// </summary>
		/// <param name="guid"> Guid of the reader to notify </param>
		public virtual void notifyReader(Guid guid)
		{
			notifyReader(guid, this.pushMode);
		}

		/// <summary>
		/// Notify a reader. Heartbeat announce thread calls this method always with 'false' as pushMode. </summary>
		/// <param name="pushMode">  </param>
		private void notifyReader(Guid guid, bool pushMode)
		{
			ReaderProxy proxy = readerProxies[guid];

			if (proxy == null)
			{
				logger.warn("Will not notify, no proxy for {}", guid);
				return;
			}

			// Send HB only if proxy is reliable and we are not configured to be in pushMode
			if (proxy.Reliable && !pushMode)
			{
				sendHeartbeat(proxy);
			}
			else
			{
				long readersHighestSeqNum = proxy.ReadersHighestSeqNum;
				sendData(proxy, readersHighestSeqNum);

				if (!proxy.Reliable)
				{
					// For best effort readers, update readers highest seqnum
					proxy.ReadersHighestSeqNum = writer_cache.SeqNumMax;
				}
			}
		}

		/// <summary>
		/// Assert liveliness of this writer. Matched readers are notified via
		/// Heartbeat message of the liveliness of this writer.
		/// </summary>
		public virtual void assertLiveliness()
		{
			foreach (ReaderProxy proxy in readerProxies.Values)
			{
				sendHeartbeat(proxy, true); // Send Heartbeat regardless of readers QosReliability
			}
		}

		/// <summary>
		/// Close this writer.
		/// </summary>
		public virtual void close()
		{
			if (hbAnnounceTask != null)
			{
				hbAnnounceTask.cancel(true);
			}

			readerProxies.Clear();
		}

		/// <summary>
		/// Add a matched reader.
		/// </summary>
		/// <param name="readerData"> SubscriptionData of the reader </param>
		/// <returns> ReaderProxy </returns>
		public virtual ReaderProxy addMatchedReader(SubscriptionData readerData)
		{
			IList<Locator> locators = getLocators(readerData);

			ReaderProxy proxy = readerProxies[readerData.BuiltinTopicKey];
			if (proxy == null)
			{
				proxy = new ReaderProxy(Guid.EntityId, readerData, locators, Configuration.NackSuppressionDuration);
				proxy.preferMulticast(Configuration.preferMulticast());

				readerProxies[readerData.BuiltinTopicKey] = proxy;
			}
			else
			{
				proxy.update(readerData);
			}

			checkContentFilter(readerData.ContentFilter);

			QosDurability readerDurability = readerData.QualityOfService.Durability;

			if (QosDurability.Kind.VOLATILE == readerDurability.Kind)
			{
				// VOLATILE readers are marked having received all the samples so far
				logger.trace("[{}] Setting highest seqNum to {} for VOLATILE reader", EntityId, writer_cache.SeqNumMax);

				proxy.ReadersHighestSeqNum = writer_cache.SeqNumMax;
			}
			else
			{
				notifyReader(proxy.Guid);
			}

			logger.debug("[{}] Added matchedReader {}", EntityId, readerData);
			return proxy;
		}

		private void checkContentFilter(ContentFilterProperty cfp)
		{
			if (cfp != null)
			{
				string filterClassName = cfp.FilterClassName;
				if (ContentFilterProperty.JAVA_FILTER_CLASS.Equals(filterClassName))
				{
					try
					{
						// TODO: Class.forName is not OSGi friendly
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({ "rawtypes", "unchecked" }) net.sf.jrtps.udds.ContentFilter<T> cf = (net.sf.jrtps.udds.ContentFilter) Class.forName(cfp.getFilterExpression()).newInstance();
						ContentFilter<T> cf = (ContentFilter)System.Activator.CreateInstance(Type.GetType(cfp.FilterExpression));
						registerContentFilter(cf);
					}
					catch (Exception e) when (e is InstantiationException || e is IllegalAccessException || e is ClassNotFoundException)
					{
						logger.warn("Failed to");
					}
				}
				else
				{
					ContentFilter<T> cf = contentFilters[cfp.Signature];
					if (cf == null)
					{
						logger.warn("No ContentFilter matching readers ContentFilterProperty: {} ", cfp);
					}
				}
			}
		}

		/// <summary>
		/// Removes all the matched writers that have a given GuidPrefix
		/// </summary>
		/// <param name="prefix"> GuidPrefix </param>
		public virtual void removeMatchedReaders(GuidPrefix prefix)
		{
			foreach (ReaderProxy rp in readerProxies.Values)
			{
				if (prefix.Equals(rp.Guid.Prefix))
				{
					removeMatchedReader(rp.SubscriptionData);
				}
			}
		}

		/// <summary>
		/// Remove a matched reader.
		/// </summary>
		/// <param name="readerData"> SubscriptionData of the reader to be removed </param>
		public virtual void removeMatchedReader(SubscriptionData readerData)
		{
			readerProxies.Remove(readerData.BuiltinTopicKey);
			logger.debug("[{}] Removed matchedReader {}, {}", EntityId, readerData.BuiltinTopicKey);
		}

		/// <summary>
		/// Gets all the matched readers of this RTPSWriter
		/// </summary>
		/// <returns> a Collection of matched readers </returns>
		public virtual ICollection<ReaderProxy> MatchedReaders
		{
			get
			{
				return readerProxies.Values;
			}
		}

		/// <summary>
		/// Gets the matched readers owned by given remote participant.
		/// </summary>
		/// <param name="prefix">
		///            GuidPrefix of the remote participant </param>
		/// <returns> a Collection of matched readers </returns>
		public virtual ICollection<ReaderProxy> getMatchedReaders(GuidPrefix prefix)
		{
			IList<ReaderProxy> proxies = new LinkedList<ReaderProxy>();

			foreach (KeyValuePair<Guid, ReaderProxy> e in readerProxies.SetOfKeyValuePairs())
			{
				if (prefix.Equals(e.Key.Prefix))
				{
					proxies.Add(e.Value);
				}
			}

			return proxies;
		}

		/// <summary>
		/// Registers a ContentFilter </summary>
		/// <param name="cf"> ContentFilter </param>
		/// <exception cref="NullPointerException"> if cf.getContentFilterProperty() returns null </exception>
		public virtual void registerContentFilter(ContentFilter<T> cf)
		{
			ContentFilterProperty cfp = cf.ContentFilterProperty;
			if (cfp == null)
			{
				throw new System.NullReferenceException("ContentFilterProperty cannot be null when registering ContentFilter to writer");
			}

			logger.debug("[{}] Registering ContentFilter with class '{}', expression '{}'", EntityId, cfp.FilterClassName, cfp.FilterExpression);

			string signature = cfp.Signature;
			contentFilters[signature] = cf;
		}


		/// <summary>
		/// Handle incoming AckNack message.
		/// </summary>
		/// <param name="senderPrefix"> </param>
		/// <param name="ackNack"> </param>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: void onAckNack(net.sf.jrtps.types.GuidPrefix senderPrefix, final net.sf.jrtps.message.AckNack ackNack)
		internal virtual void onAckNack(GuidPrefix senderPrefix, AckNack ackNack)
		{
			logger.debug("[{}] Got AckNack: #{} {}, F:{} from {}", EntityId, ackNack.Count, ackNack.ReaderSNState, ackNack.finalFlag(), senderPrefix);

//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final ReaderProxy proxy = readerProxies.get(new net.sf.jrtps.types.Guid(senderPrefix, ackNack.getReaderId()));
			ReaderProxy proxy = readerProxies[new Guid(senderPrefix, ackNack.ReaderId)];
			if (proxy != null)
			{
				if (proxy.ackNackReceived(ackNack))
				{
					ThreadStart r = () =>
				{
					sendData(proxy, ackNack.ReaderSNState.BitmapBase - 1);
				};

					logger.trace("[{}] Wait for nack response delay: {} ms", EntityId, nackResponseDelay);
					Participant.schedule(r, nackResponseDelay);
				}
			}
			else
			{
				logger.warn("[{}] Discarding AckNack from unknown reader {}", EntityId, ackNack.ReaderId);
			}
		}

		/// <summary>
		/// Send data to given participant & reader. readersHighestSeqNum specifies
		/// which is the first data to be sent.
		/// </summary>
		/// <param name="targetPrefix"> </param>
		/// <param name="readerId"> </param>
		/// <param name="readersHighestSeqNum"> </param>
		private void sendData(ReaderProxy proxy, long readersHighestSeqNum)
		{
			Message m = new Message(Guid.Prefix);
			LinkedList<Sample<T>> samples = writer_cache.getSamplesSince(readersHighestSeqNum);

			if (samples.Count == 0)
			{
				logger.debug("[{}] Remote reader already has all the data", EntityId, proxy, readersHighestSeqNum);
				return;
			}

			ContentFilter<T> filter = null;
			ContentFilterProperty cfp = proxy.SubscriptionData.ContentFilter;
			if (cfp != null)
			{
				filter = contentFilters[cfp.Signature]; // might return null
			}

			// Add INFO_DESTINATION
			m.addSubMessage(new InfoDestination(proxy.Guid.Prefix));

			long prevTimeStamp = 0;
			EntityId proxyEntityId = proxy.EntityId;

			foreach (Sample<T> aSample in samples)
			{
				try
				{
					long timeStamp = aSample.Timestamp;
					if (timeStamp > prevTimeStamp)
					{
						InfoTimestamp infoTS = new InfoTimestamp(timeStamp);
						m.addSubMessage(infoTS);
					}
					prevTimeStamp = timeStamp;

					if (filter != null && !filter.acceptSample(aSample))
					{
						// writer side filtering
						long sn = aSample.SequenceNumber;
						Gap gap = new Gap(proxyEntityId, EntityId, sn, sn);
						m.addSubMessage(gap);
					}
					else
					{
						logger.trace("Marshalling {}", aSample.Data);
						Data data = createData(proxyEntityId, proxy.expectsInlineQoS(), aSample);
						m.addSubMessage(data);
					}
				}
				catch (IOException ioe)
				{
					logger.warn("[{}] Failed to add Sample to message", EntityId, ioe);
				}
			}

			// add HB at the end of data, see 8.4.15.4 Piggybacking HeartBeat submessages
			if (proxy.Reliable)
			{
				Heartbeat hb = createHeartbeat(proxyEntityId);
				hb.finalFlag(false); // Reply needed
				m.addSubMessage(hb);
			}

			long firstSeqNum = samples.First.Value.SequenceNumber;
			long lastSeqNum = samples.Last.Value.SequenceNumber;

			logger.debug("[{}] Sending Data: {}-{} to {}", EntityId, firstSeqNum, lastSeqNum, proxy);

			bool overFlowed = sendMessage(m, proxy);
			if (overFlowed)
			{
				logger.trace("Sending of Data overflowed. Sending HeartBeat to notify reader.");
				sendHeartbeat(proxy);
			}
		}

		private void sendHeartbeat(ReaderProxy proxy)
		{
			sendHeartbeat(proxy, false);
		}

		private void sendHeartbeat(ReaderProxy proxy, bool livelinessFlag)
		{
			Message m = new Message(Guid.Prefix);

			// Add INFO_DESTINATION
			m.addSubMessage(new InfoDestination(proxy.Guid.Prefix));

			Heartbeat hb = createHeartbeat(proxy.EntityId);
			hb.livelinessFlag(livelinessFlag);
			m.addSubMessage(hb);

			logger.debug("[{}] Sending Heartbeat: #{} {}-{}, F:{}, L:{} to {}", EntityId, hb.Count, hb.FirstSequenceNumber, hb.LastSequenceNumber, hb.finalFlag(), hb.livelinessFlag(), proxy.Guid);

			sendMessage(m, proxy);

			if (!livelinessFlag)
			{
				proxy.heartbeatSent();
			}
		}

		private Heartbeat createHeartbeat(EntityId entityId)
		{
			if (entityId == null)
			{
				entityId = EntityId.UNKNOWN_ENTITY;
			}

			Heartbeat hb = new Heartbeat(entityId, EntityId, writer_cache.SeqNumMin, writer_cache.SeqNumMax, hbCount++);

			return hb;
		}

//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: private net.sf.jrtps.message.Data createData(net.sf.jrtps.types.EntityId readerId, boolean expectsInlineQos, Sample<T> sample) throws java.io.IOException
		private Data createData(EntityId readerId, bool expectsInlineQos, Sample<T> sample)
		{
			DataEncapsulation dEnc = sample.DataEncapsulation;
			ParameterList inlineQos = new ParameterList();

			if (expectsInlineQos)
			{ // If reader expects inline qos, add them
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: java.util.Set<net.sf.jrtps.message.parameter.QosPolicy<?>> inlinePolicies = getQualityOfService().getInlinePolicies();
				ISet<QosPolicy<object>> inlinePolicies = QualityOfService.InlinePolicies;
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for (net.sf.jrtps.message.parameter.QosPolicy<?> policy : inlinePolicies)
				foreach (QosPolicy<object> policy in inlinePolicies)
				{
					if (policy is DataWriterPolicy)
					{
						inlineQos.add((Parameter) policy); // TODO: safe cast, but ugly
					}
				}
			}

			CoherentSet cs = sample.CoherentSet;

			if (cs != null)
			{ // Add CoherentSet if present
				inlineQos.add(cs);
			}

			if (sample.hasKey())
			{ // Add KeyHash if present
				inlineQos.add(new KeyHash(sample.Key.Bytes));
			}

			if (!ChangeKind.WRITE.Equals(sample.Kind) && sample.Kind != null)
			{
				// Add status info for operations other than WRITE
				inlineQos.add(new StatusInfo(sample.Kind));
			}

			Data data = new Data(readerId, EntityId, sample.SequenceNumber, inlineQos, dEnc);

			return data;
		}

		/// <summary>
		/// Checks, if a given change number has been acknowledged by every known
		/// matched reader.
		/// </summary>
		/// <param name="sequenceNumber"> sequenceNumber of a change to check </param>
		/// <returns> true, if every matched reader has acknowledged given change number </returns>
		public virtual bool isAcknowledgedByAll(long sequenceNumber)
		{
			foreach (ReaderProxy proxy in readerProxies.Values)
			{
				if (proxy.Active && proxy.ReadersHighestSeqNum < sequenceNumber)
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Checks, if this RTPSWriter is already matched with a RTPSReader
		/// represented by given Guid.
		/// </summary>
		/// <param name="readerGuid"> Guid of the reader </param>
		/// <returns> true if matched </returns>
		public virtual bool isMatchedWith(Guid readerGuid)
		{
			return readerProxies[readerGuid] != null;
		}

		internal virtual bool Reliable
		{
			get
			{
				QosReliability policy = QualityOfService.Reliability;
    
				return policy.Kind == QosReliability.Kind.RELIABLE;
			}
		}
	}

}