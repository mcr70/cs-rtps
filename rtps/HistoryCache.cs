using System.Collections.Generic;
using rtps.message;

namespace rtps {
    public class HistoryCache {
        public readonly List<CacheChange> CacheChanges = new List<CacheChange>();
        
        public void AddChange(int id, Guid guid, Data data, Time timestamp) {
            CacheChanges.Add(new CacheChange(id, guid, data, timestamp));
        }
    }

    public class CacheChange {
        public int Id { get; }
        public Guid Guid { get; }
        public Data Data { get; }
        public Time Timestamp { get; }

        internal CacheChange(int id, Guid guid, Data data, Time timestamp) {
            Id = id;
            Guid = guid;
            Data = data;
            Timestamp = timestamp;
        }
    }
    
    public interface IReaderCache {
        void AddChange(int id, Guid guid, Data data, Time timestamp);
    }

    public interface IWriterCache<T> {
        void Write(T data, Time timestamp);
        void Dispose(T data, Time timestamp);
    }


    public class Instance<T> {
    }
}