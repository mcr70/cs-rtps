using rtps.message.builtin;

namespace rtps {
    public class RtpsReader : RtpsEndpoint<WriterProxy, PublicationData> {      
        public RtpsReader(Guid guid) : base(guid) {
        }

        protected override WriterProxy CreateProxy(PublicationData dd) {
            return new WriterProxy(dd);
        }
    }
}