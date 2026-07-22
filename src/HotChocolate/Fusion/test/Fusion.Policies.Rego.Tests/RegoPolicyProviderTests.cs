using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoPolicyProviderTests
{
    [Fact]
    public async Task Data_Should_NotRecompileOrEmit_When_DataIsIdentical()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(Config(Policy("p1", "c1")));
        var data = new FakeDataProvider();
        data.Push("""{"a":1}""");
        await using var provider = new RegoPolicyProvider(config, [data], new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var initialInstance = observer.Current("p1");

        // act
        data.Push("""{"a":1}""");

        // assert
        Assert.Single(observer.Updates);
        Assert.Same(initialInstance, observer.Current("p1"));
    }

    [Fact]
    public async Task Data_Should_RecompileEveryPolicyOnce_When_DataChanges()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config(Policy("p1", "c1"), Policy("p2", "c2")));
        var data = new FakeDataProvider();
        data.Push("""{"a":1}""");
        await using var provider = new RegoPolicyProvider(config, [data], new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var first1 = observer.Current("p1");
        var first2 = observer.Current("p2");

        // act
        data.Push("""{"a":2}""");

        // assert
        // Two initial upserts plus one re-emission per policy after the data change.
        Assert.Equal(4, observer.Updates.Count);
        Assert.NotSame(first1, observer.Current("p1"));
        Assert.NotSame(first2, observer.Current("p2"));
    }

    [Fact]
    public async Task Code_Should_RecompileOnlyChangedPolicy_When_SinglePolicyChanges()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config(Policy("p1", "c1"), Policy("p2", "c2")));
        var data = new FakeDataProvider();
        data.Push("""{"a":1}""");
        await using var provider = new RegoPolicyProvider(config, [data], new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var first1 = observer.Current("p1");
        var first2 = observer.Current("p2");

        // act
        config.Publish(Config(Policy("p1", "c1-changed"), Policy("p2", "c2")));

        // assert
        // Only the changed policy is re-emitted, so a single update follows the two initial upserts.
        Assert.Equal(3, observer.Updates.Count);
        Assert.NotSame(first1, observer.Current("p1"));
        Assert.Same(first2, observer.Current("p2"));
    }

    [Fact]
    public async Task Code_Should_KeepLastGoodAndLog_When_CompilationFails()
    {
        // arrange
        var diagnostics = new CapturingDiagnostics();
        await using var config = new MutableFusionConfigurationProvider(Config(Policy("p1", "c1")));
        var data = new FakeDataProvider();
        data.Push("""{"a":1}""");
        await using var provider = new RegoPolicyProvider(config, [data], diagnostics);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var lastGood = observer.Current("p1");

        // act
        config.Publish(Config(Broken("p1", "c1-broken")));

        // assert
        // The broken update is not published, so the last-good instance remains the current one.
        Assert.Single(observer.Updates);
        Assert.Same(lastGood, observer.Current("p1"));
        Assert.NotEmpty(diagnostics.Errors);
    }

    [Fact]
    public async Task Code_Should_DropPolicy_When_PolicyIsRemoved()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config(Policy("p1", "c1"), Policy("p2", "c2")));
        var data = new FakeDataProvider();
        data.Push("""{"a":1}""");
        await using var provider = new RegoPolicyProvider(config, [data], new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        config.Publish(Config(Policy("p1", "c1")));

        // assert
        // The removed policy arrives as a null-policy update, leaving only p1 current.
        Assert.Equal(new PolicyUpdate("p2", null), observer.Updates[^1]);
        Assert.NotNull(observer.Current("p1"));
    }

    [Fact]
    public async Task Provider_Should_PublishNothing_When_NoDataHasArrived()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(Config(Policy("p1", "c1")));
        var data = new FakeDataProvider();
        await using var provider = new RegoPolicyProvider(config, [data], new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var beforeData = observer.Updates.Count;

        // act
        data.Push("{}");

        // assert
        Assert.Equal(0, beforeData);
        Assert.Single(observer.Updates);
    }

    private static PolicyContent Policy(string name, string digest)
        => new(
            name,
            PolicyContentType.Rego,
            Encoding.UTF8.GetBytes($"package {name}\nimport rego.v1\nallow := [true]\n"),
            "{}"u8.ToArray(),
            Encoding.UTF8.GetBytes(digest));

    private static PolicyContent Broken(string name, string digest)
        => new(
            name,
            PolicyContentType.Rego,
            "package wrong\nimport rego.v1\nallow := [true]\n"u8.ToArray(),
            "{}"u8.ToArray(),
            Encoding.UTF8.GetBytes(digest));

    private static FusionConfiguration Config(params PolicyContent[] policies)
    {
        var schema = Utf8GraphQLParser.Parse("type Query { x: Int }");
        var settings = new JsonDocumentOwner(JsonDocument.Parse("{}"), EmptyMemoryOwner.Instance);
        var content = new PolicyContentSnapshot(
            "rego",
            new Version(1, 0, 0),
            [.. policies],
            "{}"u8.ToArray(),
            "data-digest"u8.ToArray(),
            null);
        return new FusionConfiguration(schema, settings) { Policies = content };
    }

    private sealed class CapturingObserver : IObserver<PolicyUpdate>
    {
        private readonly Dictionary<string, IPolicy> _current = new(StringComparer.Ordinal);

        public List<PolicyUpdate> Updates { get; } = [];

        public IPolicy? Current(string name)
            => _current.TryGetValue(name, out var policy) ? policy : null;

        public void OnNext(PolicyUpdate value)
        {
            Updates.Add(value);

            if (value.Policy is null)
            {
                _current.Remove(value.Name);
            }
            else
            {
                _current[value.Name] = value.Policy;
            }
        }

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
    }

    private sealed class FakeDataProvider : IPolicyDataProvider
    {
        private readonly List<IObserver<PolicyDataSnapshot>> _observers = [];
        private byte[]? _data;

        public void Push(string json)
        {
            _data = Encoding.UTF8.GetBytes(json);

            foreach (var observer in _observers.ToArray())
            {
                observer.OnNext(Materialize());
            }
        }

        public IDisposable Subscribe(IObserver<PolicyDataSnapshot> observer)
        {
            _observers.Add(observer);

            if (_data is not null)
            {
                observer.OnNext(Materialize());
            }

            return new Subscription(this, observer);
        }

        private PolicyDataSnapshot Materialize()
        {
            var owner = MemoryPool<byte>.Shared.Rent(_data!.Length);
            _data.CopyTo(owner.Memory.Span);
            return new PolicyDataSnapshot(owner, _data.Length);
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private sealed class Subscription(FakeDataProvider provider, IObserver<PolicyDataSnapshot> observer)
            : IDisposable
        {
            public void Dispose() => provider._observers.Remove(observer);
        }
    }

    private sealed class CapturingDiagnostics : FusionExecutionDiagnosticEventListener
    {
        public List<string> Errors { get; } = [];

        public override void PolicyCompilationError(string policyName, Exception error)
            => Errors.Add($"{policyName}: {error.Message}");

        public override void PolicyUpdateError(Exception error)
            => Errors.Add(error.Message);
    }

    private sealed class EmptyMemoryOwner : IMemoryOwner<byte>
    {
        public static readonly EmptyMemoryOwner Instance = new();

        public Memory<byte> Memory => default;

        public void Dispose()
        {
        }
    }
}
