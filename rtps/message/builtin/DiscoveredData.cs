using System.Collections.Generic;

namespace rtps.message.builtin {
    public class DiscoveredData {
        public ParameterList Parameters { get;  }
        public string TopicName { get; }
        public string TypeName { get; }
        public Guid BuiltinTopicKey { get; }

        protected DiscoveredData(ParameterList pList) {
            Parameters = pList;

            foreach (var p in Parameters) {
                switch (p.Id) {
                case ParameterId.PID_BUILTIN_TOPIC_KEY:
                    BuiltinTopicKey = ((BuiltinTopicKey)p).Guid;    
                    break;
                case ParameterId.PID_TOPIC_NAME:
                    TopicName = ((TopicName)p).Name;
                    break;
                case ParameterId.PID_TYPE_NAME:
                    TypeName = ((TypeName)p).Name;
                    break;
                }
            }
        }
        
        protected DiscoveredData(string topicName, string typeName, Guid guid) {
            TopicName = topicName;
            TypeName = typeName;
            BuiltinTopicKey = guid;
            Parameters = new ParameterList();
        }
    }
}