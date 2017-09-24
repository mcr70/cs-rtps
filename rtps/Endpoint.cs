using System.Collections.Generic;

namespace rtps
{
    
	/// <summary>
	/// Base class for RTPSReader and RTPSWriter.
	/// 
	/// @author mcr70
	/// </summary>
	public class Endpoint
	{
		private static readonly Logger logger = LoggerFactory.getLogger(typeof(Endpoint));

		private readonly string topicName;
		private readonly Guid guid;
		private IDictionary<GuidPrefix, ParticipantData> discoveredParticipants;

		private readonly CryptoPlugin cryptoPlugin;
		private readonly Configuration configuration;

		private QualityOfService qos;

		private RTPSParticipant participant;

		private readonly bool isSecure;

		/// <summary>
		/// Constructor </summary>
		/// <param name="participant"> <seealso cref="RTPSParticipant"/> </param>
		/// <param name="entityId"> <seealso cref="EntityId"/> </param>
		/// <param name="topicName"> Name of the topic </param>
		/// <param name="qos"> <seealso cref="QualityOfService"/> </param>
		/// <param name="configuration"> <seealso cref="Configuration"/> </param>
		protected internal Endpoint(RTPSParticipant participant, EntityId entityId, string topicName, QualityOfService qos, Configuration configuration)
		{
			this.participant = participant;
			this.guid = new Guid(participant.Guid.Prefix, entityId);
			this.topicName = topicName;
			this.qos = qos;
			this.configuration = configuration;
			this.cryptoPlugin = participant.AuthenticationPlugin.CryptoPlugin;

			// TODO: security should be enabled on topic basis.
			if (entityId.BuiltinEntity)
			{
				isSecure = false;
			}
			else
			{
				isSecure = !"none".Equals(configuration.RTPSProtection);
			}
		}

		/// <summary>
		/// Gets the name of the topic associated with this Endpoint.
		/// </summary>
		/// <returns> name of the topic </returns>
		public virtual string TopicName
		{
			get
			{
				return topicName;
			}
		}

		/// <summary>
		/// Gets the Guid of this Endpoint.
		/// </summary>
		/// <returns> Guid </returns>
		public virtual Guid Guid
		{
			get
			{
				return guid;
			}
		}

		/// <summary>
		/// Gets the EntityId of this Endpoint. This is method behaves the same as calling
		/// getGuid().getEntityId().
		/// </summary>
		/// <returns> EntityId </returns>
		public virtual EntityId EntityId
		{
			get
			{
				return guid.EntityId;
			}
		}

		internal virtual Configuration Configuration
		{
			get
			{
				return configuration;
			}
		}

		internal virtual IDictionary<GuidPrefix, ParticipantData> DiscoveredParticipants
		{
			set
			{
				this.discoveredParticipants = value;
			}
		}

		/// <summary>
		/// Gets the QualityOfService associated with this entity.
		/// </summary>
		/// <returns> QualityOfService </returns>
		public virtual QualityOfService QualityOfService
		{
			get
			{
				return qos;
			}
		}

		/// <summary>
		/// Sends a message. If an overflow occurs during marshaling of Message,
		/// only submessages before the overflow will get sent.
		/// </summary>
		/// <param name="m"> Message to send </param>
		/// <param name="proxy"> proxy of the remote entity </param>
		/// <returns> true, if an overflow occurred during send. </returns>
		protected internal virtual bool sendMessage(Message m, RemoteProxy proxy)
		{
			if (isSecure)
			{
				try
				{
					m = cryptoPlugin.encodeMessage(proxy.Guid, m);
				}
				catch (SecurityException e1)
				{
					logger.error("Failed to encode message", e1);
					return false;
				}
			}

			bool overFlowed = false;
			IList<Locator> locators = new LinkedList<Locator>();

			if (GuidPrefix.GUIDPREFIX_UNKNOWN.Equals(proxy.Guid.Prefix))
			{
				// GUIDPREFIX_UNKNOWN is used with SPDP; let's send message to every
				// configured locator
				((List<Locator>)locators).AddRange(proxy.Locators);
			}
			else
			{
				locators.Add(proxy.Locator);
			}

			foreach (Locator locator in locators)
			{
				logger.trace("Sending message to {}", locator);

				if (locator != null)
				{
					try
					{
						TransportProvider provider = TransportProvider.getProviderForKind(locator.Kind);
						Transmitter tr = provider.getTransmitter(locator);

						overFlowed = tr.sendMessage(m);
					}
					catch (IOException e)
					{
						logger.warn("[{}] Failed to send message to {}", Guid.EntityId, locator, e);
					}
				}
				else
				{
					logger.warn("[{}] Unable to send message, no suitable locator for proxy {}", Guid.EntityId, proxy);
					// participant.ignoreParticipant(targetPrefix);
				}
			}

			return overFlowed;
		}

		/// <summary>
		/// Get the RTPSParticipant, that created this entity.
		/// </summary>
		/// <returns> RTPSParticipant </returns>
		protected internal virtual RTPSParticipant Participant
		{
			get
			{
				return participant;
			}
		}

		/// <summary>
		/// Checks, if this endpoint is secure or not. If an endpoint is secure,
		/// every message that is being sent, will be encoded by Transformer.
		/// </summary>
		/// <returns> true or false </returns>
		public virtual bool Secure
		{
			get
			{
				return isSecure;
			}
		}

		/// <summary>
		/// Gets locators for given remote Guid
		/// </summary>
		/// <param name="dd"> </param>
		/// <returns> unicast and multicast locator </returns>
		internal virtual IList<Locator> getLocators(DiscoveredData dd)
		{
			IList<Locator> locators = new LinkedList<Locator>();

			// check if proxys discovery data contains locator info
			if (!(dd is ParticipantData))
			{
				IList<Parameter> @params = dd.Parameters;
				foreach (Parameter p in @params)
				{
					if (p is LocatorParameter)
					{
						LocatorParameter locParam = (LocatorParameter) p;
						locators.Add(locParam.Locator);
					}
				}
			}


			Guid remoteGuid = dd.BuiltinTopicKey;

			// Set the default locators from ParticipantData
			ParticipantData pd = discoveredParticipants[remoteGuid.Prefix];
			if (pd != null)
			{
				if (remoteGuid.EntityId.BuiltinEntity)
				{
					((List<Locator>)locators).AddRange(pd.DiscoveryLocators);
				}
				else
				{
					((List<Locator>)locators).AddRange(pd.UserdataLocators);
				}
			}
			else
			{
				logger.warn("ParticipantData was not found for {}, cannot set default locators", remoteGuid);
			}

			return locators;
		}
	}

}