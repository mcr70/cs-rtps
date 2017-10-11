using rtps.message;
using rtps.message.builtin;

namespace rtps {
    public class RtpsReader : RtpsEndpoint<WriterProxy, PublicationData> {

        public RtpsReader(Guid guid, bool reliable = false) : base(guid, reliable) {
        }

        
        public void OnGap(GuidPrefix prefix, Gap gap) {
        }

        public void OnHeartbeat(GuidPrefix prefix, Heartbeat hb) {
        }

        public void OnData(int id, GuidPrefix prefix, Data data, Time timestamp) {
        }
        
        protected override WriterProxy CreateProxy(PublicationData dd) {
            return new WriterProxy(dd);
        }
    }
}