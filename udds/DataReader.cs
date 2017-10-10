using System.Collections.Generic;
using rtps;

namespace udds {
    public class DataReader<T> : Entity {
        private readonly RtpsReader _reader;
        internal List<IDataListener<T>> dataListeners = new List<IDataListener<T>>();

        internal DataReader() : base(null, null, null) {
            // Used in tests only    
        }
        
        internal DataReader(Participant p, string topicName, RtpsReader reader) : base(p, topicName, reader.Guid) {
            _reader = reader;
        }

        public void Add(IDataListener<T> dl) => dataListeners.Add(dl);
        public void Remove(IDataListener<T> dl) => dataListeners.Remove(dl);
    }

    public interface ISampleListener<T> {
        void OnSamples(IList<T> samples); // TODO: IList<Sample<T>>
    }
    
    public interface IDataListener<in T> {
        void Create(T sample);
        void Delete(T sample);
        void Update(T sample);
    }
}