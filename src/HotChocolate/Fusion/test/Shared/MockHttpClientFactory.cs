namespace HotChocolate.Fusion.Shared;

public sealed class MockHttpClientFactory : IHttpClientFactory
{
    private readonly Dictionary<string, Func<HttpClient>> _clients;

    public MockHttpClientFactory(Dictionary<string, Func<HttpClient>> clients)
        => _clients = clients;

    public HttpClient CreateClient(string name)
        => _clients[name].Invoke();
}
