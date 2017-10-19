using System.Collections.Concurrent;
using rtps.message;

namespace rtps {
    public class RtpsMessageReceiver {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly RtpsParticipant _participant;
        private readonly GuidPrefix _myPrefix;
        private readonly BlockingCollection<byte[]> _queue;
        private bool _running = true;
        
        public RtpsMessageReceiver(RtpsParticipant p, BlockingCollection<byte[]> queue) {
            _myPrefix = p.Guid.Prefix;
            _participant = p;
            _queue = queue;
        }
 
        // This method is run by the main thread of receiver.
        // We could have multiple main receivers, or we could have receivers
        // per recipient. Or maybe some other scenario.
        public void Run() {
            while (_running) {
                byte[] bytes = _queue.Take(); // TODO: CancellationToken
                Message m = new Message(new RtpsByteBuffer(bytes));
                handleMessage(m);
            }
        }

        
        private void handleMessage(Message message) {
            if (_myPrefix.Equals(message.Header.GuidPrefix)) {
                return; // Ignore message originating from _this_ participant
            }
            
            bool destThisParticipant = true;
            Time timestamp = null;
            GuidPrefix sourcePrefix = message.Header.GuidPrefix;
            GuidPrefix destPrefix;
            
            foreach (var sm in message.SubMessages) {
                switch (sm.getKind()) {
                    case SubMessage.Kind.ACKNACK:
                        if (!destThisParticipant) continue;
                        
                        handleAckNack((AckNack)sm);
                        break;
                    case SubMessage.Kind.DATA:
                        if (!destThisParticipant) continue;
                        
                        handleData(sourcePrefix, (Data)sm, timestamp);
                        break;
                    case SubMessage.Kind.GAP:
                        if (!destThisParticipant) continue;
                        
                        handleGap(sourcePrefix, (Gap)sm);
                        break;
                    case SubMessage.Kind.HEARTBEAT:
                        if (!destThisParticipant) continue;
                        
                        handleHeartbeat(sourcePrefix, (Heartbeat)sm);
                        break;
                    case SubMessage.Kind.INFODESTINATION:
                        destPrefix = ((InfoDestination) sm).GuidPrefix;
                        destThisParticipant = destPrefix.Equals(_myPrefix) ||
                                              destPrefix.Equals(GuidPrefix.UNKNOWN);
                        break;
                    case SubMessage.Kind.INFOSOURCE:
                        sourcePrefix = ((InfoSource) sm).GuidPrefix;
                        break;
                    case SubMessage.Kind.INFOTIMESTAMP:
                        timestamp = ((InfoTimestamp) sm).TimeStamp;
                        break;
                    case SubMessage.Kind.NACKFRAG:
                    case SubMessage.Kind.DATAFRAG:
                    case SubMessage.Kind.HEARTBEATFRAG:
                    case SubMessage.Kind.PAD:
                    case SubMessage.Kind.INFOREPLY:
                    case SubMessage.Kind.INFOREPLYIP4:
                    case SubMessage.Kind.SECURESUBMSG:
                    default:
                        Log.WarnFormat("SubMessage {0} not handled", sm.getKind());
                        break;
                }
            }
        }


        private void handleHeartbeat(GuidPrefix senderPrefix, Heartbeat hb) {
            
            RtpsReader reader = _participant.GetReader(hb.ReaderId);
            if (reader != null) {
                reader.OnHeartbeat(senderPrefix, hb);
            }
            else {
                Log.DebugFormat("No RtpsReader({0}) found to handle Heartbeat", hb.ReaderId);
            }
        }

        private void handleGap(GuidPrefix senderPrefix, Gap gap) {
            RtpsReader reader = _participant.GetReader(gap.ReaderId);
            if (reader != null) {
                reader.OnGap(senderPrefix, gap);
            }
            else {
                Log.DebugFormat("No RtpsReader({0}) found to handle Gap", gap.ReaderId);
            }
        }

        
        
        private void handleData(GuidPrefix senderPrefix, Data data, Time timestamp) {
            if (timestamp == null) {
                timestamp = new Time();
            }
            
            RtpsReader reader = _participant.GetReader(data.ReaderId);
            if (reader != null) {
                reader.OnData(senderPrefix, data, timestamp);
            }
            else {
                Log.DebugFormat("No RtpsReader({0}) found to handle Data", data.ReaderId);
            }
        }

        private void handleAckNack(AckNack ackNack) {
            RtpsWriter writer = _participant.GetWriter(ackNack.WriterId);
            if (writer != null) {
                writer.OnAckNack(ackNack);
            }
            else {
                Log.DebugFormat("No RtpsWriter({0}) found to handle AckNack", ackNack.WriterId);
            }
        }
    }
}