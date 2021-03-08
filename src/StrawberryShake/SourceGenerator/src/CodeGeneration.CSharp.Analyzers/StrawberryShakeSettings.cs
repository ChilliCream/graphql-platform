using System.Collections.Generic;
using StrawberryShake.CodeGeneration.Descriptors.Operations;

namespace StrawberryShake.CodeGeneration.CSharp.Analyzers
{
    public class StrawberryShakeSettings
    {
        public string Name { get; set; } = default!;

        public string? Namespace { get; set; } = default!;

        public string? Url { get; set; }

        public bool DependencyInjection { get; set; } = true;

        public bool StrictSchemaValidation { get; set; } = true;

        public string? HashAlgorithm { get; set; }

        public RequestStrategy RequestStrategy { get; set; } = RequestStrategy.Default;

        public string OutputDirectoryName { get; set; } = ".generated";

        public List<StrawberryShakeTransportSettings>? TransportProfiles { get; set; }
    }
}
