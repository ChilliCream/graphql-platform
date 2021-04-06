using System.Collections.Generic;
using HotChocolate.Language;
using RequestStrategyGen = StrawberryShake.CodeGeneration.Descriptors.Operations.RequestStrategy;

namespace StrawberryShake.CodeGeneration.CSharp
{
    /// <summary>
    /// The csharp generator settings.
    /// </summary>
    public class CSharpGeneratorSettings
    {
        /// <summary>
        /// The name of the client class.
        /// </summary>
        public string ClientName { get; set; } = "GraphQLClient";

        /// <summary>
        /// The root namespace of the client.
        /// </summary>
        public string Namespace { get; set; } = "StrawberryShake.GraphQL";

        /// <summary>
        /// Defines if a schema needs to be fully valid.
        /// </summary>
        public bool StrictSchemaValidation { get; set; } = true;

        /// <summary>
        /// Generates the client without a store
        /// </summary>
        public bool NoStore { get; set; }

        /// <summary>
        /// Generates input types as records.
        /// </summary>
        public bool InputRecords { get; set; }

        /// <summary>
        /// Generate a single CSharp code file.
        /// </summary>
        public bool SingleCodeFile { get; set; } = true;

        /// <summary>
        /// The default request strategy.
        /// </summary>
        public RequestStrategyGen RequestStrategy { get; set; } =
            RequestStrategyGen.Default;

        /// <summary>
        /// The <see cref="IDocumentHashProvider"/> that shall be used for persisted queries.
        /// </summary>
        public IDocumentHashProvider HashProvider { get; set; } =
            new Sha1DocumentHashProvider(HashFormat.Hex);

        /// <summary>
        /// The transport profiles that shall be generated.
        /// </summary>
        public List<TransportProfile> TransportProfiles { get; set; } =
            new()
            {
                new TransportProfile(
                    TransportProfile.DefaultProfileName,
                    TransportType.Http,
                    subscription: TransportType.WebSocket)
            };
    }
}
