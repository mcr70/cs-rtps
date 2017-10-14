using rtps.message;
using rtps.message.builtin;

namespace rtps {
    public class RtpsWriter : RtpsEndpoint<ReaderProxy, SubscriptionData> {
        public RtpsWriter(Guid guid, bool reliable = false) : base(guid, reliable) {
        }

        protected override ReaderProxy CreateProxy(SubscriptionData dd) {
            return new ReaderProxy(dd);
        }

        public void OnAckNack(AckNack ackNack) {
            throw new System.NotImplementedException();
        }
    }
}