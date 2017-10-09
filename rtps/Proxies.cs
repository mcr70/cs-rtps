using System;
using System.Collections.Generic;
using System.Net;
using rtps.message.builtin;

namespace rtps {
    public class RemoteProxy {
        public DiscoveredData DiscoveredData { get; internal set; }

        protected RemoteProxy(DiscoveredData dd) {
            DiscoveredData = dd;
        }
    }
    
    public class WriterProxy : RemoteProxy {
        public WriterProxy(PublicationData pd) : base(pd) {
        }
    }

    public class ReaderProxy : RemoteProxy {
        public ReaderProxy(SubscriptionData sd) : base(sd) {
        }
    }
}