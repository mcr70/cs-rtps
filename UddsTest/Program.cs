using System;

using udds;

namespace UddsTest {
    internal class Program {
        private static readonly log4net.ILog log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args) {
            log.Debug("Starting");
            var p = new Participant();
            var dw = p.CreateDataWriter<Hello>();
            
            dw.Write(new Hello());
            var dr = p.CreateDataReader<Hello>();
            var dr2 = p.CreateDataReader<Hello>();
            
            Console.WriteLine("Closing UddsTest");
        }
    }
}