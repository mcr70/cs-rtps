using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;

namespace net.sf.jrtps.rtps
{

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	using ParticipantData = net.sf.jrtps.builtin.ParticipantData;
	using PublicationData = net.sf.jrtps.builtin.PublicationData;
	using AckNack = net.sf.jrtps.message.AckNack;
	using Data = net.sf.jrtps.message.Data;
	using Gap = net.sf.jrtps.message.Gap;
	using Heartbeat = net.sf.jrtps.message.Heartbeat;
	using InfoDestination = net.sf.jrtps.message.InfoDestination;
	using Message = net.sf.jrtps.message.Message;
	using DirectedWrite = net.sf.jrtps.message.parameter.DirectedWrite;
	using ParameterId = net.sf.jrtps.message.parameter.ParameterId;
	using QosReliability = net.sf.jrtps.message.parameter.QosReliability;
	using EntityId = net.sf.jrtps.types.EntityId;
	using Guid = net.sf.jrtps.types.Guid;
	using GuidPrefix = net.sf.jrtps.types.GuidPrefix;
	using Locator = net.sf.jrtps.types.Locator;
	using SequenceNumberSet = net.sf.jrtps.types.SequenceNumberSet;
	using Time = net.sf.jrtps.types.Time;
	using Watchdog = net.sf.jrtps.util.Watchdog;
	using Listener = net.sf.jrtps.util.Watchdog.Listener;
	using Task = net.sf.jrtps.util.Watchdog.Task;

	/// <summary>
	/// RTPSReader implements RTPS Reader endpoint functionality. 
	/// It will not communicate with unknown writers. It is the
	/// responsibility of DDS layer to provide matched readers when necessary.
	/// Likewise, DDS layer should remove matched writer, when it detects that it is
	/// not available anymore.<para>
	/// 
	/// When Samples arrive at RTPSReader, they are passed on to DDS layer through
	/// <i>ReaderCache</i>. ReaderCache is implemented by DDS layer and is responsible for
	/// storing samples.
	/// 
	/// </para>
	/// </summary>
	/// <seealso cref= ReaderCache </seealso>
	/// <seealso cref= RTPSParticipant#createReader(EntityId, String, ReaderCache, QualityOfService)
	/// 
	/// @author mcr70 </seealso>
	public class RTPSReader<T> : Endpoint
	{
		private static readonly Logger logger = LoggerFactory.getLogger(typeof(RTPSReader));

		private readonly IDictionary<Guid, WriterProxy> writerProxies = new ConcurrentDictionary<Guid, WriterProxy>();
		private readonly ReaderCache<T> rCache;
		private readonly int heartbeatResponseDelay;
		private readonly int heartbeatSuppressionDuration;

		private int ackNackCount = 0;

		private IList<WriterLivelinessListener> livelinessListeners = new LinkedList<WriterLivelinessListener>();

		internal RTPSReader(RTPSParticipant participant, EntityId entityId, string topicName, ReaderCache<T> rCache, QualityOfService qos, Configuration configuration) : base(participant, entityId, topicName, qos, configuration)
		{

			this.rCache = rCache;

			this.heartbeatResponseDelay = configuration.HeartbeatResponseDelay;
			this.heartbeatSuppressionDuration = configuration.HeartbeatSuppressionDuration;
		}


		/// <summary>
		/// Get the BuiltinEndpointSet ID of this RTPSReader. endpointSetId
		/// represents a bit in BuiltinEndpointSet_t, found during discovery. Each
		/// bit represents an existence of a predefined builtin entity.
		/// <para>
		/// See 8.5.4.3 Built-in Endpoints required by the Simple Endpoint Discovery
		/// Protocol and table 9.4 BuiltinEndpointSet_t.
		/// 
		/// </para>
		/// </summary>
		/// <returns> 0, if this RTPSReader is not builtin endpoint </returns>
		public virtual int endpointSetId()
		{
			return EntityId.EndpointSetId;
		}

		/// <summary>
		/// Adds a matched writer for this RTPSReader.
		/// </summary>
		/// <param name="writerData"> PublicationData </param>
		/// <returns> WriterProxy </returns>
//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: public WriterProxy addMatchedWriter(final net.sf.jrtps.builtin.PublicationData writerData)
		public virtual WriterProxy addMatchedWriter(PublicationData writerData)
		{

			IList<Locator> locators = getLocators(writerData);
			WriterProxy wp = writerProxies[writerData.BuiltinTopicKey];
			if (wp == null)
			{
				wp = new WriterProxy(this, writerData, locators, heartbeatSuppressionDuration);
				wp.preferMulticast(Configuration.preferMulticast());
				wp.LivelinessTask = createLivelinessTask(wp);

				writerProxies[writerData.BuiltinTopicKey] = wp;
			}
			else
			{
				wp.update(writerData);
			}

			logger.debug("[{}] Added matchedWriter {}, locators {}", EntityId, writerData, wp.Locators);

			//sendAckNack(wp);

			return wp;
		}

//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: private net.sf.jrtps.util.Watchdog.Task createLivelinessTask(final WriterProxy proxy)
		private Watchdog.Task createLivelinessTask(WriterProxy proxy)
		{
			long livelinessDuration = proxy.PublicationData.QualityOfService.Liveliness.LeaseDuration.asMillis();
			Watchdog watchdog = Participant.Watchdog;

			Watchdog.Task livelinessTask = watchdog.addTask(livelinessDuration, new ListenerAnonymousInnerClass(this, proxy));

			return livelinessTask;
		}

		private class ListenerAnonymousInnerClass : Watchdog.Listener
		{
			private readonly RTPSReader<T> outerInstance;

			private WriterProxy proxy;

			public ListenerAnonymousInnerClass(RTPSReader<T> outerInstance, WriterProxy proxy)
			{
				this.outerInstance = outerInstance;
				this.proxy = proxy;
			}

			public override void triggerTimeMissed()
			{
				proxy.isAlive(false);
				outerInstance.notifyLivelinessLost(proxy.PublicationData);
			}
		}

		private void notifyLivelinessLost(PublicationData writerData)
		{
			lock (livelinessListeners)
			{
				foreach (WriterLivelinessListener listener in livelinessListeners)
				{
					listener.livelinessLost(writerData);
				}
			}
		}

		/// <summary>
		/// Called from WriterProxy when liveliness got restored </summary>
		/// <param name="writerData"> </param>
		internal virtual void notifyLivelinessRestored(PublicationData writerData)
		{
			lock (livelinessListeners)
			{
				foreach (WriterLivelinessListener listener in livelinessListeners)
				{
					listener.livelinessRestored(writerData);
				}
			}
		}

		/// <summary>
		/// Removes all the matched writers that have a given GuidPrefix
		/// </summary>
		/// <param name="prefix"> GuidPrefix </param>
		public virtual void removeMatchedWriters(GuidPrefix prefix)
		{
			foreach (WriterProxy wp in writerProxies.Values)
			{
				if (prefix.Equals(wp.Guid.Prefix))
				{
					removeMatchedWriter(wp.PublicationData);
				}
			}
		}

		/// <summary>
		/// Removes a matched writer from this RTPSReader.
		/// </summary>
		/// <param name="writerData"> writer to remove. </param>
		/// <returns> WriterProxy or null writer corresponding to PublicationData was not found </returns>
		public virtual WriterProxy removeMatchedWriter(PublicationData writerData)
		{
			logger.debug("[{}] Removing matchedWriter {}", EntityId, writerData.BuiltinTopicKey);
			WriterProxy proxy = writerProxies.Remove(writerData.BuiltinTopicKey);

			return proxy;
		}

		/// <summary>
		/// Gets all the matched writers of this RTPSReader.
		/// </summary>
		/// <returns> a Collection of matched writers </returns>
		public virtual ICollection<WriterProxy> MatchedWriters
		{
			get
			{
				return writerProxies.Values;
			}
		}

		/// <summary>
		/// Gets the matched writers owned by given remote participant.
		/// </summary>
		/// <param name="prefix"> GuidPrefix of the remote participant </param>
		/// <returns> a Collection of matched writers </returns>
		public virtual ICollection<WriterProxy> getMatchedWriters(GuidPrefix prefix)
		{
			IList<WriterProxy> proxies = new LinkedList<WriterProxy>();

			foreach (KeyValuePair<Guid, WriterProxy> e in writerProxies.SetOfKeyValuePairs())
			{
				if (prefix.Equals(e.Key.Prefix))
				{
					proxies.Add(e.Value);
				}
			}

			return proxies;
		}

		public virtual WriterProxy getMatchedWriter(Guid writerGuid)
		{
			return writerProxies[writerGuid];
		}


		/// <summary>
		/// Checks, if this RTPSReader is already matched with a RTPSWriter
		/// represented by given Guid.
		/// </summary>
		/// <param name="writerGuid"> Guid of the writer </param>
		/// <returns> true if matched </returns>
		public virtual bool isMatchedWith(Guid writerGuid)
		{
			return writerProxies[writerGuid] != null;
		}


		/// <summary>
		/// Adds WriterLivelinessListener </summary>
		/// <param name="listener"> WriterLivelinessListener </param>
		public virtual void addWriterLivelinessListener(WriterLivelinessListener listener)
		{
			lock (livelinessListeners)
			{
				livelinessListeners.Add(listener);
			}
		}


		/// <summary>
		/// Adds WriterLivelinessListener </summary>
		/// <param name="listener"> WriterLivelinessListener </param>
		public virtual void removeWriterLivelinessListener(WriterLivelinessListener listener)
		{
			lock (livelinessListeners)
			{
				livelinessListeners.Remove(listener);
			}
		}


		/// <summary>
		/// Handle incoming HeartBeat message.
		/// </summary>
		/// <param name="senderGuidPrefix"> </param>
		/// <param name="hb"> </param>
		internal virtual void onHeartbeat(GuidPrefix senderGuidPrefix, Heartbeat hb)
		{
			logger.debug("[{}] Got Heartbeat: #{} {}-{}, F:{}, L:{} from {}", EntityId, hb.Count, hb.FirstSequenceNumber, hb.LastSequenceNumber, hb.finalFlag(), hb.livelinessFlag(), senderGuidPrefix);

			WriterProxy wp = getWriterProxy(new Guid(senderGuidPrefix, hb.WriterId));
			if (wp != null)
			{
				wp.assertLiveliness(); // Got HB, writer is alive

				if (wp.heartbeatReceived(hb))
				{
					if (hb.livelinessFlag())
					{
						//wp.assertLiveliness(); Not really needed, every HB asserts liveliness??? 
					}

					if (Reliable)
					{ // Only reliable readers respond to
						// heartbeat
						bool doSend = false;
						if (!hb.finalFlag())
						{ // if the FinalFlag is not set, then
							// the Reader must send an AckNack
							doSend = true;
						}
						else
						{
							if (wp.GreatestDataSeqNum < hb.LastSequenceNumber)
							{
								doSend = true;
							}
							else
							{
								logger.trace("[{}] Will no send AckNack, since my seq-num is {} and Heartbeat seq-num is {}", EntityId, wp.GreatestDataSeqNum, hb.LastSequenceNumber);
							}
						}

						if (doSend)
						{
							sendAckNack(wp);
						}
					}
				}
			}
			else
			{
				logger.warn("[{}] Discarding Heartbeat from unknown writer {}, {}", EntityId, senderGuidPrefix, hb.WriterId);
			}
		}

		/// <summary>
		/// Handles Gap submessage by updating WriterProxy.
		/// </summary>
		/// <param name="sourceGuidPrefix"> </param>
		/// <param name="gap"> </param>
		internal virtual void onGap(GuidPrefix sourcePrefix, Gap gap)
		{
			Guid writerGuid = new Guid(sourcePrefix, gap.WriterId);

			WriterProxy wp = getWriterProxy(writerGuid);
			if (wp != null)
			{
				wp.assertLiveliness();

				logger.debug("[{}] Applying {}", EntityId, gap);
				wp.applyGap(gap);
			}
		}

		/// <summary>
		/// Handle incoming Data message. Data is unmarshalled and added to pending
		/// samples. Once RTPSMessageHandler has finished with the whole RTPSMessage,
		/// it will call stopMessageProcessing of each RTPSReader that has received
		/// some Data messages.
		/// </summary>
		/// <param name="id"> Id of the set of changes </param>
		/// <param name="sourcePrefix"> GuidPrefix of the remote participant sending Data message </param>
		/// <param name="data"> Data SubMessage </param>
		/// <param name="timeStamp"> timestamp of the data </param>
		/// <exception cref="IOException"> </exception>
		/// <seealso cref= #stopMessageProcessing(int) </seealso>
//JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
//ORIGINAL LINE: void onData(int id, net.sf.jrtps.types.GuidPrefix sourcePrefix, net.sf.jrtps.message.Data data, net.sf.jrtps.types.Time timeStamp) throws java.io.IOException
		internal virtual void onData(int id, GuidPrefix sourcePrefix, Data data, Time timeStamp)
		{
			Guid writerGuid = new Guid(sourcePrefix, data.WriterId);

			WriterProxy wp = getWriterProxy(writerGuid);
			if (wp != null)
			{
				wp.assertLiveliness();

				if (wp.acceptData(data.WriterSequenceNumber))
				{
					if (checkDirectedWrite(data))
					{
						// Add Data to cache only if permitted by DirectedWrite, or if DirectedWrite does not exist
						logger.debug("[{}] Got Data: #{}", EntityId, data.WriterSequenceNumber);
						rCache.addChange(id, writerGuid, data, timeStamp);
					}
				}
				else
				{
					logger.debug("[{}] Data was rejected: Data seq-num={}, proxy seq-num={}", EntityId, data.WriterSequenceNumber, wp.GreatestDataSeqNum);
				}
			}
			else
			{
				logger.warn("[{}] Discarding Data from unknown writer {}, {}", EntityId, sourcePrefix, data.WriterId);
			}
		}

		/// <summary>
		/// This methods is called by RTPSMessageReceiver to signal that a message reception has started.
		/// This method is called for the first message received for this RTPSReader.
		/// </summary>
		/// <param name="msgId"> Id of the message </param>
		/// <seealso cref= #stopMessageProcessing(int) </seealso>
		internal virtual void startMessageProcessing(int msgId)
		{
			rCache.changesBegin(msgId);
		}




		/// <summary>
		/// This method is called by RTPSMessageReceiver to signal that a message reception is done.
		/// </summary>
		/// <param name="msgId"> Id of the message </param>
		/// <seealso cref= #startMessageProcessing(int) </seealso>
		/// <seealso cref= #onData(int, GuidPrefix, Data, Time) </seealso>
		internal virtual void stopMessageProcessing(int msgId)
		{
			rCache.changesEnd(msgId);
		}

		/// <summary>
		/// Checks, if Data has a DirectedWrite attribute, and if so, checks that
		/// this attribute contains Guid of this reader. </summary>
		/// <param name="data"> </param>
		/// <returns> true, if data can be added to this Reader </returns>
		private bool checkDirectedWrite(Data data)
		{
			if (data.inlineQosFlag())
			{
				DirectedWrite dw = (DirectedWrite) data.InlineQos.getParameter(ParameterId.PID_DIRECTED_WRITE);

				if (dw != null)
				{
					foreach (Guid guid in dw.Guids)
					{
						if (guid.Equals(Guid))
						{
							return true;
						}
					}

					return false;
				}
			}

			return true;
		}


//JAVA TO C# CONVERTER WARNING: 'final' parameters are not available in .NET:
//ORIGINAL LINE: private void sendAckNack(final WriterProxy wp)
		private void sendAckNack(WriterProxy wp)
		{

			ThreadStart r = () =>
		{
			Message m = new Message(Guid.Prefix);

			// Add INFO_DESTINATION
			m.addSubMessage(new InfoDestination(wp.Guid.Prefix));

			AckNack an = createAckNack(wp); // If all the data is already received, set finalFlag to true,
			an.finalFlag(wp.AllReceived); // otherwise false(response required)
			//an.finalFlag(true);
			m.addSubMessage(an);

			GuidPrefix targetPrefix = wp.Guid.Prefix;

			logger.debug("[{}] Sending AckNack: #{} {}, F:{} to {}", EntityId, an.Count, an.ReaderSNState, an.finalFlag(), targetPrefix);

			sendMessage(m, wp);
		};

			logger.trace("[{}] Wait for heartbeat response delay: {} ms", EntityId, heartbeatResponseDelay);
			Participant.schedule(r, heartbeatResponseDelay);
		}

		private AckNack createAckNack(WriterProxy wp)
		{
			// This is a simple AckNack, that can be optimized if store
			// out-of-order data samples in a separate cache.

			//long seqNumFirst = wp.getGreatestDataSeqNum(); 

			//        SequenceNumberSet snSet;
			//        if (seqNumFirst == 0) { 
			//        	// If we haven't received any samples so far, negatively ack first sample
			//        	snSet = new SequenceNumberSet(seqNumFirst + 1, 1, new int[]{-1});
			//        }
			//        else { 
			//        	// Only positive ack
			//        	snSet = new SequenceNumberSet(seqNumFirst + 1);
			//        }
			SequenceNumberSet snSet = wp.SequenceNumberSet;

			AckNack an = new AckNack(EntityId, wp.EntityId, snSet, ++ackNackCount);

			return an;
		}

		private WriterProxy getWriterProxy(Guid writerGuid)
		{
			WriterProxy wp = writerProxies[writerGuid];
			if (wp == null && EntityId.SPDP_BUILTIN_PARTICIPANT_WRITER.Equals(writerGuid.EntityId))
			{
				// Create dummy proxy for SPDP writer
				logger.debug("[{}] Creating proxy for SPDP writer {}", EntityId, writerGuid);
//JAVA TO C# CONVERTER WARNING: The .NET Type.FullName property will not always yield results identical to the Java Class.getName method:
				PublicationData pd = new PublicationData(ParticipantData.BUILTIN_TOPIC_NAME, typeof(ParticipantData).FullName, writerGuid, QualityOfService.SPDPQualityOfService);

				// Use empty Locator list, it will be handled by defaults later
				wp = new WriterProxy(this, pd, new LinkedList<Locator>(), 0);

				//wp.setLivelinessTask(createLivelinessTask(wp)); // No need to set liveliness task, since liveliness is infinite

				writerProxies[writerGuid] = wp;
			}

			return wp;
		}

		private bool Reliable
		{
			get
			{
				QosReliability reliability = QualityOfService.Reliability;
				return reliability.Kind == QosReliability.Kind.RELIABLE;
			}
		}
	}

}