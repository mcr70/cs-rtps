
using NUnit.Framework;

namespace udds.tests
{
    [TestFixture]
    internal class DataReaderTest
    {
        [TestCase]
        public void TestAddingAndRemovingDataListener() {
            var dr = new DataReader<object>();
            IDataListener <object> dl1 = new DataListener<object>();
            IDataListener <object> dl2 = new DataListener<object>();
            
            dr.Add(dl1);
            dr.Add(dl2);
            Assert.Contains(dl1, dr.dataListeners);
            Assert.Contains(dl2, dr.dataListeners);
            
            dr.Remove(dl1);
            dr.Remove(dl2);
            Assert.That(dr.dataListeners, Has.No.Member(dl1));
            Assert.That(dr.dataListeners, Has.No.Member(dl2));
        }

        private class DataListener<T> : IDataListener<T> {
            public void Create(T sample) {
                throw new System.NotImplementedException();
            }
            public void Delete(T sample) {
                throw new System.NotImplementedException();
            }
            public void Update(T sample) {
                throw new System.NotImplementedException();
            }
        }
    }
}
