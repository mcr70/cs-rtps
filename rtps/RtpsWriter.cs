using rtps.message.builtin;

namespace rtps {
    public class RtpsWriter : RtpsEndpoint<ReaderProxy, SubscriptionData> {
        public RtpsWriter(Guid guid) : base(guid) {
        }

        protected override ReaderProxy CreateProxy(SubscriptionData dd) {
            return new ReaderProxy(dd);
        }
    }
}