namespace rtps {
    public class RtpsParticipant {
        private readonly Guid _guid;

        public RtpsParticipant(Guid guid) {
            _guid = guid;
        }

        public RtpsReader CreateReader<T>(EntityId eid, IReaderCache<T> rCache) {
            return new RtpsReader(new Guid(_guid.Prefix, eid));
        }

        public RtpsWriter CreateWriter<T>(EntityId eid, IWriterCache<T> rCache) {
            return new RtpsWriter(new Guid(_guid.Prefix, eid));
        }
    }
}