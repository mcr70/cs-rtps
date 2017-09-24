using System;
using System.Collections.Generic;

namespace net.sf.jrtps.rtps
{

	using AckNack = net.sf.jrtps.message.AckNack;
	using Data = net.sf.jrtps.message.Data;
	using Gap = net.sf.jrtps.message.Gap;
	using Heartbeat = net.sf.jrtps.message.Heartbeat;
	using IllegalMessageException = net.sf.jrtps.message.IllegalMessageException;
	using InfoDestination = net.sf.jrtps.message.InfoDestination;
	using InfoReply = net.sf.jrtps.message.InfoReply;
	using InfoReplyIp4 = net.sf.jrtps.message.InfoReplyIp4;
	using InfoSource = net.sf.jrtps.message.InfoSource;
	using InfoTimestamp = net.sf.jrtps.message.InfoTimestamp;
	using Message = net.sf.jrtps.message.Message;
	using SecureSubMessage = net.sf.jrtps.message.SecureSubMessage;
	using SubMessage = net.sf.jrtps.message.SubMessage;
	using Kind = net.sf.jrtps.message.SubMessage.Kind;
	using RTPSByteBuffer = net.sf.jrtps.transport.RTPSByteBuffer;
	using Guid = net.sf.jrtps.types.Guid;
	using GuidPrefix = net.sf.jrtps.types.GuidPrefix;
	using Locator = net.sf.jrtps.types.Locator;
	using LocatorUDPv4_t = net.sf.jrtps.types.LocatorUDPv4_t;
	using Time = net.sf.jrtps.types.Time;
	using CryptoPlugin = net.sf.jrtps.udds.security.CryptoPlugin;
	using SecurityException = net.sf.jrtps.udds.security.SecurityException;

	using Logger = org.slf4j.Logger;
	using LoggerFactory = org.slf4j.LoggerFactory;

	/// <summary>
	/// RTPSMessageReceiver is a consumer to BlockingQueue<byte[]>. A network
	/// receiver produces byte arrays into this queue. These byte[] are parsed into
	/// RTPS messages by this class.
	/// <para>
	/// 
	/// Successfully parsed messages are split into submessages, which are passed to
	/// corresponding RTPS reader entities.
	/// 
	/// </para>
	/// </summary>
	/// <seealso cref= RTPSReader
	/// @author mcr70 </seealso>
	internal class RTPSMessageReceiver : ThreadStart
	{
		private static readonly Logger logger = LoggerFactory.getLogger(typeof(RTPSMessageReceiver));

		private readonly RTPSParticipant participant;
		private readonly BlockingQueue<sbyte[]> queue;
		private readonly CryptoPlugin cryptoPlugin;

		private ISet<GuidPrefix> ignoredParticipants = new HashSet<GuidPrefix>();
		private bool running = true;

		internal RTPSMessageReceiver(CryptoPlugin cryptoPlugin, RTPSParticipant p, BlockingQueue<sbyte[]> queue, Configuration config)
		{
			this.participant = p;
			this.queue = queue;
			this.cryptoPlugin = cryptoPlugin;
		}

		public override void run()
		{
			while (running)
			{
				sbyte[] bytes = null;
				try
				{
					// NOTE: We can have only one MessageReceiver. pending samples
					// concept relies on it.
					// NOTE2: pending samples concept has changed. Check this.
					bytes = queue.take();
					if (running)
					{
						long l1 = DateTimeHelperClass.CurrentUnixTimeMillis();
						Message msg = new Message(new RTPSByteBuffer(bytes));
						long l2 = DateTimeHelperClass.CurrentUnixTimeMillis();
						logger.debug("Parsed RTPS message {} in {} ms", msg, l2 - l1);

						handleMessage(msg);
						long l3 = DateTimeHelperClass.CurrentUnixTimeMillis();
						logger.trace("handleMessage in {} ms", l3 - l2);
					}
				}
				catch (InterruptedException)
				{
					running = false;
				}
				catch (IllegalMessageException ime)
				{
					logger.warn("Got Illegal message: {}, enable trace to see stacktrace", ime.Message);
					logger.trace("Illegal message", ime);
				}
				catch (Exception e)
				{
					logger.warn("Got unexpected exception during Message handling", e);
				}
			}

			logger.debug("RTPSMessageReceiver exiting");
		}

		/// <summary>
		/// Handles incoming Message. Each sub message is transferred to
		/// corresponding reader.
		/// </summary>
		/// <param name="msg"> </param>
		private void handleMessage(Message msg)
		{
			int msgId = msg.GetHashCode();
			Time timestamp = null;
			GuidPrefix destGuidPrefix = participant.Guid.Prefix;
			bool destinationThisParticipant = true;

			GuidPrefix sourceGuidPrefix = msg.Header.GuidPrefix;
			GuidPrefix myPrefix = participant.Guid.Prefix;

			if (myPrefix.Equals(sourceGuidPrefix))
			{
				logger.debug("Discarding message originating from this participant");
				return;
			}

//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: java.util.Set<RTPSReader<?>> dataReceivers = new java.util.HashSet<>();
			ISet<RTPSReader<object>> dataReceivers = new HashSet<RTPSReader<object>>();
			IList<SubMessage> subMessages = msg.SubMessages;

			foreach (SubMessage subMsg in subMessages)
			{
				long smStartTime = DateTimeHelperClass.CurrentUnixTimeMillis();

				if (subMsg.Kind == SubMessage.Kind.SECURESUBMSG)
				{
					SecureSubMessage ssm = (SecureSubMessage) subMsg;
					if (ssm.singleSubMessageFlag())
					{
						//subMsg = cryptoPlugin.decodeSubMessage(ssm);
						logger.warn("Decoding of submessage not implemented. Discarding it.");
						continue;
					}
					else
					{
						try
						{
							handleMessage(cryptoPlugin.decodeMessage(sourceGuidPrefix, ssm));
						}
						catch (SecurityException e)
						{
							logger.error("Failed to decode message", e);
						}

						continue;
					}
				}

				switch (subMsg.Kind)
				{
				case ACKNACK:
					if (!destinationThisParticipant)
					{
						continue;
					}

					if (ignoredParticipants.Contains(sourceGuidPrefix))
					{
						continue;
					}

					handleAckNack(sourceGuidPrefix, (AckNack) subMsg);
					break;
				case DATA:
					if (!destinationThisParticipant)
					{
						continue;
					}

					if (ignoredParticipants.Contains(sourceGuidPrefix))
					{
						continue;
					}

					try
					{
						Data data = (Data) subMsg;
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: RTPSReader<?> r = participant.getReader(data.getReaderId(), sourceGuidPrefix, data.getWriterId());
						RTPSReader<object> r = participant.getReader(data.ReaderId, sourceGuidPrefix, data.WriterId);

						if (r != null)
						{
							if (dataReceivers.Add(r))
							{
								r.startMessageProcessing(msgId);
							}
							r.onData(msgId, sourceGuidPrefix, data, timestamp);
						}
						else
						{
							logger.warn("No reader({}) was matched with {} to handle Data", data.ReaderId, new Guid(sourceGuidPrefix, data.WriterId));
							logger.debug("Known readers: {}", participant.Readers);
						}
					}
					catch (IOException ioe)
					{
						logger.warn("Failed to handle data", ioe);
					}
					break;
				case HEARTBEAT:
					if (!destinationThisParticipant)
					{
						continue;
					}

					if (ignoredParticipants.Contains(sourceGuidPrefix))
					{
						continue;
					}

					handleHeartbeat(sourceGuidPrefix, (Heartbeat) subMsg);
					break;
				case INFODESTINATION:
					destGuidPrefix = ((InfoDestination) subMsg).GuidPrefix;
					destinationThisParticipant = participant.Guid.Prefix.Equals(destGuidPrefix) || GuidPrefix.GUIDPREFIX_UNKNOWN.Equals(destGuidPrefix);

					break;
				case INFOSOURCE:
					sourceGuidPrefix = ((InfoSource) subMsg).GuidPrefix;
					break;
				case INFOTIMESTAMP:
					timestamp = ((InfoTimestamp) subMsg).TimeStamp;
					break;
				case INFOREPLY: // TODO: HB, AC & DATA needs to use replyLocators,
					// if present
					InfoReply ir = (InfoReply) subMsg;
					IList<Locator> replyLocators = ir.UnicastLocatorList;
					if (ir.multicastFlag())
					{
						((List<Locator>)replyLocators).AddRange(ir.MulticastLocatorList);
					}
					logger.warn("InfoReply not handled");
					break;
				case INFOREPLYIP4: // TODO: HB, AC & DATA needs to use these
					// Locators, if present
					InfoReplyIp4 ir4 = (InfoReplyIp4) subMsg;
					LocatorUDPv4_t unicastLocator = ir4.UnicastLocator;
					if (ir4.multicastFlag())
					{
						LocatorUDPv4_t multicastLocator = ir4.MulticastLocator;
					}
					logger.warn("InfoReplyIp4 not handled");
					break;
				case GAP:
					if (!destinationThisParticipant)
					{
						continue;
					}

					handleGap(sourceGuidPrefix, (Gap) subMsg);
					break;

				default:
					logger.warn("SubMessage not handled: {}", subMsg);
				break;
				}

				long smEndTime = DateTimeHelperClass.CurrentUnixTimeMillis();
				logger.trace("Processed {} in {} ms", subMsg.Kind, smEndTime - smStartTime);
			}

			logger.trace("Releasing samples for {} readers", dataReceivers.Count);
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: for (RTPSReader<?> reader : dataReceivers)
			foreach (RTPSReader<object> reader in dataReceivers)
			{
				reader.stopMessageProcessing(msgId);
			}
		}


		private void handleAckNack(GuidPrefix sourceGuidPrefix, AckNack ackNack)
		{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: RTPSWriter<?> writer = participant.getWriter(ackNack.getWriterId(), sourceGuidPrefix, ackNack.getReaderId());
			RTPSWriter<object> writer = participant.getWriter(ackNack.WriterId, sourceGuidPrefix, ackNack.ReaderId);

			if (writer != null)
			{
				writer.onAckNack(sourceGuidPrefix, ackNack);
			}
			else
			{
				logger.debug("No Writer({}) to handle AckNack from {}", ackNack.WriterId, ackNack.ReaderId);
			}
		}

		private void handleGap(GuidPrefix sourceGuidPrefix, Gap gap)
		{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: RTPSReader<?> reader = participant.getReader(gap.getReaderId(), sourceGuidPrefix, gap.getWriterId());
			RTPSReader<object> reader = participant.getReader(gap.ReaderId, sourceGuidPrefix, gap.WriterId);
			if (reader != null)
			{
				reader.onGap(sourceGuidPrefix, gap);
			}
			else
			{
				logger.debug("No Reader({}) to handle Gap from {}", gap.ReaderId, gap.WriterId);
			}
		}

		private void handleHeartbeat(GuidPrefix sourceGuidPrefix, Heartbeat hb)
		{
//JAVA TO C# CONVERTER WARNING: Java wildcard generics have no direct equivalent in .NET:
//ORIGINAL LINE: RTPSReader<?> reader = participant.getReader(hb.getReaderId(), sourceGuidPrefix, hb.getWriterId());
			RTPSReader<object> reader = participant.getReader(hb.ReaderId, sourceGuidPrefix, hb.WriterId);

			if (reader != null)
			{
				reader.onHeartbeat(sourceGuidPrefix, hb);
			}
			else
			{
				logger.debug("No Reader({}) to handle Heartbeat from {}", hb.ReaderId, hb.WriterId);
			}
		}

		private SubMessage extractSubMessage(SecureSubMessage subMsg)
		{
			logger.warn("Secure subMessage -> SubMessage not handled");
			return null;
		}

		internal virtual void ignoreParticipant(GuidPrefix prefix)
		{
			ignoredParticipants.Add(prefix);
		}

		internal virtual void close()
		{
			// Trying to close RTPSMessageReceiver gracefully, by setting running flag to false
			// and putting a dummy byte[] into receiver queue to wake up waiting thread
			running = false;
			queue.offer(new sbyte[0]); // Put a dummy array
		}
	}

}