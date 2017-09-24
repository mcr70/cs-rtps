using System;

namespace rtps {
    /// <summary>
    /// SecureSubMessage is used to wrap one or more RTPS submessages.
    /// Contents of wrapped submessages are secured as specified by 
    /// transformationKind and transformationId.
    /// 
    /// @author mcr70
    /// </summary>
    public class SecureSubMessage : SubMessage {
        public const int KIND = 0x30;

        private SecurePayload payload;

        public SecureSubMessage(SecurePayload payload) : base(new SubMessageHeader(KIND)) {
            this.payload = payload;
        }

        internal SecureSubMessage(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            UInt32 transformationKind = bb.read_long();
            byte[] trasformationId = new byte[8];
            bb.read(trasformationId);

            byte[] cipherText = new byte[bb.read_long()];
            bb.read(cipherText);

            this.payload = new SecurePayload(transformationKind, trasformationId, cipherText);
        }


        /// <summary>
        /// Gets the value of singleSubMessageFlag. If this flag is set, SecureSubMessage
        /// is an envelope for a single RTPS submessage. Otherwise, SecureSubMessage
        /// is an envelope for a full RTPS message.
        /// </summary>
        /// <returns> true, if only one submessage is encapsulated in SecuredPayload </returns>
        public virtual bool SingleSubMessageFlag {
            get {
                return (header.flags & 0x2) != 0;
            }
            set {
                if (value) {
                    header.flags |= 0x2;
                }
                else {
                    header.flags = (byte) (header.flags & ~0x2);
                }                
            }
        }

        public virtual SecurePayload SecurePayload => payload;

        public override void WriteTo(RTPSByteBuffer bb) {
            payload.writeTo(bb);
        }
    }


    public class SecurePayload {
        private UInt32 transformationKind;
        private byte[] trasformationId;
        private byte[] cipherText;

        public SecurePayload() {
        }

        public SecurePayload(UInt32 transformationKind, byte[] trasformationId, byte[] cipherText) {
            this.transformationKind = transformationKind;
            this.trasformationId = trasformationId;
            this.cipherText = cipherText;
        }

        internal void writeTo(RTPSByteBuffer bb) {
            throw new NotImplementedException();
        }
    }
}