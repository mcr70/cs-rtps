using System;
using System.Collections.Generic;
using rtps.message;
using rtps.message.builtin;

namespace rtps
{

    public class RtpsWriter : RtpsEndpoint<ReaderProxy, SubscriptionData>
    {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected readonly IWriterCache cache;

        public RtpsWriter(Guid guid, IWriterCache cache, bool reliable) : base(guid, reliable)
        {
            this.cache = cache;
        }

        public void OnAckNack(AckNack ackNack)
        {
            throw new System.NotImplementedException();
        }

        private void sendData(ReaderProxy proxy, uint readersHighestSeqNum)
        {
            List<Sample> samples = cache.GetSamplesSince(readersHighestSeqNum);
            if (samples.Count == 0)
            {
                Log.DebugFormat("No samples to send to remote reader {0}", proxy.Guid);
                return;
            }

            DateTime? prevDateTime = null;
            Message m = new Message(Guid.Prefix);
            m.Add(new InfoDestination(proxy.Guid.Prefix));
            foreach (Sample sample in samples)
            {
                if (sample.Timestamp.GetDateTime() > prevDateTime)
                {
                    m.Add(new InfoTimestamp(sample.Timestamp));
                    prevDateTime = sample.Timestamp.GetDateTime();
                }

                Data data = createData(proxy.Guid.EntityId, sample);
                m.Add(data);
            }

            if (proxy.isReliable())
            {
                Heartbeat hb = createHeartbeat(proxyEntityId);
                hb.FinalFlag = false; // Reply needed
                m.Add(hb);
            }
        }

        private Data createData(EntityId readerId, Sample sample)
        {
            // TODO: expectsInlineQos;
            Data d = new Data(readerId, Guid.EntityId, sample.SequenceNumber);

            return d;
        }
    }
}