using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class PolicyCollectionTests : FusionTestBase
{
    [Fact]
    public void Get_Should_ReturnPolicy_When_NameMatchesExactly()
    {
        // arrange
        var policy = new TestPolicy("CanReadSecret");
        using var registry = new PolicyCollection(new TestPolicyProvider(policy));
        registry.Connect();

        // act & assert
        Assert.Same(policy, registry.Get("CanReadSecret"));
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_NameDiffersByCase()
    {
        // arrange
        using var registry = new PolicyCollection(
            new TestPolicyProvider(new TestPolicy("CanReadSecret")));
        registry.Connect();

        // act & assert
        Assert.False(registry.TryGet("canReadSecret", out _));
    }

    [Fact]
    public void Get_Should_ThrowFailClosed_When_PolicyNameIsUnknown()
    {
        // arrange
        using var registry = new PolicyCollection(
            new TestPolicyProvider(new TestPolicy("CanReadSecret")));
        registry.Connect();

        // act & assert
        Assert.Throws<KeyNotFoundException>(() => registry.Get("Unknown"));
    }

    [Fact]
    public void Connect_Should_Throw_When_PolicyNameIsEmpty()
    {
        // arrange
        using var registry = new PolicyCollection(
            new TestPolicyProvider(new TestPolicy(string.Empty)));

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => registry.Connect());

        // assert
        Assert.Equal("An authorization policy must have a name.", exception.Message);
    }

    [Fact]
    public void Connect_Should_ApplySnapshot_When_ProviderReplaysCurrentSet()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection(provider);

        // act
        registry.Connect();

        // assert
        Assert.True(registry.TryGet("A", out _));
    }

    [Fact]
    public void Apply_Should_ReplacePolicy_When_ProviderEmitsNewInstance()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection(provider);
        registry.Connect();
        var replacement = new TestPolicy("A");

        // act
        provider.Emit(replacement);

        // assert
        Assert.Same(replacement, registry.Get("A"));
    }

    [Fact]
    public void Apply_Should_RemovePolicy_When_ProviderEmitsEmptySnapshot()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection(provider);
        registry.Connect();

        // act
        provider.Emit();

        // assert
        Assert.False(registry.TryGet("A", out _));
    }

    [Fact]
    public void Apply_Should_KeepInstance_When_ProviderReEmitsSameInstance()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection(provider);
        registry.Connect();
        var current = registry.Get("A");

        // act
        provider.Emit(current);

        // assert
        Assert.Same(current, registry.Get("A"));
    }

    [Fact]
    public void Apply_Should_Throw_When_SnapshotDuplicatesAPolicyName()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection(provider);
        registry.Connect();

        // act
        // The provider republishes a complete snapshot that itself carries a duplicate name, which
        // the registry rejects out of the applying observer.
        var exception = Assert.Throws<InvalidOperationException>(
            () => provider.Emit(new TestPolicy("A"), new TestPolicy("A")));

        // assert
        Assert.Equal(
            "Authorization policy 'A' is registered more than once.",
            exception.Message);
    }

    [Fact]
    public async Task Apply_Should_EvictOnlyPlansReferencingChangedPolicy_When_OneRequirementChanges()
    {
        // arrange
        var provider = new TestPolicyProvider(
            new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            new TestPolicy("B", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => provider)
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(CreateTwoSecretsSchemaDocument(), services);
        var planCache = new OperationPlanCache(16, diagnostics: null);
        schema.Policies.AttachPlanCache(planCache);
        var session = planCache.Capture();
        var planA = PlanOperation(schema, "{ secretA }");
        var planB = PlanOperation(schema, "{ secretB }");
        planCache.Add(session, "planA", planA);
        planCache.Add(session, "planB", planB);

        // act
        // Only "A"'s resource requirement changes; "B" keeps the same requirement.
        provider.Emit(
            new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id ownerId }")),
            new TestPolicy("B", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

        // assert
        // Targeted eviction removes only the plan that referenced the changed policy, in the
        // same cache instance, instead of discarding every cached plan.
        Assert.Same(session.Cache, planCache.Current);
        Assert.False(session.Cache.TryGet("planA", out _));
        Assert.True(session.Cache.TryGet("planB", out _));
    }

    [Fact]
    public async Task Apply_Should_NotCachePlan_When_AddHappensAfterTargetedEviction()
    {
        // arrange
        var provider = new TestPolicyProvider(
            new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            new TestPolicy("B", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => provider)
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(CreateTwoSecretsSchemaDocument(), services);
        var planCache = new OperationPlanCache(16, diagnostics: null);
        schema.Policies.AttachPlanCache(planCache);

        // A request captures the session before planning, exactly like the middleware does, so
        // the plan below is built against the snapshot that is about to become stale.
        var session = planCache.Capture();
        var plan = PlanOperation(schema, "{ secretA }");

        // act
        // The policy's requirement changes while the plan above is still in flight, which runs
        // EvictPolicies (and bumps the cache's version) before the late Add below.
        provider.Emit(
            new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id ownerId }")),
            new TestPolicy("B", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));
        planCache.Add(session, "plan", plan);

        // assert
        // The version check in Add must reject the late insert instead of letting a plan built
        // for the old requirement land in the live cache.
        Assert.False(planCache.Current.TryGet("plan", out _));
    }

    [Fact]
    public async Task Apply_Should_EvictPlan_When_AnyReferencedPolicyChanges()
    {
        // arrange
        var provider = new TestPolicyProvider(
            new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            new TestPolicy("B", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => provider)
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(CreateTwoSecretsSchemaDocument(), services);
        var planCache = new OperationPlanCache(16, diagnostics: null);
        schema.Policies.AttachPlanCache(planCache);
        var session = planCache.Capture();

        // A single plan referencing both policies A and B.
        var plan = PlanOperation(schema, "{ secretA secretB }");
        planCache.Add(session, "plan", plan);

        // act
        // Only "B"'s resource requirement changes; "A" keeps the same requirement.
        provider.Emit(
            new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")),
            new TestPolicy("B", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id ownerId }")));

        // assert
        // The plan is evicted even though only one of the two policies it references changed.
        Assert.False(session.Cache.TryGet("plan", out _));
    }

    [Fact]
    public void Apply_Should_NotResetPlanCache_When_PolicyRequirementIsUnchanged()
    {
        // arrange
        var resource = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }");
        var provider = new TestPolicyProvider(new TestPolicy("A", resource));
        using var registry = new PolicyCollection(provider);
        registry.Connect();
        var planCache = new OperationPlanCache(16, diagnostics: null);
        registry.AttachPlanCache(planCache);
        var cacheBeforeChange = planCache.Current;

        // act
        // A new policy instance whose requirement selects the same fields as before must not evict
        // plans that are still valid against it.
        provider.Emit(new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

        // assert
        Assert.Same(cacheBeforeChange, planCache.Current);
    }

    [Fact]
    public void Apply_Should_ResetPlanCache_When_PolicyIsRemovedAndReAdded()
    {
        // arrange
        var provider = new TestPolicyProvider(
            new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));
        using var registry = new PolicyCollection(provider);
        registry.Connect();
        var planCache = new OperationPlanCache(16, diagnostics: null);
        registry.AttachPlanCache(planCache);

        // act
        // A plan that referenced "A" while it was absent skips the unknown name instead of
        // failing closed, so re-adding it with a requirement must still evict that plan even
        // though the removal already reset the cache once in between.
        provider.Emit();
        var cacheAfterRemoval = planCache.Current;
        provider.Emit(new TestPolicy("A", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id ownerId }")));

        // assert
        Assert.NotSame(cacheAfterRemoval, planCache.Current);
    }

    [Fact]
    public void Apply_Should_ResetPlanCache_When_PolicyIsAdded()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A", (SelectionSetNode?)null));
        using var registry = new PolicyCollection(provider);
        registry.Connect();
        var planCache = new OperationPlanCache(16, diagnostics: null);
        registry.AttachPlanCache(planCache);
        var cacheBeforeChange = planCache.Current;

        // act
        // A pure addition can already be referenced by a plan planned while the name was
        // missing, so it must reset the cache rather than being treated as a no-op.
        provider.Emit(
            new TestPolicy("A", (SelectionSetNode?)null),
            new TestPolicy("B", Utf8GraphQLParser.Syntax.ParseSelectionSet("{ id }")));

        // assert
        Assert.NotSame(cacheBeforeChange, planCache.Current);
    }

    [Fact]
    public async Task CreateSchema_Should_Throw_When_PolicyNameDiffersByCase()
    {
        // arrange
        await using var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(
                _ => new TestPolicyProvider(new TestPolicy("CanReadSecret")))
            .BuildServiceProvider();

        // act
        var exception = Assert.Throws<KeyNotFoundException>(
            () => FusionSchemaDefinition.Create(
                CreateSchemaDocument("canReadSecret"),
                services));

        // assert
        Assert.Equal(
            "Authorization policy 'canReadSecret' was not found.",
            exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeProviderAndPolicyOnce_When_SchemaIsDisposed()
    {
        // arrange
        var policy = new DisposablePolicy();
        var provider = new TestPolicyProvider(policy);
        var services = new ServiceCollection()
            .AddSingleton<IPolicyProvider>(_ => provider)
            .BuildServiceProvider();
        var schema = FusionSchemaDefinition.Create(
            CreateSchemaDocument(policy.Name),
            services);

        // act
        await schema.DisposeAsync();
        await schema.DisposeAsync();

        // assert
        Assert.Same(policy, schema.Policies.Get(policy.Name));
        Assert.True(provider.IsDisposed);
        Assert.Equal(1, policy.DisposeCalls);
    }

    private static DocumentNode CreateSchemaDocument(string policyName)
        => Utf8GraphQLParser.Parse(
            $$"""
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              secret: String
                @fusion__field(schema: A)
                @fusion__policy(names: "{{policyName}}")
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """);

    private static DocumentNode CreateTwoSecretsSchemaDocument()
        => Utf8GraphQLParser.Parse(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) {
              id: ID! @fusion__field(schema: A)
              ownerId: ID! @fusion__field(schema: A)
              secretA: String
                @fusion__field(schema: A)
                @fusion__policy(names: "A")
              secretB: String
                @fusion__field(schema: A)
                @fusion__policy(names: "B")
            }

            enum fusion__Schema {
              A @fusion__schema_metadata(name: "A")
            }
            """);

    private sealed class DisposablePolicy : IPolicy, IDisposable
    {
        public string Name => "CanReadSecret";

        public PolicyRequirements Requirements => PolicyRequirements.Empty;

        public int DisposeCalls { get; private set; }

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            CancellationToken cancellationToken)
            => ValueTask.CompletedTask;

        public void Dispose() => DisposeCalls++;
    }
}
