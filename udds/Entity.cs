using rtps;

namespace udds {
    public class Entity {
        private readonly Participant _participant;
        private readonly RtpsEndpoint _rtpsEndpoint;
        public readonly string TopicName;
        
        protected Entity(Participant p, string topicName, RtpsEndpoint rtpsEndpoint) {
            _participant = p;
            TopicName = topicName;
            _rtpsEndpoint = rtpsEndpoint;
        }

        public Participant GetParticipant() => _participant;
        public Guid GetGuid() => _rtpsEndpoint.Guid;
    }
}