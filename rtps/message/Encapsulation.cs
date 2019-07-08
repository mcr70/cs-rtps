
namespace rtps.message
{

    public class DataEncapsulation
    {
        public byte[] SerializedPayload { get; internal set; }

        internal bool ContainsData()
        {
            throw new System.NotImplementedException();
        }

        internal static DataEncapsulation CreateInstance(byte[] serializedPayload)
        {
            RtpsByteBuffer bb = new RtpsByteBuffer(serializedPayload);
            byte[] encapsulationHeader = new byte[2];
            bb.read(encapsulationHeader);

            short eh = (short)(((short)encapsulationHeader[0] << 8) | (encapsulationHeader[1] & 0xff));

            switch (eh)
            {
            case 0:
            case 1:
                bool littleEndian = (eh & 0x1) == 0x1;
                bb.IsLittleEndian = littleEndian;

                return new CDREncapsulation(bb);
            case 2:
            case 3:
                littleEndian = (eh & 0x1) == 0x1;
                bb.IsLittleEndian = littleEndian;

                return new ParameterListEncapsulation(bb);
            }

            // TODO: handle this more gracefully
            return null;
        }
    }

    internal class ParameterListEncapsulation : DataEncapsulation
    {
        private ushort _options;
        private ParameterList _parameters;

        public ParameterListEncapsulation(RtpsByteBuffer bb)
        {
            _parameters = new ParameterList(bb);
            _options = bb.read_short(); // Not used
        }
    }


    internal class CDREncapsulation : DataEncapsulation
    {
        private readonly RtpsByteBuffer _bb;
        private ushort _options;

        public CDREncapsulation(RtpsByteBuffer bb)
        {
            _bb = bb;
            _options = bb.read_short(); // Not used
        }
    }
}