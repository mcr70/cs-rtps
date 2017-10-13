namespace rtps.message.builtin {
    public class DiscoveredData {
        public string TopicName { get; }
        public string TypeName { get; }
        public Guid BuiltinTopicKey { get; }

        protected DiscoveredData() {}
        
        protected DiscoveredData(string topicName, string typeName, Guid guid) {
            TopicName = topicName;
            TypeName = typeName;
            BuiltinTopicKey = guid;
        }
    }
}