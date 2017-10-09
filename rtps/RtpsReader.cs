using System.Collections.Generic;
using rtps.message.builtin;

namespace rtps {
    public class RtpsReader : RtpsEndpoint {
        private readonly Dictionary<Guid, WriterProxy> _writerProxies = 
            new Dictionary<Guid, WriterProxy>();
        
        public RtpsReader(Guid guid) : base(guid) {
        }

        public void MatchWriter(PublicationData pd) {
            if (_writerProxies.ContainsKey(pd.BuiltinTopicKey)) {
                _writerProxies[pd.BuiltinTopicKey].DiscoveredData = pd;
            }
            else {
                _writerProxies[pd.BuiltinTopicKey] = new WriterProxy(pd);
            }
        }

        public void UnmatchWriter(PublicationData pd) {
            _writerProxies.Remove(pd.BuiltinTopicKey);
        }
    }
}