using System;

using udds;

namespace UddsTest {
    internal class Program {
        public static void Main(string[] args) {
            Participant p = new Participant();
            DataWriter<Hello> dw = p.CreateDataWriter<Hello>();
            var dw2 = p.CreateDataWriter<Hello>();
            
            dw.Write(new Hello());
            DataReader<Hello> dr = p.CreateDataReader<Hello>();
            var dr2 = p.CreateDataReader<Hello>();
            
            Console.WriteLine("Closing UddsTest");
        }
    }
}