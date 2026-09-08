using System.Collections.Immutable;
using System.Reflection;
using System.Text;
using ChilliCream.Regorus;
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

    // Only cares about data.p.v, so it can prove which of a provider's snapshots is actually in
    // effect regardless of whatever the FAR data document currently contains.
    private const string PGatedPolicy =
        """
        package p1
        import rego.v1

        default allow := false
        allow if { data.p.v == 1 }
        """;

    // Only cares about data.one.n, so a two-provider scenario can prove that the served set picks
    // up a change from an unrelated provider even while a different provider's collision keeps a
    // FAR candidate pending.
    private const string OneGatedPolicy =
        """
        package p1
        import rego.v1

        default allow := false
        allow if { data.one.n == 2 }
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

    [Fact]
    public async Task Merge_Should_DropCandidate_When_ItCollidesAndFailsToCompile()
    {
        // arrange
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        var aggregator = CreateAggregator(provider);
        var diagnostics = new TestDiagnosticEvents();
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));
        var lastGood = observer.Current("p1.allow")!;

        // act: a candidate that is BOTH broken (cannot compile) AND, if it could compile, would
        // collide with the provider's data. The ORDER fix (compile-check before merge) means rule
        // 1 (drop + diagnostic) always wins: it must never get stuck pending on the collision
        // instead, waiting for a provider-side fix that could never resolve a compile error.
        policyProvider.OnNext(Snapshot(BrokenPolicy, farData: """{"feature":{}}""", digest: "d2"));

        // assert
        Assert.Same(lastGood, observer.Current("p1.allow"));
        Assert.True(diagnostics.UpdateErrors.Count + diagnostics.CompilationErrors.Count > 0);
        Assert.DoesNotContain(diagnostics.UpdateErrors, static e => e is RegoDataMergeException);
    }

    [Fact]
    public async Task Merge_Should_RecompileLastGoodFarContent_When_UnrelatedProviderRefreshesWhileCandidatePending()
    {
        // arrange: provider "one" feeds the served policy's own data key, provider "two" only
        // exists to collide with a pending FAR candidate so it never resolves during this test.
        var providerOne = new InMemoryRegoDataProvider("""{"one":{"n":1}}""");
        var providerTwo = new InMemoryRegoDataProvider("""{"two":{"x":1}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(
            [
                new RegoDataProviderRegistration("one", _ => providerOne, ownsInstance: false),
                new RegoDataProviderRegistration("two", _ => providerTwo, ownsInstance: false)
            ],
            diagnostics);
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(OneGatedPolicy));
        var lastGood = observer.Current("p1.allow")!;

        // act: a FAR candidate that collides with provider "two" is kept pending; it has nothing
        // to do with provider "one".
        policyProvider.OnNext(Snapshot(OneGatedPolicy, farData: """{"two":{}}""", digest: "d2"));
        var errorsAfterCollision = diagnostics.UpdateErrors.Count;
        Assert.True(errorsAfterCollision > 0);

        // act: the UNRELATED provider "one" refreshes. F2 fix: the currently served last-good FAR
        // content is recompiled against the new data even though a different candidate is still
        // stuck pending on provider "two" (which is retried separately and fails again).
        providerOne.Publish("""{"one":{"n":2}}""", "v2");

        // assert
        var resolved = observer.Current("p1.allow")!;
        Assert.NotSame(lastGood, resolved);
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await resolved.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);
        Assert.Equal(errorsAfterCollision + 1, diagnostics.UpdateErrors.Count);
    }

    [Fact]
    public async Task Merge_Should_ReportSingleError_When_ProviderCollidesWithFarDataAndHasNoLastGood()
    {
        // arrange: the provider's very first (and, for this test, only) response already collides
        // with the FAR data document that arrives right after, so it never gets a chance to become
        // this provider's last-good snapshot.
        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(provider, diagnostics);

        // act
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy, farData: """{"feature":{"enabled":false}}"""));

        // assert: without a last-good to fall back to, the collision is discovered only once, in
        // the final merge attempt - not once there AND again inside the per-provider reconcile
        // pass (F3). It is also attributed to the provider (F1m), not surfaced as a bare,
        // unattributed merge exception.
        Assert.Null(observer.Current("p1.allow"));
        var reported = Assert.Single(diagnostics.UpdateErrors);
        var providerError = Assert.IsType<RegoDataProviderException>(reported);
        Assert.Equal("feature", providerError.ProviderName);
    }

    [Fact]
    public async Task Invariant_Should_KeepProviderLastGood_When_BrokenCandidateArrivesDuringCollision()
    {
        // arrange: the reviewer's permanent interleaving probe (ruling 732). The policy only cares
        // about the provider's own key, so it can prove whether the TRUE last-good provider
        // snapshot ({p: v=1}) or a wrongly promoted colliding one is actually in effect.
        var provider = new InMemoryRegoDataProvider("""{"p":{"v":1}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(
            [new RegoDataProviderRegistration("p", _ => provider, ownsInstance: false)],
            diagnostics);
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(PGatedPolicy, farData: """{"feature":{"enabled":true}}"""));

        // act: the provider publishes data that collides with the committed FAR data's top-level
        // "feature" key. It is kept pending; "p"'s true last-good ({v: 1}) keeps serving.
        provider.Publish("""{"feature":{"enabled":false}}""", "v2");
        Assert.Single(diagnostics.UpdateErrors);

        // act: a FAR candidate whose data is unrelated ("{}") but whose CODE fails to compile
        // arrives. TRANSACTIONAL COMMIT (F1 fix): merging this candidate's data must never be
        // allowed to promote the provider's still-pending, still-colliding candidate as a side
        // effect - the candidate is dropped by the compile precheck before any merge is ever
        // attempted against it.
        policyProvider.OnNext(Snapshot(BrokenPolicy, farData: "{}", digest: "dBroken"));
        var errorsAfterBroken = diagnostics.UpdateErrors.Count + diagnostics.CompilationErrors.Count;
        Assert.True(errorsAfterBroken > 1);
        var beforeRebuild = observer.Current("p1.allow")!;

        // act: a further FAR publish forces another rebuild against the STILL-committed FAR data
        // ({"feature":{"enabled":true}}, plus a harmless new "marker" key). If the provider's
        // snapshot had been wrongly promoted to the colliding {"feature":{"enabled":false}} value,
        // this would now collide with the FAR data forever; with the fix, "p"'s true last-good
        // ({v: 1}) never collided with "feature" at all, so this must succeed.
        policyProvider.OnNext(
            Snapshot(PGatedPolicy, farData: """{"feature":{"enabled":true},"marker":{"m":1}}"""));

        // assert
        var resolved = observer.Current("p1.allow")!;
        Assert.NotSame(beforeRebuild, resolved);
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await resolved.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);
        Assert.Equal(errorsAfterBroken + 1, diagnostics.UpdateErrors.Count + diagnostics.CompilationErrors.Count);
    }

    [Fact]
    public async Task Rebuild_Should_DisposePrecheckCompiledSet_When_ADataAggregatorIsWired()
    {
        // arrange: a compiler stand-in that records every CompiledPolicySet it produces, so the
        // test can prove the precheck's set - which is never served - is disposed rather than
        // leaked (F3m), while the set that IS served stays usable.
        var compiled = new List<CompiledPolicySet>();

        CompiledPolicySet CountingCompiler(
            byte[] data, IReadOnlyList<PolicyModule> modules, IReadOnlyList<string> entryPoints)
        {
            var set = CompiledPolicySet.Compile(data, modules, entryPoints);
            compiled.Add(set);
            return set;
        }

        var provider = new InMemoryRegoDataProvider("""{"feature":{"enabled":true}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(provider, diagnostics);
        await using var policyProvider = new RegoPolicyProvider(diagnostics, aggregator, CountingCompiler);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);

        // act: with a data aggregator wired up, every FAR publish precheck-compiles the
        // candidate against the last-good data before ever attempting the merge - producing a
        // set that is never served.
        policyProvider.OnNext(Snapshot(FeatureGatedPolicy));

        // assert: the precheck compile ran first and its set was disposed - evaluating it now
        // throws - while the served (second, final) compile stays usable.
        Assert.Equal(2, compiled.Count);
        var precheckIndex = compiled[0].GetEntryPointIndex("data.p1.allow");
        Assert.Throws<ObjectDisposedException>(() => compiled[0].EvalBooleanWithInput(precheckIndex, "{}"u8));
        var servedIndex = compiled[1].GetEntryPointIndex("data.p1.allow");
        var servedResult = compiled[1].EvalBooleanWithInput(servedIndex, "{}"u8);
        Assert.False(servedResult.IsUndefined);
    }

    [Fact]
    public async Task Merge_Should_DiscardAttemptAndKeepProviderLastGood_When_CandidateCompilesOnLastGoodButNotMergedData()
    {
        // arrange: a compiler stand-in that fails only when given data carrying a "poison" key -
        // the real Regorus compiler cannot be made to fail based on data content alone, so this
        // is the only way to exercise the "compiled fine against the last-good data but not the
        // actual merged data" attempt-discard branch directly (F4m).
        CompiledPolicySet PoisonSensitiveCompiler(
            byte[] data, IReadOnlyList<PolicyModule> modules, IReadOnlyList<string> entryPoints)
        {
            if (Encoding.UTF8.GetString(data).Contains("poison", StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Simulated compile failure: data is poisoned.");
            }

            return CompiledPolicySet.Compile(data, modules, entryPoints);
        }

        const string zGatedPolicy =
            """
            package p1
            import rego.v1

            default allow := false
            allow if { data.other.z == 1 }
            """;

        var provider = new InMemoryRegoDataProvider("""{"other":{"z":1}}""");
        var diagnostics = new TestDiagnosticEvents();
        var aggregator = CreateAggregator(provider, diagnostics);
        await using var policyProvider =
            new RegoPolicyProvider(diagnostics, aggregator, PoisonSensitiveCompiler);
        var observer = new CapturingObserver();
        using var subscription = policyProvider.Subscribe(observer);
        policyProvider.OnNext(Snapshot(zGatedPolicy));
        var lastGood = observer.Current("p1.allow")!;

        // act: the provider republishes data that both changes its own value AND introduces the
        // poison key - its own recompile of the last-good content fails, so the poisoned
        // candidate is never promoted. A further FAR publish then compiles fine against the
        // (still unpoisoned) last-good data (the precheck) but the merge attempt - which
        // reconciles the still-pending poisoned provider snapshot - fails to compile too.
        provider.Publish("""{"other":{"z":2},"poison":true}""", "v2");
        var errorsAfterProviderRefresh = diagnostics.UpdateErrors.Count + diagnostics.CompilationErrors.Count;
        Assert.True(errorsAfterProviderRefresh > 0);

        policyProvider.OnNext(Snapshot(zGatedPolicy, digest: "d2"));

        // assert: neither failed attempt ever committed - the served policy is still the exact
        // instance compiled against the provider's original (z == 1) data, proving the
        // provider's last-good snapshot was never overwritten by the poisoned candidate.
        Assert.Same(lastGood, observer.Current("p1.allow"));
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await lastGood.EvaluateAsync(context, TestContext.Current.CancellationToken);
        Assert.Empty(context.DeniedIndices);
        Assert.True(
            diagnostics.UpdateErrors.Count + diagnostics.CompilationErrors.Count > errorsAfterProviderRefresh);

        // assert (direct, via reflection - RegoDataAggregator exposes no other way to observe a
        // provider's committed snapshot): the provider's own last-good is still its original "v1"
        // value, never promoted to the poisoned "v2" candidate by either failed attempt.
        Assert.Equal("v1", GetProviderSnapshotVersion(aggregator));
    }

    [Fact]
    public async Task Merge_Should_NotRecompileOrEmit_When_CollidingProviderRefreshLeavesServedCombinationUnchanged()
    {
        // arrange: two providers, each contributing a distinct top-level key.
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
        policyProvider.OnNext(Snapshot(AGatedPolicy));
        var beforeCollision = observer.Current("p1.allow")!;
        var updatesBeforeCollision = observer.Updates.Count;

        // act: provider "b" republishes data that collides with provider "a"'s own top-level
        // key. The candidate is rejected and "b"'s last-good stays in use, so the effective
        // served combination (FAR + "a" + "b") never actually changes (F5m).
        providerB.Publish("""{"a":{"conflict":true}}""", "v2");

        // assert: no recompile, no republish - the served set is the exact same instance.
        Assert.NotEmpty(diagnostics.UpdateErrors);
        Assert.Equal(updatesBeforeCollision, observer.Updates.Count);
        Assert.Same(beforeCollision, observer.Current("p1.allow"));
    }

    // Reflection is the only way to observe a provider's committed snapshot: RegoDataAggregator
    // deliberately exposes nothing about ProviderState beyond what TryBuildMergedData's own
    // return value already reveals.
    private static string GetProviderSnapshotVersion(RegoDataAggregator aggregator)
    {
        var providersField = typeof(RegoDataAggregator)
            .GetField("_providers", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var providerState = ((Array)providersField.GetValue(aggregator)!).GetValue(0)!;
        var snapshotProperty = providerState.GetType()
            .GetProperty("Snapshot", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var snapshot = (RegoDataSnapshot)snapshotProperty.GetValue(providerState)!;
        return snapshot.Version;
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
            ImmutableArray<PolicyLibraryModule>.Empty,
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
