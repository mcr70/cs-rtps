using System.Collections.Generic;
using rtps;

namespace udds {
    public class DataWriter<T> : Entity {
        private readonly RtpsWriter _writer;
        
        internal DataWriter(Participant p, string topicName, RtpsWriter writer) : base(p, topicName, writer.Guid) {
            _writer = writer;
        }

        public virtual void Write(T sample) => Write(new List<T> {sample});
        public virtual void Write(IList<T> samples) {
            foreach (var s in samples) {
            }
        }

        public virtual void Delete(T sample) => Delete(new List<T> {sample});        
        public virtual void Delete(IList<T> samples) {            
            foreach (var s in samples) {                
            }
        }
    }
}