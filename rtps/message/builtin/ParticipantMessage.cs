namespace rtps.message.builtin {
    public class ParticipantMessage : DiscoveredData {
        public static readonly string BUILTIN_TOPIC_NAME = "DCPSParticipantMessage";

        protected ParticipantMessage(ParameterList pList) : base(pList) {
        }
    }
}