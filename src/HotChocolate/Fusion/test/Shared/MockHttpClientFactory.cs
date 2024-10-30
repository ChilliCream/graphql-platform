namespace HotChocolate.Fusion.Shared;

public class MockHttpClientFactory(
    Dictionary<string, Func<HttpClient>> clients)
    : IHttpClientFactory
{
    public HttpClient CreateClient(string name)
        => clients[name].Invoke();
}
