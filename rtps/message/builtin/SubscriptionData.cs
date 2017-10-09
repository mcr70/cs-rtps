namespace rtps.message.builtin {
    public class SubscriptionData : DiscoveredData {
        public static readonly string BUILTIN_TOPIC_NAME = "DCPSSubscription";

        public bool expectsInlineQos() {
            throw new System.NotImplementedException();
        }

    }
}