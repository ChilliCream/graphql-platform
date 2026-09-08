using System.Buffers;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
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
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(config.Configuration!.Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var initialInstance = observer.Current("p1.allow");

        // act
        config.Publish(Config("""{"a":1}""", "d1", Policy("p1", "c1")));
        provider.OnNext(config.Configuration!.Policies);

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
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(config.Configuration!.Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var first1 = observer.Current("p1.allow");
        var first2 = observer.Current("p2.allow");

        // act
        config.Publish(
            Config("""{"a":2}""", "d2", Policy("p1", "c1"), Policy("p2", "c2")));
        provider.OnNext(config.Configuration!.Policies);

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
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(config.Configuration!.Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var first1 = observer.Current("p1.allow");
        var first2 = observer.Current("p2.allow");

        // act
        config.Publish(Config(Policy("p1", "c1-changed"), Policy("p2", "c2")));
        provider.OnNext(config.Configuration!.Policies);

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
        await using var provider = new RegoPolicyProvider(diagnostics);
        provider.OnNext(config.Configuration!.Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var lastGood = observer.Current("p1.allow");

        // act
        config.Publish(Config(Broken("p1", "c1-broken")));
        provider.OnNext(config.Configuration!.Policies);

        // assert
        // The broken update is not published, so the last-good instance remains the current one.
        Assert.Single(observer.Updates);
        Assert.Same(lastGood, observer.Current("p1.allow"));
        Assert.NotEmpty(diagnostics.Errors);
    }

    [Fact]
    public async Task Code_Should_RetryCompile_When_IdenticalBrokenContentIsResent()
    {
        // arrange
        var diagnostics = new CapturingDiagnostics();
        await using var config = new MutableFusionConfigurationProvider(Config(Broken("p1", "c1-broken")));
        await using var provider = new RegoPolicyProvider(diagnostics);

        // act
        // The exact same broken candidate is published twice. Neither attempt is ever committed
        // as the "current" content, so the second attempt is not mistaken for a no-op and is
        // retried (and reported) just like the first.
        provider.OnNext(config.Configuration!.Policies);
        config.Publish(Config(Broken("p1", "c1-broken")));
        provider.OnNext(config.Configuration!.Policies);

        // assert
        Assert.Equal(2, diagnostics.Errors.Count);
    }

    [Fact]
    public async Task Code_Should_DropPolicy_When_PolicyIsRemoved()
    {
        // arrange
        await using var config = new MutableFusionConfigurationProvider(
            Config(Policy("p1", "c1"), Policy("p2", "c2")));
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(config.Configuration!.Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        config.Publish(Config(Policy("p1", "c1")));
        provider.OnNext(config.Configuration!.Policies);

        // assert
        Assert.Single(observer.Updates[^1]);
        Assert.NotNull(observer.Current("p1.allow"));
        Assert.Null(observer.Current("p2.allow"));
    }

    [Fact]
    public async Task Code_Should_ResolveEntrypoint_When_PackageDiffersFromName()
    {
        // arrange
        // The pair name and package are free-form, so the annotated rule resolves at the pair path.
        var content = new PolicyContent(
            "acme.products",
            PolicyContentType.Rego,
            Encoding.UTF8.GetBytes(
                "package acme.products\nimport rego.v1\n# METADATA\n# entrypoint: true\ndefault visible := true\n"),
            PolicyRequirements.Empty,
            Encoding.UTF8.GetBytes("d1"));
        await using var config = new MutableFusionConfigurationProvider(Config(content));
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(config.Configuration!.Policies);
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

    [Fact]
    public async Task Code_Should_ExposeAndEvaluateAnnotatedRules_When_MetadataMarksEntrypoints()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(
            Config(
                Policy(
                    "p1",
                    """
                    package p1
                    import rego.v1

                    # METADATA
                    # entrypoint: true
                    default read := true

                    # METADATA
                    # entrypoint: true
                    default write := true

                    default allow := false
                    """,
                    "c1")).Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var read = observer.Current("p1.read")!;
        var write = observer.Current("p1.write")!;
        var readContext = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);
        var writeContext = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);

        // act
        await read.EvaluateAsync(readContext, TestContext.Current.CancellationToken);
        await write.EvaluateAsync(writeContext, TestContext.Current.CancellationToken);

        // assert
        observer.Updates[^1].Select(static p => p.Name).MatchInlineSnapshot(
            """
            [
              "p1.read",
              "p1.write"
            ]
            """);
        Assert.Empty(readContext.DeniedIndices);
        Assert.Empty(writeContext.DeniedIndices);
    }

    [Fact]
    public async Task Code_Should_ExposeAnnotatedRule_When_SourceStartsWithBom()
    {
        // arrange
        var diagnostics = new CapturingDiagnostics();
        await using var provider = new RegoPolicyProvider(diagnostics);
        provider.OnNext(
            Config(
                Policy(
                    "p1",
                    """
                    ﻿package p1
                    import rego.v1

                    # METADATA
                    # entrypoint: true
                    default read := true

                    default allow := false
                    """,
                    "c1")).Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var policy = observer.Current("p1.read")!;
        var context = new RegoPolicyTestEntities.TestPolicyContext(entities: new CompositeResultElement[1]);

        // act
        await policy.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        observer.Updates[^1].Select(static p => p.Name).MatchInlineSnapshot(
            """
            [
              "p1.read"
            ]
            """);
        Assert.Empty(context.DeniedIndices);
        Assert.Empty(diagnostics.Errors);
    }

    [Fact]
    public void Scanner_Should_IgnoreMetadataInsideRuleBodies()
    {
        // arrange
        const string source =
            """
            package p1
            import rego.v1

            allow if {
              # METADATA
              # entrypoint: true
              default body := true
            }
            """;

        // act
        var entryPoints = RegoEntrypointScanner.Scan(source);

        // assert
        Assert.Empty(entryPoints);
    }

    [Fact]
    public void Scanner_Should_IgnoreMetadataInsideMultilineRawStrings()
    {
        // arrange
        const string source =
            """
            package p1
            import rego.v1

            note := `
            # METADATA
            # entrypoint: true
            default hidden := true
            `

            # METADATA
            # entrypoint: true
            default read := true
            """;

        // act
        var entryPoints = RegoEntrypointScanner.Scan(source);

        // assert
        Assert.Equal(new[] { "read" }, entryPoints);
    }

    [Fact]
    public void Scanner_Should_IgnoreQuotedAndCommentDelimiters()
    {
        // arrange
        const string source =
            """
            package p1
            import rego.v1

            message := "quoted { # } and \"quoted\""
            # } ] )

            # METADATA
            # entrypoint: true
            default read := true
            """;

        // act
        var entryPoints = RegoEntrypointScanner.Scan(source);

        // assert
        Assert.Equal(new[] { "read" }, entryPoints);
    }

    [Fact]
    public void Scanner_Should_RecognizeSimpleAndDefaultRuleHeads()
    {
        // arrange
        const string source =
            """
            package p1
            import rego.v1

            # METADATA
            # entrypoint: true
            read if { true }

            # METADATA
            # entrypoint: true
            default write := true
            """;

        // act
        var entryPoints = RegoEntrypointScanner.Scan(source);

        // assert
        Assert.Equal(new[] { "read", "write" }, entryPoints);
    }

    [Fact]
    public void Scanner_Should_IgnoreComplexAndMalformedRuleHeads()
    {
        // arrange
        const string source =
            """
            package p1
            import rego.v1

            # METADATA
            # entrypoint: true
            read.value if { true }

            # METADATA
            # entrypoint: true
            write(value) if { true }

            # METADATA
            # entrypoint: true
            admin[input.subject] if { true }

            # METADATA
            # entrypoint: true
            deny malformed
            """;

        // act
        var entryPoints = RegoEntrypointScanner.Scan(source);

        // assert
        Assert.Empty(entryPoints);
    }

    [Fact]
    public async Task Code_Should_NotExposeUnannotatedAllow_When_AnnotatedRulesExist()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(
            Config(
                Policy(
                    "p1",
                    """
                    package p1
                    import rego.v1

                    # METADATA
                    # entrypoint: true
                    default read := true

                    default allow := true
                    """,
                    "c1")).Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        var names = observer.Updates[^1].Select(static p => p.Name);

        // assert
        names.MatchInlineSnapshot(
            """
            [
              "p1.read"
            ]
            """);
    }

    [Fact]
    public async Task Code_Should_ExposeAllowOnly_When_ModuleHasNoEntrypointMetadata()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(
            Config(
                Policy(
                    "p1",
                    """
                    package p1
                    import rego.v1

                    default read := true
                    default write := true
                    default allow := true
                    """,
                    "c1")).Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        var names = observer.Updates[^1].Select(static p => p.Name);

        // assert
        names.MatchInlineSnapshot(
            """
            [
              "p1.allow"
            ]
            """);
    }

    [Fact]
    public async Task Code_Should_IgnoreMetadataWithoutEntrypointTrue()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(
            Config(
                Policy(
                    "p1",
                    """
                    package p1
                    import rego.v1

                    # METADATA
                    # title: Read products
                    default read := true
                    default allow := true
                    """,
                    "c1")).Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        var names = observer.Updates[^1].Select(static p => p.Name);

        // assert
        names.MatchInlineSnapshot(
            """
            [
              "p1.allow"
            ]
            """);
    }

    [Fact]
    public async Task Code_Should_IgnoreEntrypointMetadata_BeforePackage()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(
            Config(
                Policy(
                    "p1",
                    """
                    # METADATA
                    # entrypoint: true
                    package p1
                    import rego.v1

                    default read := true
                    default allow := true
                    """,
                    "c1")).Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);

        // act
        var names = observer.Updates[^1].Select(static p => p.Name);

        // assert
        names.MatchInlineSnapshot(
            """
            [
              "p1.allow"
            ]
            """);
    }

    [Fact]
    public async Task Code_Should_ReportEveryDecision_When_AnnotatedPairFailsToCompile()
    {
        // arrange
        var diagnostics = new CapturingDiagnostics();
        await using var provider = new RegoPolicyProvider(diagnostics);
        provider.OnNext(
            Config(
                Policy(
                    "p",
                    """
                    package p
                    import rego.v1

                    # METADATA
                    # entrypoint: true
                    default read := true
                    """,
                    "c0"),
                Policy(
                    "p1",
                    """
                    package p1
                    import rego.v1

                    # METADATA
                    # entrypoint: true
                    default read := true

                    # METADATA
                    # entrypoint: true
                    default write := true

                    broken if {
                    """,
                    "c1")).Policies);

        // act
        var names = diagnostics.Errors.Select(static error => error.Split(':')[0]);

        // assert
        names.MatchInlineSnapshot(
            """
            [
              "p1.read",
              "p1.write"
            ]
            """);
    }

    [Fact]
    public async Task Policy_Should_RemainUsable_When_ProviderReleasesCurrentSnapshots()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(Config(Policy("p1", "v1")).Policies);
        var observer = new CapturingObserver();
        using var subscription = provider.Subscribe(observer);
        var pinnedPolicy = observer.Current("p1.allow");

        // act
        // Replacement, clearing the content, and provider disposal must only drop the provider's
        // own references. A policy pinned by an in-flight request owns the handle it needs.
        provider.OnNext(Config(Policy("p1", "v2")).Policies);
        provider.OnNext(null);
        await provider.DisposeAsync();
        var context = new RegoPolicyTestEntities.TestPolicyContext(
            entities: new CompositeResultElement[1]);
        await pinnedPolicy!.EvaluateAsync(context, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(context.DeniedIndices);
    }

    [Fact]
    public async Task Handle_Should_ReleaseProviderOwnedRoots_When_SnapshotIsReplaced()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(Config(Policy("p1", "v1")).Policies);
        var handle = CreateWeakHandleReference(provider);

        // act
        provider.OnNext(Config(Policy("p1", "v2")).Policies);
        ForceGarbageCollection();

        // assert
        Assert.False(handle.IsAlive);
    }

    [Fact]
    public async Task Handle_Should_ReleaseProviderOwnedRoots_When_EmptySnapshotIsPublished()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(Config(Policy("p1", "v1")).Policies);
        var handle = CreateWeakHandleReference(provider);

        // act
        provider.OnNext(null);
        ForceGarbageCollection();

        // assert
        Assert.False(handle.IsAlive);
    }

    [Fact]
    public async Task Handle_Should_ReleaseProviderOwnedRoots_When_ProviderIsDisposed()
    {
        // arrange
        await using var provider = new RegoPolicyProvider(new CapturingDiagnostics());
        provider.OnNext(Config(Policy("p1", "v1")).Policies);
        var handle = CreateWeakHandleReference(provider);

        // act
        await provider.DisposeAsync();
        ForceGarbageCollection();

        // assert
        Assert.False(handle.IsAlive);
    }

    private static PolicyContent Policy(string @base, string digest)
        => new(
            @base,
            PolicyContentType.Rego,
            Encoding.UTF8.GetBytes($"package {@base}\nimport rego.v1\ndefault allow := true\n"),
            PolicyRequirements.Empty,
            Encoding.UTF8.GetBytes(digest));

    private static PolicyContent Policy(string @base, string source, string digest)
        => new(
            @base,
            PolicyContentType.Rego,
            Encoding.UTF8.GetBytes(source),
            PolicyRequirements.Empty,
            Encoding.UTF8.GetBytes(digest));

    // The rule body is malformed, so the whole set fails to compile.
    private static PolicyContent Broken(string @base, string digest)
        => new(
            @base,
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateWeakHandleReference(RegoPolicyProvider provider)
    {
        RegoPolicy? policy = null;
        using var subscription = provider.Subscribe(
            new DelegatingObserver(value => policy = (RegoPolicy)value[0]));
        return new WeakReference(policy!.Handle);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ForceGarbageCollection()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
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

    private sealed class DelegatingObserver(Action<ImmutableArray<IPolicy>> onNext)
        : IObserver<ImmutableArray<IPolicy>>
    {
        public void OnNext(ImmutableArray<IPolicy> value) => onNext(value);

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
