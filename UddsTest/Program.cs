using System;
using rtps.message.builtin;
using udds;

namespace UddsTest {
    internal class Program {
        private static readonly log4net.ILog log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args) {
            log.Debug("Starting");
            var p = new Participant();
            var dw = p.CreateDataWriter<Hello>();
            var dr = p.CreateDataReader<Hello>();
            dw.Write(new Hello());

            Console.WriteLine("Press enter to close");
            Console.ReadLine();
            log.Debug("Closing UddsTest");
        }
    }
}