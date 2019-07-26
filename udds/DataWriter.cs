using System.Collections.Generic;
using rtps;

namespace udds {
    public class DataWriter<T> : Entity {
        internal readonly RtpsWriter _writer;
        
        internal DataWriter(Participant p, string topicName, RtpsWriter writer) : base(p, topicName, writer.Guid) {
            _writer = writer;
        }


        public virtual void Write(params T[] s) => Write((IEnumerable<T>)s);
        public virtual void Write(IEnumerable<T> samples) {
            foreach (var s in samples) {        
            }
        }

        public virtual void Delete(params T[] s) => Delete((IEnumerable<T>)s);
        public virtual void Delete(IEnumerable<T> samples) {            
            foreach (var s in samples) {                
            }
        }
    }
}