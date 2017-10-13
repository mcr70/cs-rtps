using System;
using System.Collections.Concurrent;
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

        public void Run() {
            while (_running) {
                try {
                    byte[] bytes = _queue.Take();
                    Message m = new Message(new RtpsByteBuffer(bytes));
                    handleMessage(m);
                }
                catch (Exception e) {
                    Log.Warn("Exception occured during message handling", e);                    
                }
            }
        }

        private void handleMessage(Message message) {
        }
    }
}