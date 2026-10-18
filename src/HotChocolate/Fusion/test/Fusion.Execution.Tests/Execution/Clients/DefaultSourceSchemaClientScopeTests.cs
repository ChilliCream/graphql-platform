using System.Collections.Immutable;
using HotChocolate.Features;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Execution.Clients;

public sealed class DefaultSourceSchemaClientScopeTests : FusionTestBase
{
    private static readonly Uri s_baseAddress = new("http://localhost:5000/graphql");

    [Fact]
    public async Task ResetAsync_Should_KeepHttpClient_When_DefaultHttpClientFactoryIsUsed()
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
        Assert.Same(first, second);
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

    [Fact]
    public async Task ResetAsync_Should_KeepClient_When_FactoryIsReusableAndMaxAgeIsNotReached()
    {
        // arrange
        var factory = new TestClientFactory(TimeSpan.FromMinutes(1));
        await using var scope = CreateScope(factory);
        var first = scope.GetClient("A", OperationType.Query);

        // act
        await scope.ResetAsync();
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.Same(first, second);
        Assert.False(((TestClient)first).IsDisposed);
    }

    [Fact]
    public async Task ResetAsync_Should_DisposeClient_When_FactoryIsNotReusable()
    {
        // arrange
        var factory = new TestClientFactory(maxClientAge: null);
        await using var scope = CreateScope(factory);
        var first = scope.GetClient("A", OperationType.Query);

        // act
        await scope.ResetAsync();
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.NotSame(first, second);
        Assert.True(((TestClient)first).IsDisposed);
    }

    [Fact]
    public async Task ResetAsync_Should_DisposeClient_When_MaxAgeIsReached()
    {
        // arrange
        var factory = new TestClientFactory(TimeSpan.FromTicks(1));
        await using var scope = CreateScope(factory);
        var first = scope.GetClient("A", OperationType.Query);
        await Task.Delay(10, TestContext.Current.CancellationToken);

        // act
        await scope.ResetAsync();
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.NotSame(first, second);
        Assert.True(((TestClient)first).IsDisposed);
    }

    [Fact]
    public async Task RetireExpiredClients_Should_ReplaceClientAndDisposeItOnReset_When_MaxAgeIsReached()
    {
        // arrange
        var factory = new TestClientFactory(TimeSpan.FromTicks(1));
        await using var scope = CreateScope(factory);
        var first = (TestClient)scope.GetClient("A", OperationType.Query);
        await Task.Delay(10, TestContext.Current.CancellationToken);

        // act
        scope.RetireExpiredClients();
        var second = scope.GetClient("A", OperationType.Query);
        var disposedBeforeReset = first.IsDisposed;
        await scope.ResetAsync();

        // assert
        Assert.NotSame(first, second);
        Assert.False(disposedBeforeReset);
        Assert.True(first.IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeRetainedClients_When_ScopeIsDisposed()
    {
        // arrange
        var factory = new TestClientFactory(TimeSpan.FromMinutes(1));
        var scope = CreateScope(factory);
        var client = scope.GetClient("A", OperationType.Query);
        await scope.ResetAsync();

        // act
        await scope.DisposeAsync();

        // assert
        Assert.True(((TestClient)client).IsDisposed);
    }

    [Fact]
    public void GetMaxClientAge_Should_StayBelowHandlerLifetime_When_HttpClientIsConfigured()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient("default");
        services.AddHttpClient("short").SetHandlerLifetime(TimeSpan.FromSeconds(20));
        services.AddHttpClient("infinite").SetHandlerLifetime(Timeout.InfiniteTimeSpan);
        services.AddHttpClient("callback", c => c.Timeout = TimeSpan.FromSeconds(5));
        using var provider = services.BuildServiceProvider();
        var factory = CreateHttpClientFactory(provider);

        // act
        var maxAges = new[] { "default", "short", "infinite", "callback" }
            .Select(name => $"{name}: {factory.GetMaxClientAge(CreateConfiguration("A", name))}");

        // assert
        maxAges.MatchInlineSnapshots(
        [
            "default: 00:00:30",
            "short: 00:00:05",
            "infinite: 00:00:30",
            "callback: 00:00:00"
        ]);
    }

    private static HttpSourceSchemaClientFactory CreateHttpClientFactory(IServiceProvider services)
        => new(
            services.GetRequiredService<IHttpClientFactory>(),
            services.GetRequiredService<IOptionsMonitor<HttpClientFactoryOptions>>());

    private static DefaultSourceSchemaClientScope CreateScope(IServiceProvider services)
        => CreateScope(CreateHttpClientFactory(services));

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

    private sealed class TestClientFactory(TimeSpan? maxClientAge)
        : ISourceSchemaClientFactory
        , IReusableSourceSchemaClientFactory
    {
        public bool CanHandle(ISourceSchemaClientConfiguration configuration) => true;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => new TestClient();

        public TimeSpan GetMaxClientAge(ISourceSchemaClientConfiguration configuration)
            => maxClientAge ?? TimeSpan.Zero;
    }

    private sealed class TestClient : ISourceSchemaClient
    {
        public bool IsDisposed { get; private set; }

        public SourceSchemaClientCapabilities Capabilities => SourceSchemaClientCapabilities.Default;

        public IAsyncEnumerable<SourceSchemaResult> ExecuteAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaBatchResult> ExecuteBatchAsync(
            OperationPlanContext context,
            ImmutableArray<SourceSchemaClientRequest> requests,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public IAsyncEnumerable<SourceSchemaResult> SubscribeAsync(
            OperationPlanContext context,
            SourceSchemaClientRequest request,
            CancellationToken cancellationToken)
            => throw new NotSupportedException();

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
