using System.Buffers;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class ArchiveRegoPolicyDataProviderTests
{
    [Fact]
    public async Task Subscribe_Should_DeliverInitialSnapshot_When_DataIsAvailable()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(Config("""{"x":1}""", "d1"));
        await using var provider = new ArchiveRegoPolicyDataProvider(config);
        var observer = new CapturingObserver();

        // act
        using var subscription = provider.Subscribe(observer);

        // assert
        var snapshot = Assert.Single(observer.Snapshots);
        Assert.Equal("""{"x":1}""", Encoding.UTF8.GetString(snapshot.Data.Span));
    }

    [Fact]
    public async Task Subscribe_Should_MaterializeSeparateSnapshots_When_MultipleSubscribers()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(Config("""{"x":1}""", "d1"));
        await using var provider = new ArchiveRegoPolicyDataProvider(config);
        var first = new CapturingObserver();
        var second = new CapturingObserver();

        // act
        using var s1 = provider.Subscribe(first);
        using var s2 = provider.Subscribe(second);

        // assert
        Assert.NotSame(first.Snapshots[0], second.Snapshots[0]);
    }

    [Fact]
    public async Task Provider_Should_Dedup_When_DataDigestIsUnchanged()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(Config("""{"x":1}""", "d1"));
        await using var provider = new ArchiveRegoPolicyDataProvider(config);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        config.Publish(Config("""{"x":2}""", "d1"));

        // assert
        Assert.Single(observer.Snapshots);
    }

    private static FusionConfiguration Config(string dataJson, string dataDigest)
    {
        var schema = Utf8GraphQLParser.Parse("type Query { x: Int }");
        var settings = new JsonDocumentOwner(JsonDocument.Parse("{}"), EmptyMemoryOwner.Instance);
        var content = new PolicyContentSnapshot(
            "rego",
            new Version(1, 0, 0),
            [],
            Encoding.UTF8.GetBytes(dataJson),
            Encoding.UTF8.GetBytes(dataDigest),
            null);
        return new FusionConfiguration(schema, settings) { Policies = content };
    }

    private sealed class CapturingObserver : IObserver<PolicyDataSnapshot>
    {
        public List<PolicyDataSnapshot> Snapshots { get; } = [];

        public void OnNext(PolicyDataSnapshot value) => Snapshots.Add(value);

        public void OnError(Exception error)
        {
        }

        public void OnCompleted()
        {
        }
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
