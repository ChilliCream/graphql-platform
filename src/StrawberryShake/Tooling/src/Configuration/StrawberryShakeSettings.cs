using System.Collections.Generic;

namespace StrawberryShake.Tools.Configuration
{
    /// <summary>
    /// The Strawberry Shake generator settings.
    /// </summary>
    public class StrawberryShakeSettings
    {
        /// <summary>
        /// Gets or sets the name of the client.
        /// </summary>
        public string Name { get; set; } = "Client";

        /// <summary>
        /// Gets or sets the namespace of the client.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// Gets or sets the Url to update the schema files.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Defines if the generator shall generate dependency injection code.
        /// </summary>
        public bool DependencyInjection { get; set; } = true;

        /// <summary>
        /// Defines if the generator shall validate the schema.
        /// </summary>
        public bool StrictSchemaValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets the persisted query hash algorithm.
        /// </summary>
        public string HashAlgorithm { get; set; } = "md5";

        /// <summary>
        /// Defines that only a single code file shall be generated.
        /// </summary>
        public bool UseSingleFile { get; set; } = true;

        /// <summary>
        /// Defines the default request strategy.
        /// </summary>
        public RequestStrategy RequestStrategy { get; set; } =
            RequestStrategy.Default;

        /// <summary>
        /// Gets or sets the name of the generated code output directory.
        /// </summary>
        public string OutputDirectoryName { get; set; } = "Generated";

        /// <summary>
        /// Defines if a client shall be generated without a store.
        /// </summary>
        public bool NoStore { get; set; } = false;

        /// <summary>
        /// Defines if the generated code shall be emitted.
        /// </summary>
        public bool EmitGeneratedCode { get; set; } = true;

        /// <summary>
        /// Defines if the generator shall generate blazor query components.
        /// </summary>
        public bool BlazorComponents { get; set; } = false;

        /// <summary>
        /// Gets the record generator settings.
        /// </summary>
        public StrawberryShakeSettingsRecords Records { get; } =
            new()
            {
                Inputs = false,
                Entities = false
            };

        /// <summary>
        /// Gets the transport profiles.
        /// </summary>
        public List<StrawberryShakeSettingsTransportProfile> TransportProfiles { get; } = new();
    }
}
