namespace rtps.message.builtin {
    public class PublicationData : DiscoveredData {
        public static readonly string BUILTIN_TOPIC_NAME = "DCPSPublication";

        public PublicationData(string writerTopicName, System.Type type, Guid writerGuid) : base(writerTopicName, type.Name, writerGuid){
        }
    }
}