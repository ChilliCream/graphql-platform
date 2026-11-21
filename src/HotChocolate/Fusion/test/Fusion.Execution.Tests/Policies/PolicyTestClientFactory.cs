using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Policies;

internal sealed class PolicyTestClientFactory(ISourceSchemaClient client) : ISourceSchemaClientFactory
{
    public bool CanHandle(ISourceSchemaClientConfiguration configuration)
        => configuration is PolicyTestClientConfiguration;

    public ISourceSchemaClient CreateClient(
        FusionSchemaDefinition schema,
        ISourceSchemaClientConfiguration configuration)
        => client;
}
