namespace rtps.message.builtin {
    public class TopicData : DiscoveredData {
        public static readonly string BUILTIN_TOPIC_NAME = "DCPSTopic";

        protected TopicData(ParameterList pList) : base(pList) {
        }
    }
}