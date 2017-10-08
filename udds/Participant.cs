using System;
using System.Collections.Generic;

namespace udds {
    public class Participant {
        private static readonly log4net.ILog log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public Participant() {
            log.Info("Starting Participant");
            Console.WriteLine("PPP");
        }

        public DataWriter<T> CreateDataWriter<T>() {
            Console.WriteLine("T: " + typeof(T));
            DataWriter<T> t;
            
            return null;
        }

        public DataReader<T> CreateDataReader<T>() {
            List<DataReader<T>> readers = new List<DataReader<T>>();
            Console.WriteLine("T: " + typeof(T));
            return null;
        }
    }
}