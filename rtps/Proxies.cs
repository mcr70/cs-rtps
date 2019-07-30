using System;
using System.Collections.Generic;
using rtps.message;
using rtps.message.builtin;

namespace rtps {
    public class RemoteProxy {
        private static readonly log4net.ILog Log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Guid Guid { get; internal set; }
        public DiscoveredData DiscoveredData { get; internal set; }
        public bool Reliable { get; internal set;}

        private Dictionary<GuidPrefix, ParticipantData> discoveredParticipants = new Dictionary<GuidPrefix, ParticipantData>();
        
        protected RemoteProxy(DiscoveredData dd) {
            DiscoveredData = dd;
            Guid = dd.BuiltinTopicKey;
            
            Reliable = true; // TODO: Use Reliability QoS
        }

        
        public List<Locator> GetLocators() {
            List<Locator> locators = new List<Locator>();

            // check if proxys discovery data contains locator info
            if (!(DiscoveredData.GetType() == typeof(ParticipantData))) {
                foreach (var p in DiscoveredData.Parameters) {
                    if (p.GetType() == typeof(LocatorParam)) {
                        locators.Add(((LocatorParam)p).Locator);
                    }
                }
            }

            // Add default locators from Participant Data
            Guid remoteGuid = DiscoveredData.BuiltinTopicKey;

            // Set the default locators from ParticipantData
            ParticipantData pd = discoveredParticipants[remoteGuid.Prefix];
            if (pd != null) {
                if (remoteGuid.EntityId.IsBuiltinEntity()) {
                    locators.InsertRange(0, pd.GetDiscoveryLocators());
                } 
                else {
                    locators.AddRange(pd.GetUserdataLocators());
                }
            }
            else {
                Log.WarnFormat("ParticipantData was not found for {0}, cannot set default locators", remoteGuid);
            }

            return locators;
        }

        
        
        public Locator GetLocator() {
            return null;
        }
    }
    
    public class WriterProxy : RemoteProxy {
        /// <summary>
        /// Sequence number of latest sample known. It will be changed when Data or Gap
        /// Message is received.
        /// </summary>
        public long SeqNumMax { get; private set; }

        /// <summary>
        /// EntityId of the remote writer
        /// </summary>
        public EntityId EntityId {
            get { return DiscoveredData.BuiltinTopicKey.EntityId; }
            //private set { }
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

        /// <summary>
        /// Checks, whether or not Data being received should be accepted or not
        /// </summary>
        /// <param name="data"></param>
        /// <param name="reliable"></param>
        /// <returns></returns>
        public bool ApplyData(Data data, bool reliable) {
            if (data.WriterSequenceNumber > SeqNumMax) {
                // TODO: What to do with this??? Now we just accept data, and miss out of order data
                
                //if (reliable && data.WriterSequenceNumber > SeqNumMax + 1 && SeqNumMax != 0) {
                //    Log.WarnFormat("Accepting data even though some data has been missed: offered seq-num {0}, my received seq-num {1}",
                //            data.WriterSequenceNumber, SeqNumMax);
                //}
    
                SeqNumMax = data.WriterSequenceNumber;
    
                return true;
            }

            return false;
        }
    }

    public class ReaderProxy : RemoteProxy {
        public ReaderProxy(SubscriptionData sd) : base(sd) {
        }
    }
}