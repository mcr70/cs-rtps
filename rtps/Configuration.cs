using System;
using System.Collections.Generic;
using System.Linq;

namespace rtps {
    public class Configuration {
        // TODO: Consider static'ness
        public static List<Uri> GetDiscoveryListenerUris() {
            return getListenetUris("jrtps.discovery.listener-uris", 
                new String[] {"udp://239.255.0.1:7400", "udp://localhost:7410"});
        }

        private static List<Uri> getListenetUris(string jrtpsDiscoveryListenerUris, string[] strings) {
            // TODO: Implement me. For now, just return defaults
            List<Uri> uris = new List<Uri>();
            foreach (var s in strings) {
                Uri u = new Uri(s);
                int port;
                // TODO: resolve port
                port = u.Port == -1 ? 0 : u.Port;

                // Reconstruct Uri
                string newUriString = u.Scheme + "://" + u.Host + ":" + port + u.PathAndQuery;
                uris.Add(new Uri(newUriString));
            }

            return uris;
        }
    }
}