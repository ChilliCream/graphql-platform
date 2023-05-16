namespace HotChocolate.Fusion.Clients;

internal sealed class GraphQLClientFactory
{
    private readonly Dictionary<string, Func<IGraphQLClient>> _clients;
    private readonly Dictionary<string, Func<IGraphQLSubscriptionClient>> _subscriptionClients;

    public GraphQLClientFactory(
        Dictionary<string, Func<IGraphQLClient>> clients,
        Dictionary<string, Func<IGraphQLSubscriptionClient>> subscriptionClients)
    {
        _clients = clients;
        _subscriptionClients = subscriptionClients;
    }

    public IGraphQLClient CreateClient(string subgraphName)
        => _clients[subgraphName]();

    public IGraphQLSubscriptionClient CreateSubscriptionClient(string subgraphName)
        => _subscriptionClients[subgraphName]();
}
