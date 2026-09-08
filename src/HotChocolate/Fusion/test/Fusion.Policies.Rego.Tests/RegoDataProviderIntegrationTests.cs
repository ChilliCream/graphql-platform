using System.Collections.Immutable;
using System.Text;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoDataProviderIntegrationTests
{
    private const string FeatureGatedPolicy =
        """
        package p1
        import rego.v1

        default allow := false
        allow if { data.feature.enabled }
        """;

    [Fact]
    public async Task Startup_Should_StayUnavailable_When_ProviderHasNotCompletedInitialLoad()
    {
        // arrange
        var gate = new TaskCompletionSource<RegoDataSnapshot>();
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":false}}""");
        provider.Handler = _ => new ValueTask<RegoDataSnapshot>(gate.Task);
        var aggregator = CreateAggregator(provider);
        await using var policyProvider = new RegoPolicyProvider(new TestDiagnosticEvents(), aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);

        // act
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));

        // assert
        // The provider has not completed its first load yet: the policy set stays on the existing
        // "no data yet" path (nothing published), matching a fail-closed unavailable state.
        Assert.Single(observer.Updates);
        Assert.Null(observer.Current("p1.allow"));
    }

    [Fact]
    public async Task Startup_Should_PublishPolicies_When_ProviderLoadCompletesAfterFarContentArrives()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        var aggregator = CreateAggregator(provider);
        await using var policyProvider = new RegoPolicyProvider(new TestDiagnosticEvents(), aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);

        // act
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));

        // assert
        Assert.NotNull(observer.Current("p1.allow"));
    }

    [Fact]
    public async Task Refresh_Should_RecompileAndPublish_When_ProviderDataChangesAfterStartup()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":false}}""");
        var aggregator = CreateAggregator(provider);
        await using var policyProvider = new RegoPolicyProvider(new TestDiagnosticEvents(), aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));
        var initial = observer.Current("p1.allow")!;

        var updatesBeforeRefresh = observer.Updates.Count;

        // act
        provider.Publish("""{"feature":{"enabled":true}}""", "v2");

        // assert
        Assert.Equal(updatesBeforeRefresh + 1, observer.Updates.Count);
        var updated = observer.Current("p1.allow")!;
        Assert.NotSame(initial, updated);

        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await updated.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);
    }

    [Fact]
    public async Task Pinning_Should_KeepPinnedPolicyBlocked_When_ProviderUpdatesDataMidRequest()
    {
        // arrange: the policy is compiled against feature.enabled = false, so it denies.
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":false}}""");
        var aggregator = CreateAggregator(provider);
        await using var policyProvider = new RegoPolicyProvider(new TestDiagnosticEvents(), aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));

        // A request captures the policy reference before the provider update: this is the
        // pinning point a request holds for its entire lifetime.
        var pinnedPolicy = observer.Current("p1.allow")!;
        var pinnedContext = new RegoPolicyTestEntities.TestPolicyContext(
            entities: new CompositeResultElement[1]);
        await pinnedPolicy.EvaluateAsync(pinnedContext, TestContext.Current.CancellationToken);
        Assert.Single(pinnedContext.DeniedIndices);

        // act: the provider now flips to enabled = true and republishes mid-flight.
        provider.Publish("""{"feature":{"enabled":true}}""", "v2");

        // assert: re-evaluating the SAME pinned reference still sees the data it was compiled
        // with, not the provider's new data - the request's snapshot never moves under it.
        var replayContext = new RegoPolicyTestEntities.TestPolicyContext(
            entities: new CompositeResultElement[1]);
        await pinnedPolicy.EvaluateAsync(replayContext, TestContext.Current.CancellationToken);
        Assert.Single(replayContext.DeniedIndices);

        // A fresh lookup after the update sees the new data and allows.
        var freshPolicy = observer.Current("p1.allow")!;
        Assert.NotSame(pinnedPolicy, freshPolicy);
        var freshContext = new RegoPolicyTestEntities.TestPolicyContext(
            entities: new CompositeResultElement[1]);
        await freshPolicy.EvaluateAsync(freshContext, TestContext.Current.CancellationToken);
        Assert.Empty(freshContext.DeniedIndices);
    }

    [Fact]
    public async Task Merge_Should_KeepLastGoodAndReportError_When_ProviderDataCollidesWithFarData()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        var aggregator = CreateAggregator(provider);
        var diagnostics = new TestDiagnosticEvents();
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));
        var beforeCollision = observer.Current("p1.allow");
        Assert.NotNull(beforeCollision);

        // act: a FAR data document that collides with the provider's top-level key.
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy, farData: """{"feature":{}}"""));

        // assert
        Assert.NotEmpty(diagnostics.UpdateErrors);
        Assert.Same(beforeCollision, observer.Current("p1.allow"));
    }

    private static RegoDataAggregator CreateAggregator(IRegoDataProvider provider)
    {
        var aggregator = new RegoDataAggregator(
            [new RegoDataProviderRegistration("feature", _ => provider, ownsInstance: false)],
            EmptyServiceProvider.Instance,
            new TestDiagnosticEvents(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        aggregator.Start();
        return aggregator;
    }

    private static PolicyContentSnapshot Snapshot(string source, string farData = "{}")
        => new(
            "rego",
            new Version(1, 0, 0),
            ImmutableArray.Create(
                new PolicyContent(
                    "p1",
                    PolicyContentType.Rego,
                    Encoding.UTF8.GetBytes(source),
                    PolicyRequirements.Empty,
                    "d1"u8.ToArray())),
            Encoding.UTF8.GetBytes(farData),
            "far-digest"u8.ToArray(),
            dataOwner: null);

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public static readonly EmptyServiceProvider Instance = new();

        public object? GetService(Type serviceType) => null;
    }

    private sealed class CapturingObserver : IObserver<ImmutableArray<IPolicy>>
    {
        private ImmutableArray<IPolicy> _current = [];

        public List<ImmutableArray<IPolicy>> Updates { get; } = [];

        public IPolicy? Current(string name)
            => _current.FirstOrDefault(p => p.Name.Equals(name, StringComparison.Ordinal));

        public void OnNext(ImmutableArray<IPolicy> value)
        {
            Updates.Add(value);
            _current = value;
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
