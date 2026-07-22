using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class PolicyCollectionTests
{
    [Fact]
    public void Get_Should_ReturnPolicy_When_NameMatchesExactly()
    {
        // arrange
        var policy = new TestPolicy("CanReadSecret");
        using var registry = new PolicyCollection([new TestPolicyProvider(policy)]);
        registry.Connect();

        // act & assert
        Assert.Same(policy, registry.Get("CanReadSecret"));
    }

    [Fact]
    public void TryGet_Should_ReturnFalse_When_NameDiffersByCase()
    {
        // arrange
        using var registry = new PolicyCollection([new TestPolicyProvider(new TestPolicy("CanReadSecret"))]);
        registry.Connect();

        // act & assert
        Assert.False(registry.TryGet("canReadSecret", out _));
    }

    [Fact]
    public void Get_Should_ThrowFailClosed_When_PolicyNameIsUnknown()
    {
        // arrange
        using var registry = new PolicyCollection([new TestPolicyProvider(new TestPolicy("CanReadSecret"))]);
        registry.Connect();

        // act & assert
        Assert.Throws<KeyNotFoundException>(() => registry.Get("Unknown"));
    }

    [Fact]
    public void Connect_Should_Throw_When_PolicyNameIsEmpty()
    {
        // arrange
        using var registry = new PolicyCollection(
            [new TestPolicyProvider(new TestPolicy(string.Empty))]);

        // act
        var exception = Assert.Throws<InvalidOperationException>(() => registry.Connect());

        // assert
        Assert.Equal("An authorization policy must have a name.", exception.Message);
    }

    [Fact]
    public void Connect_Should_ApplyUpserts_When_ProviderReplaysCurrentSet()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection([provider]);

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
        using var registry = new PolicyCollection([provider]);
        registry.Connect();
        var replacement = new TestPolicy("A");

        // act
        provider.Emit(new PolicyUpdate("A", replacement));

        // assert
        Assert.Same(replacement, registry.Get("A"));
    }

    [Fact]
    public void Apply_Should_RemovePolicy_When_ProviderEmitsNull()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection([provider]);
        registry.Connect();

        // act
        provider.Emit(new PolicyUpdate("A", null));

        // assert
        Assert.False(registry.TryGet("A", out _));
    }

    [Fact]
    public void Apply_Should_KeepInstance_When_ProviderReEmitsSameInstance()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection([provider]);
        registry.Connect();
        var current = registry.Get("A");

        // act
        provider.Emit(new PolicyUpdate("A", current));

        // assert
        Assert.Same(current, registry.Get("A"));
    }

    [Fact]
    public void Connect_Should_Throw_When_ProvidersShareAPolicyName()
    {
        // arrange
        var first = new TestPolicyProvider(new TestPolicy("Shared"));
        var second = new TestPolicyProvider(new TestPolicy("Shared"));
        using var registry = new PolicyCollection([first, second]);

        // act
        var exception = Assert.Throws<InvalidOperationException>(registry.Connect);

        // assert
        Assert.Equal(
            "Authorization policy 'Shared' is registered more than once.",
            exception.Message);
    }

    [Fact]
    public void Apply_Should_Throw_When_LiveUpdateDuplicatesAnotherProvidersName()
    {
        // arrange
        var first = new TestPolicyProvider(new TestPolicy("A"));
        var second = new TestPolicyProvider(new TestPolicy("B"));
        using var registry = new PolicyCollection([first, second]);
        registry.Connect();

        // act
        // The second provider emits a name already owned by the first provider, which the registry
        // rejects out of the applying observer.
        var exception = Assert.Throws<InvalidOperationException>(
            () => second.Emit(new PolicyUpdate("A", new TestPolicy("A"))));

        // assert
        Assert.Equal(
            "Authorization policy 'A' is registered more than once.",
            exception.Message);
    }

    [Fact]
    public void Pin_Should_ReturnNull_When_NameIsUnknown()
    {
        // arrange
        var provider = new TestPolicyProvider(new TestPolicy("A"));
        using var registry = new PolicyCollection([provider]);
        registry.Connect();

        // act & assert
        Assert.Null(registry.Pin("Unknown"));
    }

    [Fact]
    public void Pin_Should_ResolveReplacement_When_InstanceIsRetiredBetweenLookupAndPin()
    {
        // arrange
        var replacement = new CountingLifetimePolicy("A");
        TestPolicyProvider provider = null!;
        var retiring = new RetiringLifetimePolicy(
            "A",
            () => provider.Emit(new PolicyUpdate("A", replacement)));
        provider = new TestPolicyProvider(disposePolicies: false, retiring);
        using var registry = new PolicyCollection([provider]);
        registry.Connect();

        // act
        // The first pin attempt fails because the instance is being retired, which swaps in the
        // replacement so the retry resolves it.
        var pinned = registry.Pin("A");

        // assert
        Assert.Same(replacement, pinned);
    }

    [Fact]
    public void Pin_Should_KeepPinnedInstanceAlive_When_RegistrySwapsMidUse()
    {
        // arrange
        var first = new CountingLifetimePolicy("P");
        var provider = new TestPolicyProvider(disposePolicies: false, first);
        using var registry = new PolicyCollection([provider]);
        registry.Connect();
        var pinned = registry.Pin("P");

        // act
        var second = new CountingLifetimePolicy("P");
        provider.Emit(new PolicyUpdate("P", second));

        // assert
        Assert.Same(first, pinned);
        Assert.Same(second, registry.Get("P"));
    }

    [Fact]
    public void Apply_Should_LeaveReleaseToProvider_When_PolicyIsReplaced()
    {
        // arrange
        var first = new CountingLifetimePolicy("P");
        var provider = new TestPolicyProvider(disposePolicies: false, first);
        using var registry = new PolicyCollection([provider]);
        registry.Connect();
        var second = new CountingLifetimePolicy("P");

        // act
        // The provider owns and releases the retired instance; the registry must not release it too.
        provider.Emit(new PolicyUpdate("P", second));

        // assert
        Assert.Same(second, registry.Get("P"));
        Assert.Equal(1, first.ReleaseCount);
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

    private sealed class CountingLifetimePolicy(string name) : IPolicy, IPolicyLifetime
    {
        private int _refCount = 1;

        public string Name { get; } = name;

        public SelectionSetNode? Requirements => null;

        public int ReleaseCount { get; private set; }

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            ReadOnlyMemory<CompositeResultElement> entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public bool TryAddRef()
        {
            if (_refCount <= 0)
            {
                return false;
            }

            _refCount++;
            return true;
        }

        public void Release()
        {
            ReleaseCount++;
            _refCount--;
        }
    }

    // A lifetime policy that fails its first pin attempt and swaps in a replacement, so the pinning
    // retry loop resolves the new instance.
    private sealed class RetiringLifetimePolicy(string name, Action onFirstTryAddRef)
        : IPolicy, IPolicyLifetime
    {
        private bool _retired;

        public string Name { get; } = name;

        public SelectionSetNode? Requirements => null;

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            ReadOnlyMemory<CompositeResultElement> entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public bool TryAddRef()
        {
            if (_retired)
            {
                return true;
            }

            _retired = true;
            onFirstTryAddRef();
            return false;
        }

        public void Release()
        {
        }
    }

    private sealed class DisposablePolicy : IPolicy, IDisposable
    {
        public string Name => "CanReadSecret";

        public SelectionSetNode? Requirements => null;

        public int DisposeCalls { get; private set; }

        public ValueTask EvaluateAsync(
            IPolicyContext context,
            ReadOnlyMemory<CompositeResultElement> entities,
            CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public void Dispose() => DisposeCalls++;
    }
}
