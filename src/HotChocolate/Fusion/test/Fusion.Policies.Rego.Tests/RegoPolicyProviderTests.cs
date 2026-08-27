using System.Buffers;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Policies.Rego;

public sealed class RegoPolicyProviderTests
{
    [Fact]
    public async Task Data_Should_NotRecompileOrEmit_When_DataIsIdentical()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config("""{"a":1}""", "d1", Policy("p1", "c1")));
        await using var provider = new RegoPolicyProvider(config, new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var initialInstance = observer.Current("p1.allow");

        // act
        config.Publish(Config("""{"a":1}""", "d1", Policy("p1", "c1")));

        // assert
        Assert.Single(observer.Updates);
        Assert.Same(initialInstance, observer.Current("p1.allow"));
    }

    [Fact]
    public async Task Data_Should_RecompileEveryPolicy_When_DataChanges()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config("""{"a":1}""", "d1", Policy("p1", "c1"), Policy("p2", "c2")));
        await using var provider = new RegoPolicyProvider(config, new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var first1 = observer.Current("p1.allow");
        var first2 = observer.Current("p2.allow");

        // act
        config.Publish(
            Config("""{"a":2}""", "d2", Policy("p1", "c1"), Policy("p2", "c2")));

        // assert
        Assert.Equal(2, observer.Updates.Count);
        Assert.NotSame(first1, observer.Current("p1.allow"));
        Assert.NotSame(first2, observer.Current("p2.allow"));
    }

    [Fact]
    public async Task Code_Should_RecompileEveryPolicy_When_SinglePolicyChanges()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config(Policy("p1", "c1"), Policy("p2", "c2")));
        await using var provider = new RegoPolicyProvider(config, new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var first1 = observer.Current("p1.allow");
        var first2 = observer.Current("p2.allow");

        // act
        config.Publish(Config(Policy("p1", "c1-changed"), Policy("p2", "c2")));

        // assert
        // A single policy change rebuilds and publishes one complete replacement snapshot.
        Assert.Equal(2, observer.Updates.Count);
        Assert.NotSame(first1, observer.Current("p1.allow"));
        Assert.NotSame(first2, observer.Current("p2.allow"));
    }

    [Fact]
    public async Task Code_Should_KeepLastGoodAndLog_When_CompilationFails()
    {
        // arrange
        var diagnostics = new CapturingDiagnostics();
        await using var config = new MutableFusionConfigurationProvider(Config(Policy("p1", "c1")));
        await using var provider = new RegoPolicyProvider(config, diagnostics);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var lastGood = observer.Current("p1.allow");

        // act
        config.Publish(Config(Broken("p1", "c1-broken")));

        // assert
        // The broken update is not published, so the last-good instance remains the current one.
        Assert.Single(observer.Updates);
        Assert.Same(lastGood, observer.Current("p1.allow"));
        Assert.NotEmpty(diagnostics.Errors);
    }

    [Fact]
    public async Task Code_Should_DropPolicy_When_PolicyIsRemoved()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config(Policy("p1", "c1"), Policy("p2", "c2")));
        await using var provider = new RegoPolicyProvider(config, new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        config.Publish(Config(Policy("p1", "c1")));

        // assert
        Assert.Single(observer.Updates[^1]);
        Assert.NotNull(observer.Current("p1.allow"));
        Assert.Null(observer.Current("p2.allow"));
    }

    [Fact]
    public async Task Code_Should_ResolveEntrypoint_When_PackageDiffersFromName()
    {
        // arrange
        // The policy name is the full rule path and the package is free-form, so the entrypoint
        // 'data.acme.products.visible' resolves even though the package is not the policy name.
        var content = new PolicyContent(
            "acme.products.visible",
            PolicyContentType.Rego,
            Encoding.UTF8.GetBytes("package acme.products\nimport rego.v1\ndefault visible := true\n"),
            PolicyRequirements.Empty,
            Encoding.UTF8.GetBytes("d1"));
        await using var config = new MutableFusionConfigurationProvider(Config(content));
        await using var provider = new RegoPolicyProvider(config, new CapturingDiagnostics());
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        var policy = observer.Current("acme.products.visible");
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        await policy!.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.NotNull(policy);
        Assert.Empty(context.DeniedIndices);
    }

    // The policy name is the full rule path, so a base package named for the pair exposes the
    // conventional 'allow' rule at 'data.<base>.allow'.
    private static PolicyContent Policy(string @base, string digest)
        => new(
            $"{@base}.allow",
            PolicyContentType.Rego,
            Encoding.UTF8.GetBytes($"package {@base}\nimport rego.v1\ndefault allow := true\n"),
            PolicyRequirements.Empty,
            Encoding.UTF8.GetBytes(digest));

    // The rule body is malformed, so the whole set fails to compile.
    private static PolicyContent Broken(string @base, string digest)
        => new(
            $"{@base}.allow",
            PolicyContentType.Rego,
            Encoding.UTF8.GetBytes($"package {@base}\nimport rego.v1\nallow if {{\n"),
            PolicyRequirements.Empty,
            Encoding.UTF8.GetBytes(digest));

    private static FusionConfiguration Config(params PolicyContent[] policies)
        => Config("{}", "data-digest", policies);

    private static FusionConfiguration Config(
        string data,
        string dataDigest,
        params PolicyContent[] policies)
    {
        var schema = Utf8GraphQLParser.Parse("type Query { x: Int }");
        var settings = new JsonDocumentOwner(JsonDocument.Parse("{}"), EmptyMemoryOwner.Instance);
        var content = new PolicyContentSnapshot(
            "rego",
            new Version(1, 0, 0),
            [.. policies],
            Encoding.UTF8.GetBytes(data),
            Encoding.UTF8.GetBytes(dataDigest),
            null);
        return new FusionConfiguration(schema, settings) { Policies = content };
    }

    private sealed class CapturingObserver : IObserver<ImmutableArray<IPolicy>>
    {
        private ImmutableArray<IPolicy> _current = [];

        public List<ImmutableArray<IPolicy>> Updates { get; } = [];

        public IPolicy? Current(string name)
            => _current.FirstOrDefault(
                p => p.Name.Equals(name, StringComparison.Ordinal));

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
