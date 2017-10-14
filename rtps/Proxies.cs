using System;
using rtps.message;
using rtps.message.builtin;

namespace rtps {
    public class RemoteProxy {
        public DiscoveredData DiscoveredData { get; internal set; }

        protected RemoteProxy(DiscoveredData dd) {
            DiscoveredData = dd;
        }
    }
    
    public class WriterProxy : RemoteProxy {
        public long SeqNumMax { get; private set; }
        public EntityId EntityId {
            get { return DiscoveredData.BuiltinTopicKey.EntityId; }
            private set { }
        }

        private int _hbSuppressionDuration = 0; // TODO: Configurable
        private Heartbeat _latestHeartbeat;
        private DateTime _latestHbReceiveTime;
        
        public WriterProxy(PublicationData pd) : base(pd) {
        }

        internal void ApplyGap(Gap gap) {
            AssertLiveliness();
            // If the gap start is smaller than or equal to current seqNum + 1 (next seqNum)...
            if (gap.GapStart <= SeqNumMax + 1) {
                // ...and gap end is greater than current seqNum...
                if (gap.GapEnd > SeqNumMax) {
                    SeqNumMax = gap.GapEnd; // ...then mark current seqNum to be gap end.
                }
            }
        }

        internal bool ApplyHeartbeat(Heartbeat hb) {
            AssertLiveliness();
            DateTime now = DateTime.Now;
            
            if (_latestHeartbeat == null) {
                _latestHeartbeat = hb;
                _latestHbReceiveTime = now;
                return true;
            }
            
            // Accept only if count > than previous, and enough time (suppression duration) has
            // elapsed since previous HB
            if (hb.Count > _latestHeartbeat.Count && 
                now > _latestHbReceiveTime + TimeSpan.FromMilliseconds(_hbSuppressionDuration)) {
                _latestHeartbeat = hb;
                _latestHbReceiveTime = now;
                return true;
            }

            return false;
        }

        public void AssertLiveliness() {
            // TODO: implement me
        }

        public bool IsAllReceived() {
            if (_latestHeartbeat == null) {
                return false;
            }

            return _latestHeartbeat.LastSequenceNumber == SeqNumMax;
        }


        internal SequenceNumberSet GetSequenceNumberSet() {
            long _base = SeqNumMax + 1;
            long firstSN = _latestHeartbeat.FirstSequenceNumber;
            long lastSN = _latestHeartbeat.LastSequenceNumber;
            uint numBits; 
        
            if (_base < firstSN) {
                _base = firstSN;
                numBits = (uint) (lastSN - firstSN + 1);
            }
            else {
                numBits = (uint) (lastSN - _base + 1);
            }
        
            if (numBits > 256) {
                numBits = 256;
            }
        
            return new SequenceNumberSet(_base, numBits);
        }

    }

    public class ReaderProxy : RemoteProxy {
        public ReaderProxy(SubscriptionData sd) : base(sd) {
        }
    }
}