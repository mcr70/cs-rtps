using System.Collections.Generic;

namespace rtps.message.builtin {
    public class DiscoveredData {
        public ParameterList Parameters { get;  }
        public string TopicName { get; }
        public string TypeName { get; }
        public Guid BuiltinTopicKey { get; }

        protected DiscoveredData(ParameterList pList) {
            Parameters = pList;
        }
        
        protected DiscoveredData(string topicName, string typeName, Guid guid) {
            TopicName = topicName;
            TypeName = typeName;
            BuiltinTopicKey = guid;
            Parameters = new ParameterList();
        }
    }
}