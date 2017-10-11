namespace rtps {
    public class RtpsParticipant {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Guid _guid;

        public RtpsParticipant(Guid guid) {
            _guid = guid;
        }

        public RtpsReader CreateReader<T>(EntityId eid, IReaderCache rCache) {
            return new RtpsReader(new Guid(_guid.Prefix, eid));
        }

        public RtpsWriter CreateWriter<T>(EntityId eid, IWriterCache<T> rCache) {
            return new RtpsWriter(new Guid(_guid.Prefix, eid));
        }

        public void Start() {
            Log.Debug("Starting Participant " + _guid.Prefix);
        }
    }
}