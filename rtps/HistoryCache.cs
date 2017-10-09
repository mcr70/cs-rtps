using System.Collections.Generic;
using rtps.message;

namespace rtps {
    public interface IReaderCache<T> {
        void AddChange(int id, Guid guid, Data data, Time timestamp);
    }

    public interface IWriterCache<T> {
        void Write(T data, Time timestamp);
        void Dispose(T data, Time timestamp);
    }

    public interface IHistoryCache<T> {
        HashSet<Instance<T>> GetInstances();
    }

    public class Instance<T> {
    }
}