using System.Collections.Generic;

namespace net.sf.jrtps.rtps
{

	using DiscoveredData = net.sf.jrtps.builtin.DiscoveredData;
	using QosReliability = net.sf.jrtps.message.parameter.QosReliability;
	using TransportProvider = net.sf.jrtps.transport.TransportProvider;
	using EntityId = net.sf.jrtps.types.EntityId;
	using Guid = net.sf.jrtps.types.Guid;
	using Locator = net.sf.jrtps.types.Locator;

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	/// <summary>
	/// A base class used to represent a remote entity. 
	/// 
	/// @author mcr70
	/// </summary>
	public class RemoteProxy
	{
		private static readonly Logger logger = LoggerFactory.getLogger(typeof(RemoteProxy));

		private DiscoveredData discoveredData;
		private readonly IList<Locator> locators = new LinkedList<Locator>();
//JAVA TO C# CONVERTER NOTE: Fields cannot have the same name as methods:
		private bool preferMulticast_Renamed = false; // TODO: not used at the moment

		/// <summary>
		/// Constructor for RemoteProxy.
		/// </summary>
		/// <param name="dd"> DiscoveredData </param>
		/// <param name="locators"> a List of Locators </param>
		protected internal RemoteProxy(DiscoveredData dd, IList<Locator> locators)
		{
			this.discoveredData = dd;

			// Add only locators we can handle
			foreach (Locator locator in locators)
			{
				TransportProvider provider = TransportProvider.getProviderForKind(locator.Kind);
				if (provider != null)
				{
					// TODO: Convert generic locator to UDPLocator, MemLocator etc.
					//       and remove all the unnecessary stuff Like InetAddress from
					//       Locator
					this.locators.Add(locator);
				}
			}
		}

		/// <summary>
		/// Updates DiscoveredData of this RemoteProxy </summary>
		/// <param name="dd"> DiscoveredData </param>
		public virtual void update(DiscoveredData dd)
		{
			this.discoveredData = dd;
		}

		/// <summary>
		/// Gets the Locator for the remote entity. Tries to return preferred locator
		/// (unicast or multicast). If the preferred locator is null, return non
		/// preferred locator. By default, unicast is preferred.
		/// </summary>
		/// <returns> Locator </returns>
		public virtual Locator Locator
		{
			get
			{
				if (locators.Count > 0)
				{ // TODO: should we return the first one, or should we do some filtering
					return locators[0]; // Get the first available locator
				}
    
				logger.warn("Could not find a suitable Locator from {}", locators);
    
				return null;
			}
		}

		/// <summary>
		/// Gets all the locators for this RemoteProxy </summary>
		/// <returns> All the locators that can be handled by TransportProviders </returns>
		public virtual IList<Locator> Locators
		{
			get
			{
				return locators;
			}
		}

		/// <summary>
		/// Gets the DiscoveredData associated with this Proxy.
		/// </summary>
		/// <returns> DiscoveredData </returns>
		public virtual DiscoveredData DiscoveredData
		{
			get
			{
				return discoveredData;
			}
		}

		/// <summary>
		/// Return true, if remote entity represented by this RemoteProxy is
		/// configured to be reliable.
		/// </summary>
		/// <returns> true, if this RemoteProxy represents a reliable entity </returns>
		public virtual bool Reliable
		{
			get
			{
				QosReliability policy = DiscoveredData.QualityOfService.Reliability;
    
				return policy.Kind == QosReliability.Kind.RELIABLE;
			}
		}

		/// <summary>
		/// Gets the Guid of remote entity.
		/// </summary>
		/// <returns> Guid </returns>
		public virtual Guid Guid
		{
			get
			{
				return discoveredData.BuiltinTopicKey;
			}
		}

		/// <summary>
		/// Gets the EntityId of remote entity. This is method behaves the same as calling
		/// getGuid().getEntityId().
		/// </summary>
		/// <returns> EntityId </returns>
		public virtual EntityId EntityId
		{
			get
			{
				return Guid.EntityId;
			}
		}


		/// <summary>
		/// Sets whether or not to prefer multicast. Default is not to prefer
		/// multicast.
		/// </summary>
		/// <param name="preferMulticast"> Whether or not proxy prefers multicast </param>
		public virtual void preferMulticast(bool preferMulticast)
		{
			// BUG: this concept can be removed. Writer can determine if reader
			// can receive multicast or not
			this.preferMulticast_Renamed = preferMulticast;
		}

		public override string ToString()
		{
			return Guid.ToString() + ", locators " + locators + ", prefers mc: " + preferMulticast_Renamed;
		}
	}

}