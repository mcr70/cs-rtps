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

        public RtpsReader CreateReader(EntityId eId, IReaderCache rCache) {
            Guid rGuid = new Guid(Guid.Prefix, eId);
            RtpsReader reader = new RtpsReader(rGuid, rCache);
            _readers[eId] = reader;

            return reader;
        }

        public RtpsWriter CreateWriter(EntityId eId, IWriterCache rCache) {
            Guid wGuid = new Guid(Guid.Prefix, eId);
            RtpsWriter writer = new RtpsWriter(wGuid);
            _writers[eId] = writer;

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

                // Receiver is an internal receiver using queue for incoming messages
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
            try
            {
                IReceiver rec = tp.GetReceiver(u, queue);
                Task.Run(() => rec.Receive());
            }
            catch(Exception e)
            {
                Log.Error("Failed to start receiver for " + u, e);
            }
        }
    }
}