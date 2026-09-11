using System.Collections.Immutable;
using HotChocolate.Fusion.Configuration;

namespace HotChocolate.Fusion.Execution;

public sealed class CompositePolicyProviderTests
{
    [Fact]
    public async Task Snapshot_Should_ContainOnlyBuiltIns_When_NoUserProviderIsRegistered()
    {
        // arrange
        var builtIns = BuiltInPolicySet.Create(
            [BuiltInPolicyNames.Authenticated], BuiltInPolicySet.DefaultScopeClaimTypes);
        await using var provider = new CompositePolicyProvider(null, builtIns);
        using var collection = new PolicyCollection(provider);

        // act
        collection.Connect();

        // assert
        Assert.True(collection.TryGet(BuiltInPolicyNames.Authenticated, out _));
    }

    [Fact]
    public async Task Snapshot_Should_LetUserPolicyOverrideBuiltIn_When_NamesCollide()
    {
        // arrange
        var userPolicy = new TestPolicy(BuiltInPolicyNames.Authenticated);
        var userProvider = new TestPolicyProvider(userPolicy);
        var builtIns = BuiltInPolicySet.Create(
            [BuiltInPolicyNames.Authenticated], BuiltInPolicySet.DefaultScopeClaimTypes);
        await using var provider = new CompositePolicyProvider(userProvider, builtIns);
        using var collection = new PolicyCollection(provider);

        // act
        collection.Connect();

        // assert
        Assert.Same(userPolicy, collection.Get(BuiltInPolicyNames.Authenticated));
    }

    [Fact]
    public async Task Snapshot_Should_IncludeBothUserAndBuiltInPolicies_When_NamesDiffer()
    {
        // arrange
        var userPolicy = new TestPolicy("CanRead");
        var userProvider = new TestPolicyProvider(userPolicy);
        var builtIns = BuiltInPolicySet.Create(
            [BuiltInPolicyNames.Authenticated], BuiltInPolicySet.DefaultScopeClaimTypes);
        await using var provider = new CompositePolicyProvider(userProvider, builtIns);
        using var collection = new PolicyCollection(provider);

        // act
        collection.Connect();

        // assert
        Assert.Same(userPolicy, collection.Get("CanRead"));
        Assert.IsType<AuthenticatedPolicy>(collection.Get(BuiltInPolicyNames.Authenticated));
    }

    [Fact]
    public async Task Snapshot_Should_RejectDuplicateNames_When_UserProviderEmitsDuplicate()
    {
        // arrange
        var userProvider = new TestPolicyProvider(disposePolicies: true);
        var builtIns = BuiltInPolicySet.Create([], BuiltInPolicySet.DefaultScopeClaimTypes);
        await using var provider = new CompositePolicyProvider(userProvider, builtIns);
        using var collection = new PolicyCollection(provider);
        collection.Connect();

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => userProvider.Emit(new TestPolicy("CanRead"), new TestPolicy("CanRead")));

        // assert
        Assert.Contains("CanRead", exception.Message);
    }

    [Fact]
    public async Task OnNext_Should_ForwardContent_When_UserProviderIsAContentSink()
    {
        // arrange
        var sink = new RecordingContentSink();
        var builtIns = BuiltInPolicySet.Create([], BuiltInPolicySet.DefaultScopeClaimTypes);
        await using var provider = new CompositePolicyProvider(sink, builtIns);

        // act
        ((IObserver<PolicyContentSnapshot?>)provider).OnNext(null);

        // assert
        Assert.True(sink.OnNextCalled);
    }

    // Regression for repo-ctf.24 fix cycle 2 (comment 734, F1): the gateway's schema services
    // resolve this composite (as the policy content sink) before CompositeSchemaBuilder knows the
    // schema's built-ins, so it is constructed with an empty set and configured afterward via
    // SetBuiltIns, on the very same instance a content push already reached.
    [Fact]
    public async Task SetBuiltIns_Should_AddBuiltIns_When_CalledAfterConstruction()
    {
        // arrange
        var userPolicy = new TestPolicy("CanRead");
        var userProvider = new TestPolicyProvider(userPolicy);
        await using var provider = new CompositePolicyProvider(userProvider, []);
        using var collection = new PolicyCollection(provider);
        collection.Connect();

        // act
        provider.SetBuiltIns(
            BuiltInPolicySet.Create([BuiltInPolicyNames.Authenticated], BuiltInPolicySet.DefaultScopeClaimTypes));

        // assert
        Assert.Same(userPolicy, collection.Get("CanRead"));
        Assert.IsType<AuthenticatedPolicy>(collection.Get(BuiltInPolicyNames.Authenticated));
    }

    // Regression for repo-ctf.24 fix cycle 2 (comment 734, F1): a content push delivered while the
    // built-in set is still empty (the sink is resolved ahead of schema completion) must still
    // reach the wrapped user provider, and the built-ins configured afterward must still be
    // present alongside the content it produced.
    [Fact]
    public async Task OnNext_Should_ReachUserPolicies_When_DeliveredBeforeBuiltInsAreConfigured()
    {
        // arrange
        var sink = new ContentDrivenPolicyProvider();
        await using var provider = new CompositePolicyProvider(sink, []);
        using var collection = new PolicyCollection(provider);
        collection.Connect();

        // act
        ((IObserver<PolicyContentSnapshot?>)provider).OnNext(null);
        provider.SetBuiltIns(
            BuiltInPolicySet.Create([BuiltInPolicyNames.Authenticated], BuiltInPolicySet.DefaultScopeClaimTypes));

        // assert
        Assert.Same(sink.CurrentPolicy, collection.Get("CanRead"));
        Assert.IsType<AuthenticatedPolicy>(collection.Get(BuiltInPolicyNames.Authenticated));
    }

    private sealed class ContentDrivenPolicyProvider : IPolicyProvider, IObserver<PolicyContentSnapshot?>
    {
        private IObserver<ImmutableArray<IPolicy>>? _observer;

        public IPolicy CurrentPolicy { get; private set; } = new TestPolicy("CanRead");

        public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
        {
            _observer = observer;
            observer.OnNext([]);
            return NullDisposable.Instance;
        }

        public void OnNext(PolicyContentSnapshot? value) => _observer?.OnNext([CurrentPolicy]);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed class RecordingContentSink : IPolicyProvider, IObserver<PolicyContentSnapshot?>
    {
        public bool OnNextCalled { get; private set; }

        public IDisposable Subscribe(IObserver<ImmutableArray<IPolicy>> observer)
        {
            observer.OnNext([]);
            return NullDisposable.Instance;
        }

        public void OnNext(PolicyContentSnapshot? value) => OnNextCalled = true;

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new();

            public void Dispose()
            {
            }
        }
    }
}
