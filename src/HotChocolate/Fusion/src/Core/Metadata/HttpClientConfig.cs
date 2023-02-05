namespace HotChocolate.Fusion.Metadata;

internal sealed class HttpClientConfig
{
    public HttpClientConfig(string schemaName, Uri baseAddress)
    {
        SchemaName = schemaName;
        BaseAddress = baseAddress;
    }

    public string SchemaName { get; }

    public Uri BaseAddress { get; }
}
