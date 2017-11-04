using System.Collections.Generic;
using rtps.message;
using rtps.message.builtin;

namespace rtps {
    
    public class Sample {
        public Data Data { get; }
        public Time Timestamp { get; }
        public StatusInfo StatusInfo => Data.StatusInfo;

        public Sample(Data data, Time timestamp) {
            Data = data;
            Timestamp = timestamp;
        }
    }   

    
    public interface IWriterCache {
        uint GetLatestSequenceNumber();
        List<Sample> GetSamplesSince(uint sequenceNumber);
    }
    
    public interface IReaderCache {
        void AddSamples(Guid guid, params Sample[] samples);
    }
}