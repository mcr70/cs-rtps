using System.Collections.Generic;
using rtps.message;
using rtps.message.builtin;

namespace rtps {
    
    public class Sample<T> {
        public T Data { get; internal set; }
        public Time Timestamp { get; }
        public StatusInfo StatusInfo => SubMessage.StatusInfo;
        public long SequenceNumber { get; }
        internal Data SubMessage;

        /// <summary>
        /// Creates new Sample with given data
        /// </summary>
        /// <param name="data">Data that is represented by this Sample</param>
        /// <param name="time">If Time is not given, current time is used</param>
        public Sample(T data, Time time = null)
        {
            Data = data;
            if (time == null)
            {
                time = new Time();
            }

            Timestamp = time;
        }

        /// <summary>
        /// Constructor used when receiving Data from network
        /// </summary>
        /// <param name="data"></param>
        /// <param name="timestamp"></param>
        internal Sample(Data data, Time timestamp) {
            SubMessage = data;
            Timestamp = timestamp;
            SequenceNumber = data.WriterSequenceNumber;
        }

        public T GetData()
        {
            return default(T);
        }


    }   


    public interface Cache<T>
    {
        void Add(params Sample<T>[] samples);
        void Add(IEnumerable<T> samples);
    }

    public interface IWriterCache {
        uint GetLatestSequenceNumber();
        List<Sample> GetSamplesSince(uint sequenceNumber);
    }
    
    public interface IReaderCache {
        void AddSamples(Guid guid, params Sample[] samples);
    }
}