using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using rtps.message;

namespace rtps.transport {
    public abstract class TransportProvider {
        private static readonly log4net.ILog Log = 
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<string, TransportProvider> _providersForScheme = new Dictionary<string, TransportProvider>();
        private static Dictionary<uint, TransportProvider> _providersForKind = new Dictionary<uint, TransportProvider>();
        
        public static void Register(TransportProvider provider) {
            Log.DebugFormat("Registering provider for scheme '{0}', kind {1}: {2}", 
                provider.GetScheme(), provider.GetKinds(), provider.GetType().Name);
            
            _providersForScheme[provider.GetScheme()] = provider;
            foreach (var k in provider.GetKinds()) {
                _providersForKind[k] = provider;    
            }
        }

        public static TransportProvider getProvider(uint kind) {
            return _providersForKind[kind];
        }

        public abstract string GetScheme();
        public abstract uint[] GetKinds();
        public abstract ITransmitter GetTransmitter(Locator loc);
        public abstract IReceiver GetReceiver(Locator loc, BlockingCollection<byte[]> queue);
    }

    public interface IReceiver {
        void Receive();
        void Close();
    }

    public interface ITransmitter {
        void SendMessage(Message msg);
        void Close();
    }
    
    // ---------------------------------------------------------------------------

    public class UdpProvider : TransportProvider {
        Dictionary<Locator, UdpTransmitter> _transmitters = new Dictionary<Locator, UdpTransmitter>();
        Dictionary<Locator, UdpReceiver> _receivers = new Dictionary<Locator, UdpReceiver>();
        
        public override string GetScheme() {
            return "udp";
        }

        public override uint[] GetKinds() {
            return new uint[] {Locator.LOCATOR_KIND_UDPv4, Locator.LOCATOR_KIND_UDPv6};
        }

        public override ITransmitter GetTransmitter(Locator loc) {
            UdpTransmitter tr = _transmitters[loc];
            if (tr == null) {
                tr = new UdpTransmitter(loc);
                _transmitters[loc] = tr;
            }

            return tr;
        }

        public override IReceiver GetReceiver(Locator loc, BlockingCollection<byte[]> queue) {
            UdpReceiver r = _receivers[loc];
            if (r == null) {
                r = new UdpReceiver((int)loc.Port, queue);
                _receivers[loc] = r;
            }

            return r;
        }
    }
    
    
    public class UdpReceiver : IReceiver {
        private readonly BlockingCollection<byte[]> _queue;
        private readonly UdpClient _client;

        internal UdpReceiver(int port, BlockingCollection<byte[]> queue) {
            _queue = queue;
            _client = new UdpClient(port);
        }


        public void Receive() {
            IPEndPoint remoteEndpoint = new IPEndPoint(IPAddress.Any, 0);
            while (true) {
                byte[] bytes = _client.Receive(ref remoteEndpoint);
                _queue.Add(bytes);
            }
        }

        public void Close() {
            _client.Close();
        }
    }

    internal class UdpTransmitter : ITransmitter {
        private readonly IPEndPoint _endPoint;
        private readonly UdpClient _client;
        
        internal UdpTransmitter(Locator loc) {
            IPAddress ip = new IPAddress(loc.Address);
            _endPoint = new IPEndPoint(ip, (int)loc.Port);
            _client = new UdpClient();
        }
        
        public void SendMessage(Message msg) {
            RtpsByteBuffer bb = new RtpsByteBuffer();
            msg.WriteTo(bb);
            byte[] bytes = bb.ToArray();
            _client.Send(bytes, bytes.Length, _endPoint);
        }

        public void Close() {
            _client.Close();
        }
    }

}