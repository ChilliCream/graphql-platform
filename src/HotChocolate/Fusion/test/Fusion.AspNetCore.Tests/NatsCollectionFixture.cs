using CookieCrumble.Resources;

namespace HotChocolate.Fusion;

[CollectionDefinition(DefinitionName)]
public class NatsCollectionFixture : ICollectionFixture<NatsResource>
{
    internal const string DefinitionName = "Nats";
}
