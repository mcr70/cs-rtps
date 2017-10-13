using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

using rtps.message;

namespace rtps {
    public class RTPSMessageReceiver {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        
        private readonly BlockingCollection<byte[]> _queue;
        private bool _running = true;
        
        public RTPSMessageReceiver(BlockingCollection<byte[]> queue) {
            _queue = queue;
        }
 
        // This method is run by the main Thread of Receiver.
        // We could have multiple main receivers, or we could have receivers
        // per recipient. Or maybe some other scenario.
        public void Run() {
            while (_running) {
                byte[] bytes = _queue.Take(); // TODO: CancellationToken
                // Dispatch another Thread for handling Message
                Task.Run(() => handleBytes(bytes));                    
            }
        }

        // This method is run asynchronously to process bytes into Message,
        // and deliver submessages to recipients.
        private void handleBytes(byte[] bytes) {
            Message m = new Message(new RtpsByteBuffer(bytes));
            handleMessage(m);
        }
        
        private void handleMessage(Message message) {
            // TODO: implement me
        }
    }
}