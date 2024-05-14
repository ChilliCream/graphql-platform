namespace HotChocolate.Fusion.Metadata;

internal sealed class ServiceDiscoveryConfigurationRewriter : ConfigurationRewriter
{
    protected override ValueTask<HttpClientConfiguration> RewriteAsync(
        HttpClientConfiguration configuration,
        CancellationToken cancellationToken)
    {
        var x = configuration with
        {
            EndpointUri = new Uri($"http://{configuration.SubgraphName}/graphql"),
        };

        return new ValueTask<HttpClientConfiguration>(x);
    }
}
