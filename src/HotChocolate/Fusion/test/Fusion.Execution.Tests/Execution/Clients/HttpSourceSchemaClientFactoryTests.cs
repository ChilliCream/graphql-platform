namespace HotChocolate.Fusion.Execution.Clients;

public sealed class HttpSourceSchemaClientFactoryTests
{
    [Theory]
    [InlineData(SourceSchemaClientCapabilities.AliasBatching, true)]
    [InlineData(SourceSchemaClientCapabilities.AliasBatching | SourceSchemaClientCapabilities.VariableBatching, true)]
    [InlineData(SourceSchemaClientCapabilities.AliasBatching | SourceSchemaClientCapabilities.RequestBatching, true)]
    [InlineData(
        SourceSchemaClientCapabilities.AliasBatching
        | SourceSchemaClientCapabilities.VariableBatching
        | SourceSchemaClientCapabilities.RequestBatching,
        false)]
    [InlineData(SourceSchemaClientCapabilities.VariableBatching | SourceSchemaClientCapabilities.RequestBatching, false)]
    public void GetAliasBatchQueryCache_Should_ReturnACache_When_ClientsCanSendAliasBatchedRequests(
        SourceSchemaClientCapabilities capabilities,
        bool expected)
    {
        // arrange
        using var factory = new HttpSourceSchemaClientFactory(new HttpClientFactory(), null);
        var configuration = CreateConfiguration(capabilities);

        // act
        var cache = factory.GetAliasBatchQueryCache(configuration);

        // assert
        Assert.Equal(expected, cache is not null);
    }

    [Fact]
    public void GetAliasBatchQueryCache_Should_ReturnOneCachePerConfiguration_When_CalledAgain()
    {
        // arrange
        using var factory = new HttpSourceSchemaClientFactory(new HttpClientFactory(), null);
        var configuration = CreateConfiguration(SourceSchemaClientCapabilities.AliasBatching);
        var otherConfiguration = CreateConfiguration(SourceSchemaClientCapabilities.AliasBatching);

        // act
        var first = factory.GetAliasBatchQueryCache(configuration);
        var second = factory.GetAliasBatchQueryCache(configuration);
        var other = factory.GetAliasBatchQueryCache(otherConfiguration);

        // assert
        Assert.Same(first, second);
        Assert.NotSame(first, other);
    }

    private static HttpSourceSchemaClientConfiguration CreateConfiguration(SourceSchemaClientCapabilities capabilities)
        => new("a", new Uri("http://localhost:5000/graphql"), capabilities: capabilities);

    private sealed class HttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
