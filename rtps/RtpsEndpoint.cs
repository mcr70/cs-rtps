using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using rtps.message.builtin;

namespace rtps {
    public abstract class RtpsEndpoint<TProxyType, TProxyData> 
               where TProxyType: RemoteProxy
               where TProxyData: DiscoveredData {
        public bool Reliable { get; }

        protected readonly Dictionary<Guid, TProxyType> RemoteProxies = 
            new Dictionary<Guid, TProxyType>();

        public readonly Guid Guid;

        protected RtpsEndpoint(Guid guid, bool reliable) {
            Reliable = reliable;
            Guid = guid;
        }
        
        public void MatchEndpoint(TProxyData builtinData) {
            if (RemoteProxies.ContainsKey(builtinData.BuiltinTopicKey)) {
                RemoteProxies[builtinData.BuiltinTopicKey].DiscoveredData = builtinData;
            }
            else {
                RemoteProxies[builtinData.BuiltinTopicKey] = CreateProxy(builtinData);
            }

            // TODO: implement concurrent behaviour, could this work:
            //ConcurrentDictionary<Guid, TProxyType> cd = null;
            //cd.AddOrUpdate(builtinData.BuiltinTopicKey, 
            //    CreateProxy(builtinData), 
            //    (guid, type) => CreateProxy(builtinData));
        }

        public void UnmatchEndpoint(TProxyData pd) {
            RemoteProxies.Remove(pd.BuiltinTopicKey);
        }

        protected abstract TProxyType CreateProxy(TProxyData dd);
    }
}