using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    public async Task GetClient_Should_ReturnSameHttpClient_When_ScopesShareFactory()
    {
        // arrange
        var services = new ServiceCollection();
        services.AddHttpClient("fusion");
        await using var provider = services.BuildServiceProvider();
        var factory = CreateHttpClientFactory(provider);
        var schema = CreateSchema();
        await using var firstScope = new DefaultSourceSchemaClientScope(schema, [factory]);
        await using var secondScope = new DefaultSourceSchemaClientScope(schema, [factory]);

        // act
        var first = firstScope.GetClient("A", OperationType.Query);
        var second = secondScope.GetClient("A", OperationType.Query);

        // assert
        Assert.Same(first, second);
    }

    [Fact]
    public async Task ResetAsync_Should_KeepClient_When_SharedClientHasNotExpired()
    {
        // arrange
        var factory = new TestClientFactory(shareClients: true);
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
    public async Task ResetAsync_Should_DisposeClient_When_FactoryDoesNotShareClients()
    {
        // arrange
        var factory = new TestClientFactory(shareClients: false);
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
    public async Task GetClient_Should_ReplaceClientWithoutDisposingIt_When_SharedClientExpiredBeforeRequest()
    {
        // arrange
        var factory = new TestClientFactory(shareClients: true, sharedClientExpiresAt: 100);
        await using var scope = CreateScope(factory);
        scope.BeginRequest(50);
        var first = scope.GetClient("A", OperationType.Query);
        await scope.ResetAsync();
        factory.ReplaceSharedClient(sharedClientExpiresAt: 200);

        // act
        scope.BeginRequest(100);
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.NotSame(first, second);
        Assert.False(((TestClient)first).IsDisposed);
    }

    [Fact]
    public async Task GetClient_Should_ReplaceClientWithoutDisposingIt_When_SharedClientIsInvalidated()
    {
        // arrange
        var factory = new TestClientFactory(shareClients: true);
        await using var scope = CreateScope(factory);
        var first = scope.GetClient("A", OperationType.Query);
        factory.InvalidateSharedClient();

        // act
        var second = scope.GetClient("A", OperationType.Query);

        // assert
        Assert.NotSame(first, second);
        Assert.False(((TestClient)first).IsDisposed);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeOnlyOwnClients_When_ScopeIsDisposed()
    {
        // arrange
        var sharedScope = CreateScope(new TestClientFactory(shareClients: true));
        var ownScope = CreateScope(new TestClientFactory(shareClients: false));
        var sharedClient = sharedScope.GetClient("A", OperationType.Query);
        var ownClient = ownScope.GetClient("A", OperationType.Query);

        // act
        await sharedScope.DisposeAsync();
        await ownScope.DisposeAsync();

        // assert
        Assert.False(((TestClient)sharedClient).IsDisposed);
        Assert.True(((TestClient)ownClient).IsDisposed);
    }

    [Fact]
    public void GetMaxClientAge_Should_NotExceedHandlerLifetime_When_HttpClientIsConfigured()
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
            "default: 00:02:00",
            "short: 00:00:20",
            "infinite: 00:02:00",
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
        => new(CreateSchema(), [factory]);

    private static FusionSchemaDefinition CreateSchema()
    {
        var features = new FeatureCollection();
        features.Set(new FusionRequestOptions());
        features.Set(new SourceSchemaClientConfigurations([CreateConfiguration("A", "fusion")]));
        return FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(FileResource.Open("fusion1.graphql")),
            features: features);
    }

    private static HttpSourceSchemaClientConfiguration CreateConfiguration(string name, string httpClientName)
        => new(name, httpClientName, s_baseAddress);

    private sealed class TestHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }

    private sealed class TestClientFactory(bool shareClients, long sharedClientExpiresAt = long.MaxValue)
        : ISourceSchemaClientFactory
        , ISharedSourceSchemaClientFactory
    {
        private SharedSourceSchemaClient _sharedClient = new(new TestClient(), sharedClientExpiresAt);

        public bool CanHandle(ISourceSchemaClientConfiguration configuration) => true;

        public ISourceSchemaClient CreateClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration)
            => new TestClient();

        public bool TryGetSharedClient(
            FusionSchemaDefinition schema,
            ISourceSchemaClientConfiguration configuration,
            [NotNullWhen(true)] out SharedSourceSchemaClient? client)
        {
            client = shareClients ? _sharedClient : null;
            return shareClients;
        }

        public void ReplaceSharedClient(long sharedClientExpiresAt)
            => _sharedClient = new SharedSourceSchemaClient(new TestClient(), sharedClientExpiresAt);

        public void InvalidateSharedClient()
        {
            _sharedClient.Invalidate();
            ReplaceSharedClient(long.MaxValue);
        }
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
