using System.Collections.Generic;

namespace udds {
    public class DataWriter<T> {
        public virtual void Write(T sample) => Write(new List<T> {sample});
        public virtual void Write(IList<T> samples) {            
        }

        public virtual void Delete(T sample) => Delete(new List<T> {sample});        
        public virtual void Delete(IList<T> samples) {            
        }
    }
}