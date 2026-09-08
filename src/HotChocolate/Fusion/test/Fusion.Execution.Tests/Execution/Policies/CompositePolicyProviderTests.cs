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
