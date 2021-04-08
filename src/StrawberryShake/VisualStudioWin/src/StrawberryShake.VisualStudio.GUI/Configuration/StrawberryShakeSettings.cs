using System.Collections.Generic;

namespace StrawberryShake.Tools.Configuration
{
    public class StrawberryShakeSettings
    {
        public string Name { get; set; }

        public string Namespace { get; set; }

        public string Url { get; set; }

        public bool NoStore { get; set; } = false;

        public bool EmitGeneratedCode { get; set; } = true;

        public StrawberryShakeSettingsRecords Records { get; } =
            new StrawberryShakeSettingsRecords
            {
                Inputs = false,
                Entities = false
            };

        public List<StrawberryShakeTransportSettings> TransportProfiles { get; } =
            new List<StrawberryShakeTransportSettings>
            {
                new StrawberryShakeTransportSettings
                {
                    Default = TransportType.Http,
                    Subscription = TransportType.WebSocket
                }
            };
    }
}
