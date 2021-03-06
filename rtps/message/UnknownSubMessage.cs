namespace rtps.message {
    /// <summary>
    /// UnknownSubMessage. If an unknown SubMessage is received, it is wrapped in
    /// this class. Implementation does not known what to do with it, but rest of the
    /// SubMessages are processed.
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class UnknownSubMessage : SubMessage {
        private byte[] bytes;

        public UnknownSubMessage(SubMessageHeader smh, RtpsByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        private void readMessage(RtpsByteBuffer bb) {
            bytes = new byte[header.submessageLength];
            bb.read(bytes);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(bytes);
        }
    }
}