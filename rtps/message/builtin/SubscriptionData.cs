namespace rtps.message.builtin {
    public class SubscriptionData : DiscoveredData {
        public static readonly string BUILTIN_TOPIC_NAME = "DCPSSubscription";

        public SubscriptionData(string readerTopicName, System.Type type, Guid readerGuid) : base(readerTopicName, type.Name, readerGuid) { 
        }

        public bool ExpectsInlineQos() {
            throw new System.NotImplementedException();
        }
    }
}