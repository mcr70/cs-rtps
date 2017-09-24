using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace net.sf.jrtps.rtps
{

	using ParticipantData = net.sf.jrtps.builtin.ParticipantData;
	using PublicationData = net.sf.jrtps.builtin.PublicationData;
	using Receiver = net.sf.jrtps.transport.Receiver;
	using TransportProvider = net.sf.jrtps.transport.TransportProvider;
	using EntityId = net.sf.jrtps.types.EntityId;
	using Guid = net.sf.jrtps.types.Guid;
	using GuidPrefix = net.sf.jrtps.types.GuidPrefix;
	using Locator = net.sf.jrtps.types.Locator;
	using AuthenticationPlugin = net.sf.jrtps.udds.security.AuthenticationPlugin;
	using Watchdog = net.sf.jrtps.util.Watchdog;

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	/// <summary>
	/// RTPSParticipant is the main entry point to RTPS (DDS) domain. Participant is
	/// responsible for creating readers and writers and setting up network
	/// receivers.
	/// 
	/// @author mcr70
	/// 
	/// </summary>
	public class RTPSParticipant
	{
		private static readonly Logger logger = LoggerFactory.getLogger(typeof(RTPSParticipant));

		private readonly Configuration config;
		private readonly ScheduledThreadPoolExecutor threadPoolExecutor;
		private readonly Watchdog watchdog;

		/// <summary>
		/// Maps that stores discovered participants. discovered participant is
		/// shared with all entities created by this participant.
		/// </summary>
		private readonly IDictionary<GuidPrefix, ParticipantData> discoveredParticipants;


//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: private final java.util.List<RTPSReader<?>> readerEndpoints = new java.util.concurrent.CopyOnWriteArrayList<>();
		private readonly IList<RTPSReader<object>> readerEndpoints = new CopyOnWriteArrayList<RTPSReader<object>>();
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: private final java.util.List<RTPSWriter<?>> writerEndpoints = new java.util.concurrent.CopyOnWriteArrayList<>();
		private readonly IList<RTPSWriter<object>> writerEndpoints = new CopyOnWriteArrayList<RTPSWriter<object>>();

		private readonly LinkedList<Locator> discoveryLocators = new LinkedList<Locator>();
		private readonly LinkedList<Locator> userdataLocators = new LinkedList<Locator>();


		private readonly Guid guid;
		private RTPSMessageReceiver handler;

		private int domainId;
		private int participantId;

		private readonly AuthenticationPlugin aPlugin;


		/// <summary>
		/// Creates a new participant with given domainId and participantId. Domain
		/// ID and participant ID is used to construct unicast locators to this
		/// RTPSParticipant. In general, participants in the same domain get to know
		/// each other through SPDP. Each participant has a unique unicast locator
		/// to access its endpoints.
		/// </summary>
		/// <param name="guid"> Guid, that is assigned to this participant. Every entity created by this
		///        RTPSParticipant will share the GuidPrefix of this Guid. </param>
		/// <param name="domainId"> Domain ID of the participant </param>
		/// <param name="participantId"> Participant ID of this participant. If set to -1, and port number is not given
		///        during starting of receivers, participantId will be determined based on the first
		///        suitable network socket. </param>
		/// <param name="tpe"> threadPoolExecutor </param>
		/// <param name="discoveredParticipants"> a Map that holds discovered participants </param>
		/// <param name="aPlugin"> AuthenticationPlugin  </param>
		public RTPSParticipant(Guid guid, int domainId, int participantId, ScheduledThreadPoolExecutor tpe, IDictionary<GuidPrefix, ParticipantData> discoveredParticipants, AuthenticationPlugin aPlugin)
		{
			this.guid = guid;
			this.domainId = domainId;
			this.participantId = participantId;
			this.threadPoolExecutor = tpe;
			this.aPlugin = aPlugin;
			this.watchdog = new Watchdog(threadPoolExecutor);
			this.discoveredParticipants = discoveredParticipants;
			this.config = aPlugin.Configuration;
		}


		/// <summary>
		/// Starts this Participant. All the configured endpoints are initialized.
		/// </summary>
		public virtual void start()
		{
			BlockingQueue<sbyte[]> queue = new LinkedBlockingQueue<sbyte[]>(config.MessageQueueSize);

			// NOTE: We can have only one MessageReceiver. pending samples concept
			// relies on it.
			handler = new RTPSMessageReceiver(aPlugin.CryptoPlugin, this, queue, config);
			threadPoolExecutor.execute(handler);

			logger.debug("Starting receivers for discovery");
			IList<URI> discoveryURIs = config.DiscoveryListenerURIs;
			int receiverCount = startReceiversForURIs(queue, discoveryURIs, true);

			logger.debug("Starting receivers for user data");
			IList<URI> listenerURIs = config.ListenerURIs;
			receiverCount += startReceiversForURIs(queue, listenerURIs, false);

			logger.debug("{} receivers, {} readers and {} writers started", receiverCount, readerEndpoints.Count, writerEndpoints.Count);
		}

		/// <summary>
		/// Creates a new RTPSReader.
		/// </summary>
		/// <param name="eId"> EntityId of the reader </param>
		/// <param name="topicName"> Name of the topic </param>
		/// <param name="rCache"> ReaderCache </param>
		/// <param name="qos"> QualityOfService </param>
		/// @param <T> Type of RTPSReader </param>
		/// <returns> RTPSReader </returns>
		public virtual RTPSReader<T> createReader<T>(EntityId eId, string topicName, ReaderCache<T> rCache, QualityOfService qos)
		{
			RTPSReader<T> reader = new RTPSReader<T>(this, eId, topicName, rCache, qos, config);
			reader.DiscoveredParticipants = discoveredParticipants;

			readerEndpoints.Add(reader);

			return reader;
		}

		/// <summary>
		/// Creates a new RTPSWriter.
		/// </summary>
		/// <param name="eId"> EntityId of the reader </param>
		/// <param name="topicName"> Name of the topic </param>
		/// <param name="wCache"> WriterCache </param>
		/// <param name="qos"> QualityOfService </param>
		/// @param <T> Type of RTPSWriter
		/// </param>
		/// <returns> RTPSWriter </returns>
		public virtual RTPSWriter<T> createWriter<T>(EntityId eId, string topicName, WriterCache<T> wCache, QualityOfService qos)
		{
			RTPSWriter<T> writer = new RTPSWriter<T>(this, eId, topicName, wCache, qos, config);
			writer.DiscoveredParticipants = discoveredParticipants;

			writerEndpoints.Add(writer);

			return writer;
		}

		/// <summary>
		/// Close this RTPSParticipant. All the network listeners will be stopped and
		/// all the history caches of all entities will be cleared.
		/// </summary>
		public virtual void close()
		{
			logger.debug("Closing RTPSParticipant {}", guid);
			handler.close(); // Close RTPSMessageReceiver loop gracefully

//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for (RTPSWriter<?> w : writerEndpoints)
			foreach (RTPSWriter<object> w in writerEndpoints)
			{ // Closes periodical announce thread
				w.close();
			}

			writerEndpoints.Clear();
			readerEndpoints.Clear();

			// Let TransportProviders do cleanup
			ICollection<TransportProvider> transportProviders = TransportProvider.TransportProviders;
			foreach (TransportProvider tp in transportProviders)
			{
				tp.close();
			}
		}

		/// <summary>
		/// Gets the guid of this participant.
		/// </summary>
		/// <returns> guid </returns>
		public virtual Guid Guid
		{
			get
			{
				return guid;
			}
		}

		/// <summary>
		/// Gets the Locators that can be used for discovery </summary>
		/// <returns> a List of Locators </returns>
		public virtual IList<Locator> DiscoveryLocators
		{
			get
			{
				return discoveryLocators;
			}
		}

		/// <summary>
		/// Gets the Locators that can be used for user data </summary>
		/// <returns> a List of Locators </returns>
		public virtual IList<Locator> UserdataLocators
		{
			get
			{
				return userdataLocators;
			}
		}

		/// <summary>
		/// Ignores messages originating from given Participant </summary>
		/// <param name="prefix"> GuidPrefix of the participant to ignore </param>
		public virtual void ignoreParticipant(GuidPrefix prefix)
		{
			handler.ignoreParticipant(prefix);
		}



		/// <summary>
		/// Gets the domainId of this participant;
		/// </summary>
		/// <returns> domainId </returns>
		internal virtual int DomainId
		{
			get
			{
				return domainId;
			}
		}

		/// <summary>
		/// Waits for a given amount of milliseconds.
		/// </summary>
		/// <param name="millis"> </param>
		/// <returns> true, if timeout occured normally </returns>
		internal virtual bool waitFor(int millis)
		{
			if (millis > 0)
			{
				try
				{
					return !threadPoolExecutor.awaitTermination(millis, TimeUnit.MILLISECONDS);
				}
				catch (InterruptedException)
				{
					logger.debug("waitFor(...) was interrupted");
				}
			}

			return false;
		}

		/// <summary>
		/// Schedules given Runnable to be executed at given rate.
		/// </summary>
		/// <param name="r"> </param>
		/// <param name="millis">
		///            Number of milliseconds between executions </param>
		/// <returns> ScheduledFuture </returns>
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: java.util.concurrent.ScheduledFuture<?> scheduleAtFixedRate(Runnable r, long millis)
		internal virtual ScheduledFuture<object> scheduleAtFixedRate(ThreadStart r, long millis)
		{
			return threadPoolExecutor.scheduleAtFixedRate(r, millis, millis, TimeUnit.MILLISECONDS);
		}

//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: java.util.concurrent.ScheduledFuture<?> schedule(Runnable r, long delayInMillis)
		internal virtual ScheduledFuture<object> schedule(ThreadStart r, long delayInMillis)
		{
			return threadPoolExecutor.schedule(r, delayInMillis, TimeUnit.MILLISECONDS);
		}

		/// <summary>
		/// Gets a Reader with given readerId. If readerId is null or
		/// EntityId_t.UNKNOWN_ENTITY, a search is made to match with corresponding
		/// writerId. I.e. If writer is SEDP_BUILTIN_PUBLICATIONS_WRITER, a search is
		/// made for SEDP_BUILTIN_PUBLICATIONS_READER.
		/// </summary>
		/// <param name="readerId"> </param>
		/// <param name="sourceGuidPrefix"> </param>
		/// <param name="writerId"> </param>
		/// <returns> RTPSReader </returns>
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: RTPSReader<?> getReader(net.sf.jrtps.types.EntityId readerId, net.sf.jrtps.types.GuidPrefix sourceGuidPrefix, net.sf.jrtps.types.EntityId writerId)
		internal virtual RTPSReader<object> getReader(EntityId readerId, GuidPrefix sourceGuidPrefix, EntityId writerId)
		{
			//logger.warn("getReader({}, {}, {}", readerId, sourceGuidPrefix, writerId);
			if (readerId != null && !EntityId.UNKNOWN_ENTITY.Equals(readerId))
			{
				return getReader(readerId);
			}

			if (writerId.Equals(EntityId.SEDP_BUILTIN_PUBLICATIONS_WRITER))
			{
				return getReader(EntityId.SEDP_BUILTIN_PUBLICATIONS_READER);
			}

			if (writerId.Equals(EntityId.SEDP_BUILTIN_SUBSCRIPTIONS_WRITER))
			{
				return getReader(EntityId.SEDP_BUILTIN_SUBSCRIPTIONS_READER);
			}

			if (writerId.Equals(EntityId.SEDP_BUILTIN_TOPIC_WRITER))
			{
				return getReader(EntityId.SEDP_BUILTIN_TOPIC_READER);
			}

			if (writerId.Equals(EntityId.SPDP_BUILTIN_PARTICIPANT_WRITER))
			{
				return getReader(EntityId.SPDP_BUILTIN_PARTICIPANT_READER);
			}

			if (writerId.Equals(EntityId.BUILTIN_PARTICIPANT_MESSAGE_WRITER))
			{
				return getReader(EntityId.BUILTIN_PARTICIPANT_MESSAGE_READER);
			}

			Guid writerGuid = new Guid(sourceGuidPrefix, writerId);
			if (EntityId.UNKNOWN_ENTITY.Equals(readerId))
			{
				StringBuilder sb = new StringBuilder();
				logger.debug("writer {} wants to talk to UNKNOWN_ENTITY", writerId);

//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for (RTPSReader<?> r : readerEndpoints)
				foreach (RTPSReader<object> r in readerEndpoints)
				{
					logger.trace("Check if reader {} is matched: {}", r.EntityId, r.isMatchedWith(writerGuid));
					sb.Append(r.EntityId + " ");
					if (r.isMatchedWith(writerGuid))
					{
						logger.debug("Found reader {} that is matched with {}", r.EntityId, writerGuid);
						return r; // TODO: we should return a List<RTPSReader>
					}
				}

				logger.trace("Known reader entities: {}", sb);
			}

			return null;
		}


//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: RTPSWriter<?> getWriter(net.sf.jrtps.types.EntityId writerId, net.sf.jrtps.types.GuidPrefix sourceGuidPrefix, net.sf.jrtps.types.EntityId readerId)
		internal virtual RTPSWriter<object> getWriter(EntityId writerId, GuidPrefix sourceGuidPrefix, EntityId readerId)
		{
			if (writerId != null && !EntityId.UNKNOWN_ENTITY.Equals(writerId))
			{
				return getWriter(writerId);
			}

			if (readerId.Equals(EntityId.SEDP_BUILTIN_PUBLICATIONS_READER))
			{
				return getWriter(EntityId.SEDP_BUILTIN_PUBLICATIONS_WRITER);
			}

			if (readerId.Equals(EntityId.SEDP_BUILTIN_SUBSCRIPTIONS_READER))
			{
				return getWriter(EntityId.SEDP_BUILTIN_SUBSCRIPTIONS_WRITER);
			}

			if (readerId.Equals(EntityId.SEDP_BUILTIN_TOPIC_READER))
			{
				return getWriter(EntityId.SEDP_BUILTIN_TOPIC_WRITER);
			}

			if (readerId.Equals(EntityId.SPDP_BUILTIN_PARTICIPANT_READER))
			{
				return getWriter(EntityId.SPDP_BUILTIN_PARTICIPANT_WRITER);
			}

			if (readerId.Equals(EntityId.BUILTIN_PARTICIPANT_MESSAGE_READER))
			{
				return getWriter(EntityId.BUILTIN_PARTICIPANT_MESSAGE_WRITER);
			}

			Guid readerGuid = new Guid(sourceGuidPrefix, readerId);
			if (EntityId.UNKNOWN_ENTITY.Equals(writerId))
			{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for (RTPSWriter<?> writer : writerEndpoints)
				foreach (RTPSWriter<object> writer in writerEndpoints)
				{
					if (writer.isMatchedWith(readerGuid))
					{
						return writer; // TODO: we should return a List<RTPSWriter>
					}
				}
			}

			logger.warn("None of the writers were matched with reader {}", readerGuid);

			return null;
		}


		/// <summary>
		/// Gets the Watchdog of this RTPSParticipant. </summary>
		/// <returns> Watchdog </returns>
		internal virtual Watchdog Watchdog
		{
			get
			{
				return watchdog;
			}
		}

		/// <summary>
		/// Finds a Reader with given entity id.
		/// </summary>
		/// <param name="readerId"> </param>
		/// <returns> RTPSReader </returns>
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: private RTPSReader<?> getReader(net.sf.jrtps.types.EntityId readerId)
		private RTPSReader<object> getReader(EntityId readerId)
		{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for (RTPSReader<?> reader : readerEndpoints)
			foreach (RTPSReader<object> reader in readerEndpoints)
			{
				if (reader.Guid.EntityId.Equals(readerId))
				{
					return reader;
				}
			}

			return null;
		}

		/// <summary>
		/// Finds a Writer with given entity id.
		/// </summary>
		/// <param name="writerId"> </param>
		/// <returns> RTPSWriter </returns>
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: private RTPSWriter<?> getWriter(net.sf.jrtps.types.EntityId writerId)
		private RTPSWriter<object> getWriter(EntityId writerId)
		{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for (RTPSWriter<?> writer : writerEndpoints)
			foreach (RTPSWriter<object> writer in writerEndpoints)
			{
				if (writer.Guid.EntityId.Equals(writerId))
				{
					return writer;
				}
			}

			return null;
		}

		private int startReceiversForURIs(BlockingQueue<sbyte[]> queue, IList<URI> listenerURIs, bool discovery)
		{
			int count = 0;
			foreach (URI uri in listenerURIs)
			{
				TransportProvider provider = TransportProvider.getProviderForScheme(uri.Scheme);

				if (provider != null)
				{
					try
					{
						logger.debug("Starting receiver for {}", uri);
						Locator locator = provider.createLocator(uri, domainId, participantId, discovery);
						Receiver receiver = provider.getReceiver(locator, queue);

						addLocator(locator, discovery);
						threadPoolExecutor.execute(receiver);
						count++;
					}
					catch (IOException ioe)
					{
						logger.warn("Failed to start receiver for URI {}", uri, ioe);
					}
				}
				else
				{
					logger.warn("Unknown scheme for URI {}", uri);
				}
			}

			return count;
		}

		/// <summary>
		/// Assigns Receivers Locator to proper field </summary>
		/// <param name="loc"> </param>
		/// <param name="discovery"> </param>
		private void addLocator(Locator loc, bool discovery)
		{
			if (discovery)
			{
				discoveryLocators.AddLast(loc);
			}
			else
			{
				userdataLocators.AddLast(loc);
			}
		}

//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: java.util.List<RTPSReader<?>> getReaders()
		internal virtual IList<RTPSReader<object>> Readers
		{
			get
			{
				return readerEndpoints;
			}
		}
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: java.util.List<RTPSWriter<?>> getWriters()
		internal virtual IList<RTPSWriter<object>> Writers
		{
			get
			{
				return writerEndpoints;
			}
		}

		internal virtual AuthenticationPlugin AuthenticationPlugin
		{
			get
			{
				return aPlugin;
			}
		}
	}

}