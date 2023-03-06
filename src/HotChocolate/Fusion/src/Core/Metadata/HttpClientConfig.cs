namespace HotChocolate.Fusion.Metadata;

internal sealed class HttpClientConfig
{
    public HttpClientConfig(string subgraph, Uri baseAddress)
    {
        Subgraph = subgraph;
        BaseAddress = baseAddress;
    }

    public string Subgraph { get; }

    public Uri BaseAddress { get; }
}
