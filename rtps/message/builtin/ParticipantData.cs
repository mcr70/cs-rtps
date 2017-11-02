using System.Collections.Generic;
using System.Linq;

namespace rtps.message.builtin {
    public class ParticipantData : DiscoveredData {
        public static readonly string BUILTIN_TOPIC_NAME = "DCPSParticipants";
        private readonly List<Locator> _discoveryLocators;
        private readonly List<Locator> _userdataLocators;


        public ParticipantData(ParameterList parameterList) : base(parameterList) {
            _discoveryLocators = new List<Locator>();
            // Find discovery locators
            var parms = Parameters.Where(p => p.Id == ParameterId.PID_METATRAFFIC_MULTICAST_LOCATOR ||
                                              p.Id == ParameterId.PID_METATRAFFIC_UNICAST_LOCATOR);
            foreach (var p in parms) {
                _discoveryLocators.Add(((LocatorParam)p).Locator);
            }
            
            // Find user data locators
            var uLocs = Parameters.Where(p => p.Id == ParameterId.PID_DEFAULT_MULTICAST_LOCATOR ||
                                              p.Id == ParameterId.PID_DEFAULT_UNICAST_LOCATOR);
            foreach (var p in uLocs) {
                _userdataLocators.Add(((LocatorParam)p).Locator);
            }
        }

        public List<Locator> GetDiscoveryLocators() {
            return _discoveryLocators;
        }

        /**
         * Gets the list of Locators that can be used for user data
         * @return List of Locators for user data
         */
        public List<Locator> GetUserdataLocators() {
            return _userdataLocators;
        }
    }
}