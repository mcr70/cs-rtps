using System.Collections.Generic;
using rtps.message;
using rtps.message.builtin;

namespace rtps {

    public class RtpsWriter : RtpsEndpoint<ReaderProxy, SubscriptionData> {
        public RtpsWriter(Guid guid, bool reliable = false) : base(guid, reliable) {
        }

        public void OnAckNack(AckNack ackNack) {
            throw new System.NotImplementedException();
        }
    }
}