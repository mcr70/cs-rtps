using System.Collections.Generic;

namespace udds {
    public class DataReader<T> {
        internal List<IDataListener<T>> dataListeners = new List<IDataListener<T>>();
        
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