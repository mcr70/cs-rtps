using rtps;
using rtps.message.builtin;

namespace udds {
    public class Entity
    {
        private readonly Participant _participant;
        public readonly string TopicName;
        
        protected Entity(Participant p, string topicName, Guid writerGuid) {
            _participant = p;
            TopicName = topicName;
        }

        public Participant GetParticipant() => _participant;

    }
}