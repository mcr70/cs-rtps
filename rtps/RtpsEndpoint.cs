using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime;
using log4net.Config;
using rtps.message;
using rtps.message.builtin;
using rtps.transport;

namespace rtps {
    public abstract class RtpsEndpoint<TProxyType, TProxyData> 
               where TProxyType: RemoteProxy
               where TProxyData: DiscoveredData {

        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public bool Reliable { get; }
        protected readonly Dictionary<Guid, TProxyType> RemoteProxies = new Dictionary<Guid, TProxyType>();
        public readonly Guid Guid;

        
        protected RtpsEndpoint(Guid guid, bool reliable) {
            Reliable = reliable;
            Guid = guid;
        }
        
        public void MatchEndpoint(TProxyData builtinData) {
            Log.DebugFormat("MatchEndpoint({0} -> {0})", Guid, builtinData.BuiltinTopicKey);

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

        //protected abstract TProxyType CreateProxy(TProxyData dd);


        protected void SendMessage(Message msg, TProxyType proxy) {
            Locator loc = proxy.GetLocator();
            if (loc != null) {
                TransportProvider provider = TransportProvider.GetProvider(loc.Kind);
                ITransmitter tr = provider.GetTransmitter(loc);
                tr.SendMessage(msg);
            }
            else {
                Log.WarnFormat("Unable to send message, no suitable Locator for proxy '{0}'", proxy.Guid);               
            }
        }


        protected TProxyType CreateProxy(TProxyData pd)
        {
            TProxyType pt = (TProxyType)System.Activator.CreateInstance(typeof(TProxyType), pd);
            RemoteProxies[pd.BuiltinTopicKey] = pt;

            return pt;
        }

    }
}