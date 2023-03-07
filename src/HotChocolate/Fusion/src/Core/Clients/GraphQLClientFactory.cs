namespace HotChocolate.Fusion.Clients;

internal sealed class GraphQLClientFactory
{
    private readonly Dictionary<string, Func<IGraphQLClient>> _clientFactories;

    public GraphQLClientFactory(Dictionary<string, Func<IGraphQLClient>> clientFactories)
    {
        _clientFactories = clientFactories;
    }

    public IGraphQLClient Create(string subgraphName)
        => _clientFactories[subgraphName]();
}
