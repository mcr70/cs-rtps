using System;
using System.Collections.Generic;

namespace rtps {
    /// <summary>
    /// see 8.3.7.3 DataFrag
    /// 
    /// @author mcr70
    /// 
    /// </summary>
    public class DataFrag : SubMessage {
        public const int KIND = 0x16;

        private UInt16 extraFlags;
        private EntityId readerId;
        private EntityId writerId;
        private SequenceNumber writerSN;
        private UInt32 fragmentStartingNum;
        private UInt16 fragmentsInSubmessage;
        private UInt16 fragmentSize;
        private UInt32 sampleSize;

        private IList<Parameter> parameterList = new List<Parameter>();
        private byte[] serializedPayload;

        public DataFrag(SubMessageHeader smh, RTPSByteBuffer bb) : base(smh) {
            readMessage(bb);
        }

        public virtual bool inlineQosFlag() {
            return (header.flags & 0x2) != 0;
        }

        public virtual bool keyFlag() {
            return (header.flags & 0x4) != 0;
        }

        public virtual EntityId ReaderId => readerId;

        public virtual EntityId WriterId => writerId;

        public virtual SequenceNumber WriterSequenceNumber => writerSN;

        public virtual UInt32 FragmentStartingNumber => fragmentStartingNum;

        public virtual UInt16 FragmentsInSubmessage => fragmentsInSubmessage;

        public virtual UInt16 FragmentSize => fragmentSize;

        public virtual UInt32 SampleSize => sampleSize;

        public virtual IList<Parameter> ParameterList => parameterList;

        public virtual byte[] SerializedPayload => serializedPayload;

        private void readMessage(RTPSByteBuffer bb) {
            long start_count = bb.Position; // start of bytes read so far from the
            // beginning

            this.extraFlags = bb.read_short();
            int octetsToInlineQos = bb.read_short() & 0xffff;

            long currentCount = bb.Position; // count bytes to inline qos

            this.readerId = new EntityId(bb);
            this.writerId = new EntityId(bb);
            this.writerSN = new SequenceNumber(bb);

            this.fragmentStartingNum = bb.read_long(); // ulong
            this.fragmentsInSubmessage = bb.read_short(); // ushort
            this.fragmentSize = bb.read_short(); // ushort
            this.sampleSize = bb.read_long(); // ulong

            long bytesRead = bb.Position - currentCount;
            long unknownOctets = octetsToInlineQos - bytesRead;

            for (int i = 0; i < unknownOctets; i++) {
                bb.read_octet(); // Skip unknown octets, @see 9.4.5.3.3 octetsToInlineQos
            }

            if (inlineQosFlag()) {
                readParameterList(bb);
            }

            long end_count = bb.Position; // end of bytes read so far from the beginning

            this.serializedPayload = new byte[header.submessageLength - (end_count - start_count)];
            bb.read(serializedPayload);
        }

        /// 
        /// <param name="bb"> </param>
        /// <exception cref="IOException"> </exception>
        /// <seealso cref= 9.4.2.11 ParameterList </seealso>
        private void readParameterList(RTPSByteBuffer bb) {
            while (true) {
                bb.align(4);
                Parameter param = ParameterFactory.readParameter(bb);
                parameterList.Add(param);
                if (param.Id == ParameterId.PID_SENTINEL) {
                    break;
                }
            }
        }

        public override void WriteTo(RTPSByteBuffer bb) {
            bb.write_short(extraFlags);

            UInt16 octets_to_inline_qos = 4 + 4 + 8 + 4 + 2 + 2 + 4;
            bb.write_short(octets_to_inline_qos);

            readerId.WriteTo(bb);
            writerId.WriteTo(bb);
            writerSN.WriteTo(bb);

            bb.write_long(fragmentStartingNum);
            bb.write_short(fragmentsInSubmessage);
            bb.write_short(fragmentSize);
            bb.write_long(sampleSize);

            if (inlineQosFlag()) {
                writeParameterList(bb);
            }

            bb.write(serializedPayload); // TODO: check this
        }

        private void writeParameterList(RTPSByteBuffer buffer) {
            foreach (Parameter param in parameterList) {
                param.WriteTo(buffer);
            }

            // TODO: last Parameter must be PID_SENTINEL
        }
    }
}