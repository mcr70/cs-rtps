using System.Collections.Generic;
using rtps;
using rtps.message;

namespace udds {
    public class HistoryCache : IReaderCache {
        public readonly List<Sample> CacheChanges = new List<Sample>();
        
        public void AddSamples(Guid guid, params Sample[] samples) {
            foreach (var sample in samples) {
                CacheChanges.Add(sample); // TODO, fix me                
            }
        }
    }

    public class Instance<T> {
    }
}