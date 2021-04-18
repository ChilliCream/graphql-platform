using System.Collections.Generic;

namespace StrawberryShake.Tools.Configuration
{
    public class StrawberryShakeSettings
    {
        public string Name { get; set; } = "Client";

        public string? Namespace { get; set; }

        public string? Url { get; set; }

        public bool DependencyInjection { get; set; } = true;

        public bool StrictSchemaValidation { get; set; } = true;

        public string HashAlgorithm { get; set; } = "md5";

        public bool UseSingleFile { get; set; } = true;

        public RequestStrategy RequestStrategy { get; set; } =
            RequestStrategy.Default;

        public string OutputDirectoryName { get; set; } = "Generated";

        public bool NoStore { get; set; } = false;

        public bool EmitGeneratedCode { get; set; } = true;

        public StrawberryShakeSettingsRecords Records { get; } =
            new()
            {
                Inputs = false,
                Entities = false
            };

        public List<StrawberryShakeTransportProfile> TransportProfiles { get; } = new();
    }
}
