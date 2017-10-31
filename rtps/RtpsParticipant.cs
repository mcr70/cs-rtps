using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using rtps.transport;

namespace rtps {
    public class RtpsParticipant {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<EntityId, RtpsReader> _readers = new Dictionary<EntityId, RtpsReader>(); 
        private Dictionary<EntityId, RtpsWriter> _writers = new Dictionary<EntityId, RtpsWriter>();

        public Guid Guid { get; internal set; }

        public RtpsParticipant(Guid guid) {
            Guid = guid;
        }

        public RtpsReader CreateReader(EntityId eid, HistoryCache rCache) {
            Guid rGuid = new Guid(Guid.Prefix, eid);
            RtpsReader reader = new RtpsReader(rGuid);
            _readers[eid] = reader;
            
            return reader;
        }

        public RtpsWriter CreateWriter(EntityId eid, HistoryCache rCache) {
            Guid wGuid = new Guid(Guid.Prefix, eid);
            RtpsWriter writer = new RtpsWriter(wGuid);
            _writers[eid] = writer;
            
            return writer;
        }

        public RtpsReader GetReader(EntityId eid) {
            return _readers[eid];
        }
        
        public RtpsWriter GetWriter(EntityId eid) {
            return _writers[eid];
        }
        
        public void Start() {
            Log.Debug("Starting Participant " + Guid.Prefix);
            foreach (var uri in Configuration.GetDiscoveryListenerUris()) {
                BlockingCollection<byte[]> queue = new BlockingCollection<byte[]>();
            
                RtpsMessageReceiver receiver = new RtpsMessageReceiver(this, queue);
                Task.Run(() => receiver.Run());

                startReceiver(queue, uri);   
            }            
        }

        private void startReceiver(BlockingCollection<byte[]> queue, Uri u) {
            TransportProvider tp = TransportProvider.GetProvider(u.Scheme);
            if (tp == null) {
                Log.WarnFormat("Failed to get Uri for scheme {0}", u.Scheme);
                return;
            }

            Log.DebugFormat("Starting Receiver for {0}", u);     
            IReceiver rec = tp.GetReceiver(u, queue);
            Task.Run(() => rec.Receive());
        }
    }
}