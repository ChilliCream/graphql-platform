using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Errors;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Planning;

/// <summary>
/// Opt-in harness that composes the corpus in <c>FUSION_PLANNER_CORPUS_DIR</c> (replacing source schemas by
/// file name from <c>FUSION_PLANNER_CORPUS_OVERRIDE_DIR</c> when set) and plans every operation within
/// <c>FUSION_PLANNER_CORPUS_TIME_LIMIT_SECONDS</c> (default 120). The tests are skipped when the corpus
/// directory is not available, and the per-operation lines are shown by the test assembly with
/// <c>--xunit-diagnostics on</c> and are written to the report of <c>--report-trx</c>.
/// </summary>
public sealed class PlannerCorpusTests(PlannerCorpusTests.CorpusFixture corpus)
    : IClassFixture<PlannerCorpusTests.CorpusFixture>
{
    private const string DirectoryVariable = "FUSION_PLANNER_CORPUS_DIR";
    private const string TimeLimitVariable = "FUSION_PLANNER_CORPUS_TIME_LIMIT_SECONDS";
    private const string OverrideDirectoryVariable = "FUSION_PLANNER_CORPUS_OVERRIDE_DIR";

    private const string SkipReason =
        "Set FUSION_PLANNER_CORPUS_DIR to a corpus directory to run the planner corpus tests.";

    [Fact]
    public void Compose_Should_ProduceSchema_When_CorpusIsAvailable()
    {
        // arrange
        Assert.SkipWhen(!corpus.IsAvailable, SkipReason);

        // act
        var schema = corpus.Schema!;

        // assert
        Write(
            $"subgraphs {corpus.SubgraphCount}, types {schema.Types.Count}, "
            + $"composition {corpus.CompositionTime.TotalSeconds.ToString("F1", CultureInfo.InvariantCulture)} s");
        Assert.True(corpus.SubgraphCount > 0);
        Assert.NotEmpty(schema.Types);
    }

    [Fact]
    public void Plan_Should_ReportMeasurements_When_CorpusOperationsArePlanned()
    {
        // arrange
        Assert.SkipWhen(!corpus.IsAvailable, SkipReason);
        var schema = corpus.Schema!;
        var timeLimit = GetTimeLimit();
        var files = Directory
            .GetFiles(System.IO.Path.Combine(corpus.Directory!, "operations"), "*.graphql")
            .Order(StringComparer.Ordinal)
            .ToArray();
        Assert.NotEmpty(files);
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        using var listener = new PlannerPhaseListener();
        var lines = new List<string>();

        // act
        Write($"time limit {timeLimit.TotalSeconds.ToString("F0", CultureInfo.InvariantCulture)} s per operation");

        foreach (var file in files)
        {
            var name = System.IO.Path.GetFileNameWithoutExtension(file);
            var line = PlanOperation(schema, pool, listener, name, file, timeLimit);
            lines.Add(line);
            Write(line);
        }

        // assert
        Assert.Equal(files.Length, lines.Count);
        Assert.All(
            files.Zip(lines),
            pair => Assert.StartsWith(
                $"{System.IO.Path.GetFileNameWithoutExtension(pair.First)}: ",
                pair.Second,
                StringComparison.Ordinal));
    }

    private static string PlanOperation(
        FusionSchemaDefinition schema,
        ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> pool,
        PlannerPhaseListener listener,
        string name,
        string file,
        TimeSpan timeLimit)
    {
        var stopwatch = new Stopwatch();

        try
        {
            var rewritten = new DocumentRewriter(schema)
                .RewriteDocument(Utf8GraphQLParser.Parse(File.ReadAllText(file)), operationName: null);
            var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();
            var options = new OperationPlannerOptions { MaxPlanningTime = timeLimit };
            var planner = new OperationPlanner(schema, new OperationCompiler(schema, pool), options);
            var operationId = $"corpus_{name}";
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(
                TestContext.Current.CancellationToken);
            cts.CancelAfter(timeLimit * 2);
            listener.Begin(operationId);
            stopwatch.Start();

            var plan = planner.CreatePlan(operationId, operationId, operationId, operation, cts.Token);
            stopwatch.Stop();
            var measure = Measure(plan);

            return $"{name}: greedy {FormatGreedyTime(listener)}, total {FormatMilliseconds(stopwatch)}, "
                + $"expandedNodes {plan.ExpandedNodes}, searchSpace {plan.SearchSpace}, "
                + $"depth {measure.Depth}, steps {measure.Steps}, requests {measure.Requests}";
        }
        catch (OperationPlannerGuardrailException ex)
        {
            return $"{name}: greedy {FormatGreedyTime(listener)}, total {FormatMilliseconds(stopwatch)}, "
                + $"guardrail {ex.Reason} (limit {ex.Limit}, observed {ex.Observed})";
        }
        catch (OperationCanceledException) when (!TestContext.Current.CancellationToken.IsCancellationRequested)
        {
            return $"{name}: greedy {FormatGreedyTime(listener)}, total {FormatMilliseconds(stopwatch)}, canceled";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"{name}: failed {ex.GetType().Name}: {ex.Message.ReplaceLineEndings(" ")}";
        }
    }

    private static string FormatGreedyTime(PlannerPhaseListener listener)
        => listener.GreedyTime is { } greedyTime
            ? $"{greedyTime.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)} ms"
            : "n/a";

    private static string FormatMilliseconds(Stopwatch stopwatch)
        => $"{stopwatch.Elapsed.TotalMilliseconds.ToString("F0", CultureInfo.InvariantCulture)} ms";

    private static TimeSpan GetTimeLimit()
    {
        var value = Environment.GetEnvironmentVariable(TimeLimitVariable);

        return int.TryParse(value, CultureInfo.InvariantCulture, out var seconds) && seconds > 0
            ? TimeSpan.FromSeconds(seconds)
            : TimeSpan.FromSeconds(120);
    }

    private static void Write(string line)
    {
        TestContext.Current.TestOutputHelper?.WriteLine(line);
        TestContext.Current.SendDiagnosticMessage(line);
    }

    private static PlanMeasure Measure(OperationPlan plan)
    {
        var depthById = new Dictionary<int, int>();
        var steps = 0;
        var requests = 0;
        var maxDepth = 0;

        foreach (var node in plan.AllNodes)
        {
            switch (node.Type)
            {
                case ExecutionNodeType.Operation:
                    steps++;
                    requests++;
                    break;

                case ExecutionNodeType.OperationBatch:
                    steps += node switch
                    {
                        OperationBatchExecutionNode batch => batch.Operations.Length,
                        ApolloOperationBatchExecutionNode batch => batch.Operations.Length,
                        _ => throw new NotSupportedException(
                            $"The harness cannot measure a batch node of type {node.GetType().Name}.")
                    };
                    requests++;
                    break;

                case ExecutionNodeType.Introspection:
                    continue;

                default:
                    throw new NotSupportedException(
                        $"The harness cannot measure a node of type {node.Type}.");
            }

            maxDepth = Math.Max(maxDepth, GetDepth(plan, node, depthById));
        }

        return new PlanMeasure(maxDepth, steps, requests);
    }

    private static int GetDepth(OperationPlan plan, ExecutionNode node, Dictionary<int, int> depthById)
    {
        if (depthById.TryGetValue(node.Id, out var depth))
        {
            return depth;
        }

        depth = 1;

        foreach (var dependency in node.Dependencies)
        {
            var dependencyNode = plan.GetNodeById(dependency.Id);

            if (dependencyNode is IntrospectionExecutionNode)
            {
                continue;
            }

            depth = Math.Max(depth, GetDepth(plan, dependencyNode, depthById) + 1);
        }

        depthById[node.Id] = depth;
        return depth;
    }

    private readonly record struct PlanMeasure(int Depth, int Steps, int Requests);

    /// <summary>
    /// Composes the corpus once for all tests of the class, without the satisfiability pass.
    /// Stays empty when the corpus directory is not available.
    /// </summary>
    public sealed class CorpusFixture : IAsyncLifetime
    {
        public bool IsAvailable => Schema is not null;

        public string? Directory { get; private set; }

        public FusionSchemaDefinition? Schema { get; private set; }

        public int SubgraphCount { get; private set; }

        public TimeSpan CompositionTime { get; private set; }

        public ValueTask InitializeAsync()
        {
            var directory = Environment.GetEnvironmentVariable(DirectoryVariable);

            if (string.IsNullOrWhiteSpace(directory)
                || !System.IO.Directory.Exists(System.IO.Path.Combine(directory, "subgraphs"))
                || !System.IO.Directory.Exists(System.IO.Path.Combine(directory, "operations")))
            {
                return ValueTask.CompletedTask;
            }

            var overrideDirectory = Environment.GetEnvironmentVariable(OverrideDirectoryVariable);

            var files = System.IO.Directory
                .GetFiles(System.IO.Path.Combine(directory, "subgraphs"), "*.graphqls")
                .Order(StringComparer.Ordinal)
                .ToList();

            var sources = files
                .Select(file => new SourceSchemaText(
                    System.IO.Path.GetFileNameWithoutExtension(file),
                    File.ReadAllText(GetOverride(overrideDirectory, file) ?? file)))
                .ToList();

            var options = new SchemaComposerOptions();

            foreach (var source in sources)
            {
                options.SourceSchemas[source.Name] = new SourceSchemaOptions
                {
                    Preprocessor = new SourceSchemaPreprocessorOptions { InferKeysFromLookups = false }
                };
            }

            var log = new CompositionLog();
            var stopwatch = Stopwatch.StartNew();
            var result = ComposeWithoutSatisfiability(sources, options, log);
            stopwatch.Stop();

            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(FormatCompositionFailure(log, result.Errors, files));
            }

            Directory = directory;
            SubgraphCount = sources.Count;
            CompositionTime = stopwatch.Elapsed;
            Schema = FusionSchemaDefinition.Create(result.Value.ToSyntaxNode());

            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private static string FormatCompositionFailure(
            CompositionLog log,
            ImmutableArray<CompositionError> resultErrors,
            List<string> files)
        {
            var fileNames = files.ToDictionary(
                file => System.IO.Path.GetFileNameWithoutExtension(file.AsSpan()).ToString(),
                file => System.IO.Path.GetFileName(file.AsSpan()).ToString(),
                StringComparer.Ordinal);

            var lines = log
                .Where(entry => entry.Severity == LogSeverity.Error)
                .Select(entry =>
                {
                    var schema = entry.Schema is { } source
                        ? $" (source schema file {fileNames.GetValueOrDefault(source.Name, source.Name)})"
                        : string.Empty;

                    return $"{entry.Code}: {entry.Message.ReplaceLineEndings(" ")}{schema}";
                })
                .Concat(resultErrors.Select(error => error.Message.ReplaceLineEndings(" ")))
                .ToList();

            return $"The corpus did not compose, {lines.Count} errors:{Environment.NewLine}"
                + string.Join(Environment.NewLine, lines);
        }

        private static string? GetOverride(string? overrideDirectory, string file)
        {
            if (string.IsNullOrWhiteSpace(overrideDirectory))
            {
                return null;
            }

            var path = System.IO.Path.Combine(overrideDirectory, System.IO.Path.GetFileName(file));

            return File.Exists(path) ? path : null;
        }
    }

    // Mirrors SchemaComposer.Compose() for default options and stops before the satisfiability pass.
    private static CompositionResult<MutableSchemaDefinition> ComposeWithoutSatisfiability(
        List<SourceSchemaText> sourceSchemas,
        SchemaComposerOptions composerOptions,
        CompositionLog log)
    {
        var parsingResult = sourceSchemas.Select(schema =>
        {
            var options = composerOptions.SourceSchemas.GetValueOrDefault(schema.Name);
            return new SourceSchemaParser(
                schema,
                log,
                options?.Parser,
                options?.InvalidFieldDeprecationSeverity ?? LogSeverity.Warning,
                options?.IsApolloFederationV1 ?? false).Parse();
        }).Combine();

        if (parsingResult.IsFailure)
        {
            return parsingResult.Errors;
        }

        var schemas = parsingResult.Value.ToImmutableSortedSet(
            new SchemaByNameComparer<MutableSchemaDefinition>());

        var preprocessingResult = schemas.Select(schema =>
        {
            var options = composerOptions.SourceSchemas.GetValueOrDefault(schema.Name);
            return new SourceSchemaPreprocessor(
                schema,
                schemas,
                log,
                options?.Version,
                options?.Preprocessor,
                options?.InvalidFieldDeprecationSeverity ?? LogSeverity.Warning,
                options?.IsApolloFederationV1 ?? false).Preprocess();
        }).Combine();

        if (preprocessingResult.IsFailure)
        {
            return preprocessingResult.Errors;
        }

        var enrichmentResult = schemas
            .Select(schema => new SourceSchemaEnricher(schema, schemas).Enrich())
            .Combine();

        if (enrichmentResult.IsFailure)
        {
            return enrichmentResult.Errors;
        }

        if (composerOptions.Merger.RemoveUnreferencedDefinitions)
        {
            var preservedTypeNames = MutableSchemaDefinitionExtensions.GetPreservedTypeNames(schemas);

            foreach (var schema in schemas)
            {
                schema.RemoveUnreferencedDefinitions(preservedTypeNames, seedUnionsAsRoots: true);
            }
        }

        var validationResult =
            new SourceSchemaValidator(schemas, SchemaComposer.SourceSchemaRules, log).Validate();

        if (validationResult.IsFailure)
        {
            return validationResult;
        }

        var preMergeValidationResult = new PreMergeValidator(
            schemas,
            SchemaComposer.CreatePreMergeRules(composerOptions),
            log).Validate();

        if (preMergeValidationResult.IsFailure)
        {
            return preMergeValidationResult;
        }

        var (_, isMergeFailure, mergedSchema, mergeErrors) = new SourceSchemaMerger(
            schemas,
            composerOptions.Merger,
            composerOptions.ApolloFederationCompatibility.ShareableFieldRuntimeTypeRouting).Merge();

        if (isMergeFailure)
        {
            return mergeErrors;
        }

        var postMergeValidationResult =
            new PostMergeValidator(mergedSchema, SchemaComposer.PostMergeRules, schemas, log).Validate();

        if (postMergeValidationResult.IsFailure)
        {
            return postMergeValidationResult;
        }

        return mergedSchema;
    }

    private sealed class PlannerPhaseListener : EventListener
    {
        private string? _operationId;
        private long _startTimestamp;
        private long _firstDequeueTimestamp;

        public TimeSpan? GreedyTime
            => Volatile.Read(ref _firstDequeueTimestamp) is not 0 and var first
                ? Stopwatch.GetElapsedTime(_startTimestamp, first)
                : null;

        public void Begin(string operationId)
        {
            _startTimestamp = Stopwatch.GetTimestamp();
            Volatile.Write(ref _firstDequeueTimestamp, 0);
            Volatile.Write(ref _operationId, operationId);
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name.Equals("HotChocolate-Fusion-Planner", StringComparison.Ordinal))
            {
                EnableEvents(eventSource, EventLevel.Verbose);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.Payload is not { Count: > 0 } payload
                || payload[0] is not string operationId
                || !operationId.Equals(Volatile.Read(ref _operationId), StringComparison.Ordinal))
            {
                return;
            }

            var timestamp = Stopwatch.GetTimestamp();

            switch (eventData.EventId)
            {
                case 1:
                    _startTimestamp = timestamp;
                    break;

                case 4 when Volatile.Read(ref _firstDequeueTimestamp) == 0:
                    Volatile.Write(ref _firstDequeueTimestamp, timestamp);
                    break;
            }
        }
    }
}
