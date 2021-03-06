namespace rtps.message {
    /// <summary>
    /// The purpose of this Submessage is to allow the introduction of any padding
    /// necessary to meet any desired memory alignment requirements. Its has no other
    /// meaning.
    /// 
    /// see 8.3.7.11 Pad
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class Pad : SubMessage {
        public const int KIND = 0x01;

        private byte[] bytes;

        public Pad(SubMessageHeader smh, RtpsByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        private void readMessage(RtpsByteBuffer bb) {
            this.bytes = new byte[header.submessageLength];
            bb.read(bytes);
        }

        public override void WriteTo(RtpsByteBuffer bb) {
            bb.write(bytes);
        }
    }
}