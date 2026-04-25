using Microsoft.Extensions.Primitives;

namespace Mocha.Resources.Tests;

public class CompositeMochaResourceSourceTests
{
    [Fact]
    public void GetChangeToken_Should_Fire_When_ChildSourceFires()
    {
        // arrange
        var child = new MutableMochaResourceSource();
        var composite = new CompositeMochaResourceSource([child]);
        var fired = false;
        composite.GetChangeToken().RegisterChangeCallback(_ => fired = true, null);

        // act
        child.AddResource(new TestResource("a", "id-a"));

        // assert
        Assert.True(fired);
    }

    [Fact]
    public void Resources_Should_IncludeChildAddedAfterFire_When_ChildFires()
    {
        // arrange
        var child = new MutableMochaResourceSource();
        child.AddResource(new TestResource("a", "id-a"));
        var composite = new CompositeMochaResourceSource([child]);

        // prime the snapshot before mutating
        Assert.Single(composite.Resources);

        // act
        child.AddResource(new TestResource("b", "id-b"));

        // assert
        Assert.Equal(2, composite.Resources.Count);
    }

    [Fact]
    public void GetChangeToken_Should_NotRecurseInfinitely_When_CallbackReregisters()
    {
        // arrange
        var child = new MutableMochaResourceSource();
        var composite = new CompositeMochaResourceSource([child]);
        var callbackCount = 0;

        void Register()
        {
            composite.GetChangeToken().RegisterChangeCallback(_ =>
            {
                callbackCount++;
                if (callbackCount < 5)
                {
                    Register();
                }
            }, null);
        }

        Register();

        // act — five token fires, each fire's callback re-registers on the *next* token, not the firing one
        for (var i = 0; i < 5; i++)
        {
            child.RaiseChange();
        }

        // assert — exactly five invocations, no stack overflow
        Assert.Equal(5, callbackCount);
    }

    [Fact]
    public void Dispose_Should_DisposeChildRegistrations()
    {
        // arrange
        var child = new DisposableMochaResourceSource();
        var composite = new CompositeMochaResourceSource([child]);

        // prime change token so registrations exist
        composite.GetChangeToken();

        // act
        composite.Dispose();

        // assert
        Assert.True(child.Disposed);
    }

    [Fact]
    public void GetChangeToken_Should_BeLazy_When_NoConsumerHasReadIt()
    {
        // arrange
        var child = new MutableMochaResourceSource();
        var composite = new CompositeMochaResourceSource([child]);

        // act — fire the child before any consumer subscribes
        child.RaiseChange();

        var token = composite.GetChangeToken();

        // assert — token has not yet fired because nobody listened before
        Assert.False(token.HasChanged);
    }

    private sealed class MutableMochaResourceSource : MochaResourceSource
    {
        private readonly List<MochaResource> _resources = [];
        private CancellationTokenSource _cts = new();

        public override IReadOnlyList<MochaResource> Resources => _resources;

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(_cts.Token);

        public void AddResource(MochaResource resource)
        {
            _resources.Add(resource);
            RaiseChange();
        }

        public void RaiseChange()
        {
            var oldCts = _cts;
            _cts = new CancellationTokenSource();
            oldCts.Cancel();
            oldCts.Dispose();
        }
    }

    private sealed class DisposableMochaResourceSource : MochaResourceSource, IDisposable
    {
        private readonly CancellationTokenSource _cts = new();

        public override IReadOnlyList<MochaResource> Resources => [];

        public override IChangeToken GetChangeToken() => new CancellationChangeToken(_cts.Token);

        public bool Disposed { get; private set; }

        public void Dispose()
        {
            Disposed = true;
            _cts.Dispose();
        }
    }
}

internal sealed class TestResource : MochaResource
{
    public TestResource(string kind, string id)
    {
        Kind = kind;
        Id = id;
    }

    public override string Kind { get; }

    public override string Id { get; }

    public override void Write(System.Text.Json.Utf8JsonWriter writer)
    {
        writer.WriteString("kind", Kind);
    }
}
