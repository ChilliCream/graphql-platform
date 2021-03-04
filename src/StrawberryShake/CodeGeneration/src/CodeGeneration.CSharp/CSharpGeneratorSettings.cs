using System.Collections.Generic;
using HotChocolate.Language;
using RequestStrategyGen = StrawberryShake.CodeGeneration.Descriptors.Operations.RequestStrategy;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class CSharpGeneratorSettings
    {
        public string ClientName { get; set; } = "GraphQLClient";

        public string Namespace { get; set; } = "StrawberryShake.GraphQL";

        public bool StrictSchemaValidation { get; set; } = true;

        public RequestStrategyGen RequestStrategy { get; set; } =
            RequestStrategyGen.Default;

        public IDocumentHashProvider HashProvider { get; set; } =
            new Sha1DocumentHashProvider(HashFormat.Hex);

        public List<CSharpGeneratorTransportProfile> TransportProfiles { get; } =
            new()
            {
                new CSharpGeneratorTransportProfile(
                    "Default",
                    TransportType.Http,
                    subscription: TransportType.WebSocket)
            };
    }
}
