using System;
using System.Collections;

using rtps;
using rtps.message.builtin;
using Guid = rtps.Guid;

namespace udds {
    public class Participant {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly Hashtable _dataWriters = new Hashtable();
        private readonly Hashtable _dataReaders = new Hashtable();
        
        private readonly RtpsParticipant _rtpsParticipant;
        private int _userEntityIdx = 1;
        
        public Participant() {
            var guid = createGuid();
            Log.Info("Starting Participant " + guid.Prefix);

            _rtpsParticipant = new RtpsParticipant(guid);

            createBuiltinEntities();

            _rtpsParticipant.Start();
        }


        public DataWriter<T> CreateDataWriter<T>(string topicName = null) {
            if (topicName == null) {
                topicName = typeof(T).Name;
            }

            EntityId eId;
            if (TopicData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SEDP_BUILTIN_TOPIC_WRITER;
            } else if (SubscriptionData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SEDP_BUILTIN_SUBSCRIPTIONS_WRITER;
            } else if (PublicationData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SEDP_BUILTIN_PUBLICATIONS_WRITER;
            } else if (ParticipantData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SPDP_BUILTIN_PARTICIPANT_WRITER;
            } else if (ParticipantMessage.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.BUILTIN_PARTICIPANT_MESSAGE_WRITER;
            } else if (ParticipantStatelessMessage.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.BUILTIN_PARTICIPANT_STATELESS_WRITER;
            } else {
                int myIdx = _userEntityIdx++;
                byte[] myKey = new byte[3];
                myKey[2] = (byte) (myIdx & 0xff);
                myKey[1] = (byte) (myIdx >> 8 & 0xff);
                myKey[0] = (byte) (myIdx >> 16 & 0xff);

                byte kind = 0x02; // User defined writer, with key, see 9.3.1.2 Mapping of the EntityId_t
//                if (!m.hasKey()) { // Marshaller.hasKey
//                    kind = 0x03; // User defined writer, no key
//                }

                eId = new EntityId(myKey, kind);            
            }
            
            if (_dataWriters.ContainsKey(eId)) {
                Log.WarnFormat("Returning existing writer for '{0}'", topicName);
                return _dataWriters[eId] as DataWriter<T>;
            }
            
            IWriterCache<T> wCache = null; // TODO: implement me
            var rtpsWriter = _rtpsParticipant.CreateWriter(eId, wCache); 
            var writer = new DataWriter<T>(this, topicName, rtpsWriter);
            _dataWriters[eId] = writer;
            
            Log.DebugFormat("Created DataWriter for '{0}': {1}", topicName, eId);
            writePublicationData(writer); // Publish our new writer

            return writer;
        }


        public DataReader<T> CreateDataReader<T>(string topicName = null) {
            if (topicName == null) {
                topicName = typeof(T).Name;
            }

            EntityId eId;
            if (TopicData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SEDP_BUILTIN_TOPIC_READER;
            } else if (SubscriptionData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SEDP_BUILTIN_SUBSCRIPTIONS_READER;
            } else if (PublicationData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SEDP_BUILTIN_PUBLICATIONS_READER;
            } else if (ParticipantData.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.SPDP_BUILTIN_PARTICIPANT_READER;
            } else if (ParticipantMessage.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.BUILTIN_PARTICIPANT_MESSAGE_READER;
            } else if (ParticipantStatelessMessage.BUILTIN_TOPIC_NAME.Equals(topicName)) {
                eId = EntityId.BUILTIN_PARTICIPANT_STATELESS_READER;
            } else {
                int myIdx = _userEntityIdx++;
                byte[] myKey = new byte[3];
                myKey[2] = (byte) (myIdx & 0xff);
                myKey[1] = (byte) (myIdx >> 8 & 0xff);
                myKey[0] = (byte) (myIdx >> 16 & 0xff);

                byte kind = 0x07; // User defined reader
//                if (!m.hasKey()) { // (Marshaller.hasKey)
//                    kind = 0x04; // User defined reader, no key
//                }

                eId = new EntityId(myKey, kind);            
            }

            if (_dataReaders.ContainsKey(eId)) {
                Log.WarnFormat("Returning existing reader for '{0}'", topicName);
                return _dataWriters[eId] as DataReader<T>;
            }

            IReaderCache<T> rCache = null; // TODO: implement me
            var rtpsReader = _rtpsParticipant.CreateReader(eId, rCache); 
            var reader = new DataReader<T>(this, topicName, rtpsReader);
            _dataReaders[eId] = reader;
            
            Log.DebugFormat("Created DataReader for '{0}': {1}", topicName, eId);
            writeSubscriptionData(reader);
            
            return reader;
        }


        private void createBuiltinEntities() {
            // Create SPDP Entities
            CreateDataReader<ParticipantData>(ParticipantData.BUILTIN_TOPIC_NAME);
            CreateDataWriter<ParticipantData>(ParticipantData.BUILTIN_TOPIC_NAME);
            // TODO: Add matched reader to SPDP Writer: GuidPrefix.UNKNOWN
            
            // Create SEDP Entities
            CreateDataReader<TopicData>(TopicData.BUILTIN_TOPIC_NAME);
            CreateDataWriter<TopicData>(TopicData.BUILTIN_TOPIC_NAME);
            CreateDataReader<PublicationData>(PublicationData.BUILTIN_TOPIC_NAME);
            CreateDataWriter<PublicationData>(PublicationData.BUILTIN_TOPIC_NAME);
            CreateDataReader<SubscriptionData>(SubscriptionData.BUILTIN_TOPIC_NAME);
            CreateDataWriter<SubscriptionData>(SubscriptionData.BUILTIN_TOPIC_NAME);
        }

        private Guid createGuid() {
            return new Guid(GuidPrefix.GUIDPREFIX_UNKNOWN, EntityId.PARTICIPANT); // TODO: Implement me
        }

        private void writePublicationData<T>(DataWriter<T> writer) {
            PublicationData pd = new PublicationData(); // TODO: implement me
        }

        private void writeSubscriptionData<T>(DataReader<T> reader) {
            SubscriptionData sd = new SubscriptionData();
        }
    }
}