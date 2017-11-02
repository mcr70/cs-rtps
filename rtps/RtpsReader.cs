using System.Collections.Generic;
using System.Threading.Tasks;
using rtps.message;
using rtps.message.builtin;

namespace rtps {
    public class RtpsReader : RtpsEndpoint<WriterProxy, PublicationData> {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private uint _ackNackCount = 0;
        private HistoryCache rCache = new HistoryCache();
        
        public RtpsReader(Guid guid, bool reliable = false) : base(guid, reliable) {
        }

        
        public void OnGap(GuidPrefix senderPrefix, Gap gap) {
            Guid remoteGuid = new Guid(senderPrefix, gap.WriterId);
            WriterProxy wp;
            if (RemoteProxies.TryGetValue(remoteGuid, out wp)) {
                wp.ApplyGap(gap);
            }
        }

        public void OnHeartbeat(GuidPrefix senderPrefix, Heartbeat hb) {
            Guid remoteGuid = new Guid(senderPrefix, hb.WriterId);
            WriterProxy wp;
            if (RemoteProxies.TryGetValue(remoteGuid, out wp)) {
                if (wp.ApplyHeartbeat(hb) && Reliable) {
                    // Reply with Acknack, if FinalFlag is not set, or
                    // we have not received every data
                    if (!hb.FinalFlag || wp.SeqNumMax < hb.LastSequenceNumber) {
                        Task.Run(() => sendAckNack(wp));
                    }
                }
            }
        }

        private void sendAckNack(WriterProxy wp) {
            Message msg = new Message(Guid.Prefix);
            AckNack a = new AckNack(Guid.EntityId, wp.EntityId, wp.GetSequenceNumberSet(), ++_ackNackCount);
            a.FinalFlag = wp.IsAllReceived();
            
            msg.Add(a);
            SendMessage(msg, wp);
        }


        public void OnData(GuidPrefix senderPrefix, Data data, Time timestamp) {
            Guid remoteGuid = new Guid(senderPrefix, data.WriterId);
            
            WriterProxy wp = getWriterProxy(remoteGuid);
            if (RemoteProxies.TryGetValue(remoteGuid, out wp)) {
                if (wp.ApplyData(data, Reliable)) {
                    Log.Debug("Adding change to history cache " + data.WriterSequenceNumber);
                    rCache.AddChange(0, remoteGuid, data, timestamp);                    
                }
            }
            else {
                Log.DebugFormat("Discarding Data from unknown writer: {0}", remoteGuid);                
            }
        }

        private WriterProxy getWriterProxy(Guid remoteGuid) {
            WriterProxy wp;
            if (!RemoteProxies.TryGetValue(remoteGuid, out wp) &&
                EntityId.SPDP_BUILTIN_PARTICIPANT_WRITER.Equals(remoteGuid.EntityId)) {
                
                Log.DebugFormat("Creating proxy for SPDP writer {0}", remoteGuid.EntityId);
                
                PublicationData pd = new PublicationData(ParticipantData.BUILTIN_TOPIC_NAME,
                    typeof(PublicationData), remoteGuid);

                wp = CreateProxy(pd);
            }

            return wp;
        }


        protected override WriterProxy CreateProxy(PublicationData pd) {
            WriterProxy wp = new WriterProxy(pd);
            RemoteProxies[pd.BuiltinTopicKey] = wp;

            return wp;
        }
    }
}