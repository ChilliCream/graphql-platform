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

    // The rule body is malformed, so the whole set fails to compile.
    private const string BrokenPolicy =
        """
        package p1
        import rego.v1
        allow if {
        """;

    // Textually distinct from FeatureGatedPolicy (an extra "or" branch), so a compile against
    // this source is only ever mistaken for FeatureGatedPolicy if a stale candidate is used.
    private const string OverrideGatedPolicy =
        """
        package p1
        import rego.v1

        default allow := false
        allow if { data.feature.enabled }
        allow if { data.override.on }
        """;

    // Only cares about data.a.x, so a two-provider scenario can prove that a rejected candidate
    // from a different, colliding provider never affects the served result.
    private const string AGatedPolicy =
        """
        package p1
        import rego.v1

        default allow := false
        allow if { data.a.x == 1 }
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
        var gate = new TaskCompletionSource<RegoDataSnapshot>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        provider.Handler = _ => new ValueTask<RegoDataSnapshot>(gate.Task);
        var aggregator = CreateAggregator(provider);
        await using var policyProvider = new RegoPolicyProvider(new TestDiagnosticEvents(), aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));
        Assert.Null(observer.Current("p1.allow"));

        // act: the provider's initial load completes after the FAR content already arrived.
        gate.SetResult(new RegoDataSnapshot("""{"feature":{"enabled":true}}"""u8, "v1"));
        await WaitUntilAsync(() => observer.Current("p1.allow") is not null);

        // assert
        var policy = observer.Current("p1.allow")!;
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);
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

    [Fact]
    public async Task Refresh_Should_RecompileLastGoodContent_When_ProviderUpdatesAfterBrokenFarCandidate()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":false}}""");
        var aggregator = CreateAggregator(provider);
        var diagnostics = new TestDiagnosticEvents();
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));
        var lastGood = observer.Current("p1.allow")!;

        // act: a FAR update with the same policy name but a syntactically broken body arrives.
        policyProvider.OnNext(Snapshot(BrokenPolicy, digest: "d2"));
        var errorsAfterBrokenCandidate =
            diagnostics.UpdateErrors.Count + diagnostics.CompilationErrors.Count;

        // assert: the broken candidate is rejected, the last-good policy stays published.
        Assert.Same(lastGood, observer.Current("p1.allow"));
        Assert.True(errorsAfterBrokenCandidate > 0);

        // act: the provider then republishes fresh data; the last-good code must be recompiled
        // against it rather than the discarded broken candidate.
        provider.Publish("""{"feature":{"enabled":true}}""", "v2");

        // assert
        var updated = observer.Current("p1.allow")!;
        Assert.NotSame(lastGood, updated);
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await updated.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);
        Assert.Equal(
            errorsAfterBrokenCandidate,
            diagnostics.UpdateErrors.Count + diagnostics.CompilationErrors.Count);
    }

    [Fact]
    public async Task Startup_Should_StayUnavailableAndReportError_When_ProviderThrowsOnInitialLoad()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":false}}""");
        provider.Handler = _ => throw new InvalidOperationException("boom");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(provider, diagnostics);
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);

        // act
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));

        // assert: the provider's first load failed, so the policy set stays unavailable (fail
        // closed) and the failure is reported through the diagnostics contract.
        Assert.Null(observer.Current("p1.allow"));
        var reported = Assert.Single(diagnostics.UpdateErrors);
        var providerError = Assert.IsType<RegoDataProviderException>(reported);
        Assert.Equal("feature", providerError.ProviderName);
    }

    [Fact]
    public async Task Merge_Should_RecompileWithoutFarRepublish_When_ProviderDataStopsColliding()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        var aggregator = CreateAggregator(provider);
        var diagnostics = new TestDiagnosticEvents();
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));
        var beforeCollision = observer.Current("p1.allow")!;

        // act: a FAR data document that collides with the provider's top-level key is kept
        // pending rather than dropped.
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy, farData: """{"feature":{}}"""));
        Assert.Same(beforeCollision, observer.Current("p1.allow"));
        var errorsAfterCollision = diagnostics.UpdateErrors.Count;
        Assert.True(errorsAfterCollision > 0);

        // act: the provider republishes data that no longer defines the colliding "feature" key,
        // resolving the collision without the FAR content ever being republished.
        provider.Publish("""{"orders":{"open":true}}""", "v2");

        // assert: the pending FAR candidate is recompiled against the fixed provider data.
        var resolved = observer.Current("p1.allow")!;
        Assert.NotSame(beforeCollision, resolved);
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await resolved.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Single(context.DeniedIndices);
        Assert.Equal(errorsAfterCollision, diagnostics.UpdateErrors.Count);
    }

    [Fact]
    public async Task Merge_Should_Recompile_When_FarRepublishNoLongerCollides()
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

        // act: a FAR data document that collides with the provider's top-level key is kept
        // pending rather than dropped.
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy, farData: """{"feature":{}}""", digest: "d2"));
        Assert.NotEmpty(diagnostics.UpdateErrors);
        Assert.Same(beforeCollision, observer.Current("p1.allow"));

        // act: a fresh FAR publish that no longer collides replaces the pending candidate.
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy, farData: "{}", digest: "d3"));

        // assert
        var resolved = observer.Current("p1.allow")!;
        Assert.NotSame(beforeCollision, resolved);
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await resolved.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);
    }

    [Fact]
    public async Task Merge_Should_KeepProviderLastGood_When_ItsRefreshCollidesWithAnotherProvider()
    {
        // arrange: two providers, each contributing a distinct top-level key that does not
        // collide with the other or with the (empty) FAR data document.
        var providerA = new InMemoryRegoDataProvider("""{"a":{"x":1}}""");
        var providerB = new InMemoryRegoDataProvider("""{"b":{"y":1}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(
            [
                new RegoDataProviderRegistration("a", _ => providerA, ownsInstance: false),
                new RegoDataProviderRegistration("b", _ => providerB, ownsInstance: false)
            ],
            diagnostics);
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        // The initial data merges cleanly: provider "a" contributes x == 1, so the policy allows.
        policyProvider.OnNext(Snapshot(AGatedPolicy));

        // act: provider "a" republishes data that redefines its own key AND collides with
        // provider "b"'s top-level key.
        providerA.Publish("""{"a":{"x":2},"b":{"conflict":true}}""", "v2");

        // assert: the collision is reported against provider "a", and its last-good snapshot
        // (x == 1) stays in use - the whole colliding candidate is rejected, not merged partially.
        var reported = Assert.Single(diagnostics.UpdateErrors);
        Assert.Equal("a", ((RegoDataProviderException)reported).ProviderName);

        var afterCollision = observer.Current("p1.allow")!;
        var afterCollisionContext =
            new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await afterCollision.EvaluateAsync(afterCollisionContext, TestContext.Current.CancellationToken);
        Assert.Empty(afterCollisionContext.DeniedIndices);

        // act: provider "a" fixes its data so it no longer collides with provider "b".
        providerA.Publish("""{"a":{"x":2}}""", "v3");

        // assert: the new (non-colliding) data is now in effect.
        var resolved = observer.Current("p1.allow")!;
        var resolvedContext = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await resolved.EvaluateAsync(resolvedContext, TestContext.Current.CancellationToken);
        Assert.Single(resolvedContext.DeniedIndices);
        Assert.Single(diagnostics.UpdateErrors);
    }

    [Fact]
    public async Task Invariant_Should_ServeNewestCleanlyMergedCombination_When_FarAndProviderCandidatesInterleave()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        var aggregator = CreateAggregator(provider);
        var diagnostics = new TestDiagnosticEvents();
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);

        // V1: FAR is the feature-gated policy, the provider enables the feature.
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));
        var v1 = observer.Current("p1.allow")!;

        // act: a FAR candidate that fails to compile is dropped; the last-good (V1's code) stays
        // published.
        policyProvider.OnNext(Snapshot(BrokenPolicy, digest: "broken"));
        Assert.Same(v1, observer.Current("p1.allow"));

        // act: a provider refresh recompiles the LAST-GOOD FAR code against fresh data - never
        // the dropped broken candidate.
        provider.Publish("""{"feature":{"enabled":false}}""", "v2");
        var v2 = observer.Current("p1.allow")!;
        Assert.NotSame(v1, v2);

        // act: a new, textually distinct FAR candidate (adds an "override" rule) compiles cleanly
        // but its data collides with the provider's current top-level key; it is kept pending and
        // the served set stays on V2.
        policyProvider.OnNext(Snapshot(OverrideGatedPolicy, farData: """{"feature":{}}""", digest: "p2"));
        Assert.Same(v2, observer.Current("p1.allow"));

        // act: the provider then republishes data that no longer collides. The served set must
        // reflect the NEWEST valid combination - the pending "override" FAR candidate merged with
        // the provider's newest data - never V1's code (already superseded) or the provider's
        // earlier data.
        provider.Publish("""{"override":{"on":true}}""", "v3");

        // assert: a stale candidate (V1/V2's code, which has no "override" rule, or the provider's
        // earlier data, which would still collide with "feature") could never produce this result.
        var v3 = observer.Current("p1.allow")!;
        Assert.NotSame(v2, v3);
        var v3Context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await v3.EvaluateAsync(v3Context, TestContext.Current.CancellationToken);
        Assert.Empty(v3Context.DeniedIndices);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("The condition was not met in time.");
    }

    private static RegoDataAggregator CreateAggregator(
        IRegoDataProvider provider,
        TestDiagnosticEvents? diagnostics = null)
        => CreateAggregator(
            [new RegoDataProviderRegistration("feature", _ => provider, ownsInstance: false)],
            diagnostics);

    private static RegoDataAggregator CreateAggregator(
        IReadOnlyList<RegoDataProviderRegistration> registrations,
        TestDiagnosticEvents? diagnostics = null)
    {
        var aggregator = new RegoDataAggregator(
            registrations,
            EmptyServiceProvider.Instance,
            diagnostics ?? new TestDiagnosticEvents(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance);
        aggregator.Start();
        return aggregator;
    }

    private static PolicyContentSnapshot Snapshot(string source, string farData = "{}", string digest = "d1")
        => new(
            "rego",
            new Version(1, 0, 0),
            ImmutableArray.Create(
                new PolicyContent(
                    "p1",
                    PolicyContentType.Rego,
                    Encoding.UTF8.GetBytes(source),
                    PolicyRequirements.Empty,
                    Encoding.UTF8.GetBytes(digest))),
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
