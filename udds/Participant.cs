using System;
using System.Collections.Generic;

using rtps;
using rtps.message.builtin;
using Guid = rtps.Guid;

namespace udds {
    public class Participant {
        private static readonly log4net.ILog log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly RtpsParticipant _rtpsParticipant;
        private int userEntityIdx = 0;
        
        public Participant() {
            log.Info("Starting Participant");
            Guid guid = createGuid();
            _rtpsParticipant = new RtpsParticipant(guid);    
        }

        private Guid createGuid() {
            return new Guid(null, null);
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
                int myIdx = userEntityIdx++;
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

            IWriterCache<T> wCache = null; // TODO: implement me
            var rtpsWriter = _rtpsParticipant.CreateWriter(eId, wCache); 
            
            return new DataWriter<T>(this, topicName, rtpsWriter);
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
                int myIdx = userEntityIdx++;
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

            IReaderCache<T> rCache = null; // TODO: implement me
            var rtpsReader = _rtpsParticipant.CreateReader(eId, rCache); 
            
            return new DataReader<T>(this, topicName, rtpsReader);
        }
        
    }
}