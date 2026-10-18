using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class DefaultSourceSchemaClientScopeTests : FusionTestBase
{
    private static readonly Uri s_baseAddress = new("http://localhost:5000/graphql");

    [Fact]
    public async Task ResetAsync_Should_CreateNewHttpClient_When_DefaultHttpClientFactoryIsUsed()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient("fusion");
        await using var provider = services.BuildServiceProvider();
        await using var scope = CreateScope(provider);
        var first = scope.GetClient("A", OperationType.Query);

        // act
        await scope.ResetAsync();
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task ResetAsync_Should_CreateNewHttpClient_When_HttpClientHasConfigureCallback()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient("fusion", c => c.Timeout = TimeSpan.FromSeconds(5));
        await using var provider = services.BuildServiceProvider();
        await using var scope = CreateScope(provider);
        var first = scope.GetClient("A", OperationType.Query);

        // act
        await scope.ResetAsync();
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.NotSame(first, second);
    }

    [Fact]
    public async Task ResetAsync_Should_CreateNewHttpClient_When_CustomHttpClientFactoryIsUsed()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient("fusion");
        services.AddSingleton<IHttpClientFactory, TestHttpClientFactory>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = CreateScope(provider);
        var first = scope.GetClient("A", OperationType.Query);

        // act
        await scope.ResetAsync();
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.NotSame(first, second);
    }

    private static DefaultSourceSchemaClientScope CreateScope(IServiceProvider services)
        => CreateScope(new HttpSourceSchemaClientFactory(services.GetRequiredService<IHttpClientFactory>()));

    private static DefaultSourceSchemaClientScope CreateScope(ISourceSchemaClientFactory factory)
    {
        var features = new FeatureCollection();
        features.Set(new FusionRequestOptions());
        features.Set(new SourceSchemaClientConfigurations([CreateConfiguration("A", "fusion")]));
        var schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql")),
            features: features);
        return new DefaultSourceSchemaClientScope(schema, [factory]);
    }

    private static HttpSourceSchemaClientConfiguration CreateConfiguration(string name, string httpClientName)
        => new(name, httpClientName, s_baseAddress);

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
