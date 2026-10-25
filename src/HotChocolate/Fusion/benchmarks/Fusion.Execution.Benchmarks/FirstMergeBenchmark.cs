using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using BenchmarkDotNet.Attributes;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution.Benchmarks;

/// <summary>
/// Parses the captured subgraph responses of the federation benchmark query and merges them
/// into a fresh result store with the real operation plan: the two root results and the
/// reviews lookup results (composite: 11 JSON Lines records, Apollo: two <c>_entities</c>
/// responses). <see cref="ParseOnly"/> tokenizes the bodies, <see cref="MergeOnly"/> merges
/// pre-parsed documents, <see cref="ParseAndMerge"/> does both.
/// </summary>
[MemoryDiagnoser]
public class FirstMergeBenchmark
{
    private const string Composite = "composite";
    private const string Fed = "fed";

    private FusionSchemaDefinition _schema = null!;
    private OperationPlan _plan = null!;
    private MemoryArena _parsedArena = null!;
    private RootResult[] _rootResults = null!;
    private RecordResult[] _records = null!;
    private EntityResult[] _entities = null!;
    private byte[][] _bodies = null!;

    [Params(Composite, Fed)]
    public string Family { get; set; } = Composite;

    [GlobalSetup]
    public void Setup()
    {
        var resources = GetResourceDirectory();
        var schemaFile = Family == Composite ? "composite-gateway.graphqls" : "fed-gateway.graphqls";
        _schema = FusionSchemaDefinition.Create(
            Utf8GraphQLParser.Parse(File.ReadAllText(System.IO.Path.Combine(resources, schemaFile))));
        _plan = Plan(_schema, File.ReadAllText(System.IO.Path.Combine(resources, "benchmark-query.graphql")));
        _parsedArena = new MemoryArena();

        var bodies = new List<byte[]>();

        if (Family == Composite)
        {
            var users = File.ReadAllBytes(System.IO.Path.Combine(resources, "composite-users.json"));
            var topProducts = File.ReadAllBytes(System.IO.Path.Combine(resources, "composite-topProducts.json"));
            var reviews = File.ReadAllBytes(System.IO.Path.Combine(resources, "composite-reviews.jsonl"));
            bodies.Add(users);
            bodies.Add(topProducts);

            _rootResults =
            [
                new RootResult(FindRootNode("users"), users, Parse(users)),
                new RootResult(FindRootNode("topProducts"), topProducts, Parse(topProducts))
            ];

            var usersNode = FindLookupNode("$.users");
            var productsNode = FindLookupNode("$.topProducts");
            var records = new List<RecordResult>();

            foreach (var line in SplitLines(reviews))
            {
                bodies.Add(line);
                using var envelope = JsonDocument.Parse(line);
                var requestIndex = envelope.RootElement.GetProperty("requestIndex").GetInt32();
                var variableIndex = envelope.RootElement.GetProperty("variableIndex").GetInt32();
                var node = requestIndex == 0 ? usersNode : productsNode;
                records.Add(new RecordResult(node, requestIndex, variableIndex, line, Parse(line)));
            }

            _records = records.ToArray();
            _entities = [];
        }
        else
        {
            var users = File.ReadAllBytes(System.IO.Path.Combine(resources, "fed-users.json"));
            var topProducts = File.ReadAllBytes(System.IO.Path.Combine(resources, "fed-topProducts.json"));
            var reviewsUsers = File.ReadAllBytes(System.IO.Path.Combine(resources, "fed-reviews-users.json"));
            var reviewsProducts = File.ReadAllBytes(System.IO.Path.Combine(resources, "fed-reviews-products.json"));
            bodies.Add(users);
            bodies.Add(topProducts);
            bodies.Add(reviewsUsers);
            bodies.Add(reviewsProducts);

            _rootResults =
            [
                new RootResult(FindRootNode("users"), users, Parse(users)),
                new RootResult(FindRootNode("topProducts"), topProducts, Parse(topProducts))
            ];
            _records = [];
            _entities =
            [
                new EntityResult(FindApolloNode("$.users"), "users", reviewsUsers, Parse(reviewsUsers)),
                new EntityResult(FindApolloNode("$.topProducts"), "topProducts", reviewsProducts, Parse(reviewsProducts))
            ];
        }

        _bodies = bodies.ToArray();

        // The merge must produce the same document from parsed and freshly parsed inputs.
        var merged = RenderMerge(reuseParsedDocuments: true);
        var parsedAndMerged = RenderMerge(reuseParsedDocuments: false);

        if (merged != parsedAndMerged)
        {
            throw new InvalidOperationException("The parse-and-merge result differs from the merge-only result.");
        }

        if (!merged.Contains("\"reviews\":[{", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("The merged document holds no reviews: " + merged);
        }
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        foreach (var root in _rootResults)
        {
            root.Document.Dispose();
        }

        foreach (var record in _records)
        {
            record.Document.Dispose();
        }

        foreach (var entity in _entities)
        {
            entity.Document.Dispose();
        }

        _parsedArena.Dispose();
    }

    [Benchmark]
    public int ParseOnly()
    {
        using var arena = new MemoryArena();
        var rows = 0;

        foreach (var body in _bodies)
        {
            using var document = SourceResultDocument.Parse(arena, body, body.Length);
            rows += document.Root.GetPropertyCount();
        }

        return rows;
    }

    [Benchmark]
    public int MergeOnly()
    {
        using var arena = new MemoryArena();
        using var store = CreateStore(arena);
        return Merge(store, reuseParsedDocuments: true);
    }

    [Benchmark]
    public int ParseAndMerge()
    {
        using var arena = new MemoryArena();
        using var store = CreateStore(arena);
        return Merge(store, reuseParsedDocuments: false);
    }

    private int Merge(FetchResultStore store, bool reuseParsedDocuments)
    {
        var arena = reuseParsedDocuments ? null : new MemoryArena();

        try
        {
            foreach (var root in _rootResults)
            {
                var document = reuseParsedDocuments ? Reset(root.Document) : Parse(arena!, root.Body);
                store.AddPartialResult(
                    root.Node.Source,
                    CreateResult(CompactPath.Root, document, reuseParsedDocuments),
                    root.Node.ResultSelectionSet,
                    containsErrors: false);
            }

            var count = 0;

            if (_records.Length > 0)
            {
                var usersPaths = store.GetResultPaths(SelectionPath.Root.AppendField("users"));
                var productsPaths = store.GetResultPaths(SelectionPath.Root.AppendField("topProducts"));
                var results = new SourceSchemaResult[_records.Length];
                var start = 0;

                for (var i = 0; i < _records.Length; i++)
                {
                    var record = _records[i];
                    var document = reuseParsedDocuments ? Reset(record.Document) : Parse(arena!, record.Body);
                    var path = record.RequestIndex == 0
                        ? usersPaths[record.VariableIndex]
                        : productsPaths[record.VariableIndex];
                    results[i] = CreateResult(path, document, reuseParsedDocuments);

                    // Records of one node are merged as one batch, like the executor does.
                    if (i + 1 == _records.Length || _records[i + 1].Node != record.Node)
                    {
                        store.AddPartialResults(
                            record.Node.Source,
                            results.AsSpan(start, i + 1 - start),
                            record.Node.ResultSelectionSet,
                            containsErrors: false);
                        count += i + 1 - start;
                        start = i + 1;
                    }
                }
            }

            foreach (var entity in _entities)
            {
                var paths = store.GetResultPaths(SelectionPath.Root.AppendField(entity.TargetField));
                var resultPaths = ImmutableArray.CreateBuilder<EntityResultPath>(paths.Length);

                foreach (var path in paths)
                {
                    resultPaths.Add(new EntityResultPath(path, default));
                }

                var document = reuseParsedDocuments ? Reset(entity.Document) : Parse(arena!, entity.Body);
                store.AddRepresentationResult(
                    entity.Node.Source,
                    CreateResult(CompactPath.Root, document, reuseParsedDocuments),
                    new RepresentationValue(default, resultPaths.MoveToImmutable()),
                    entity.Node.ResultSelectionSet,
                    containsErrors: false);
                count += paths.Length;
            }

            return count;
        }
        finally
        {
            arena?.Dispose();
        }
    }

    private string RenderMerge(bool reuseParsedDocuments)
    {
        using var arena = new MemoryArena();
        using var store = CreateStore(arena);
        Merge(store, reuseParsedDocuments);
        using var buffer = new PooledArrayWriter();
        store.Result.Data.WriteTo(buffer);
        return System.Text.Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private FetchResultStore CreateStore(MemoryArena arena)
    {
        var store = new FetchResultStore();
        store.Initialize(
            arena,
            _schema,
            DefaultErrorHandler.Default,
            _plan.Operation,
            ErrorHandlingMode.Propagate,
            includeFlags: default,
            deferFlags: default,
            pathSegmentLocalPoolCapacity: 64);
        return store;
    }

    private static SourceSchemaResult CreateResult(
        CompactPath path,
        SourceResultDocument document,
        bool reuseParsedDocuments)
        => reuseParsedDocuments
            ? new SourceSchemaResult(path, document, KeepAlive(document))
            : new SourceSchemaResult(path, document);

    private SourceResultDocument Parse(byte[] body) => Parse(_parsedArena, body);

    private static SourceResultDocument Parse(MemoryArena arena, byte[] body)
        => SourceResultDocument.Parse(arena, body, body.Length);

    /// <summary>
    /// A parsed document is bound to the result document that first references it, so a
    /// reused document is unbound before every merge.
    /// </summary>
    private static SourceResultDocument Reset(SourceResultDocument document)
    {
        document.Id = -1;
        return document;
    }

    private MergeTarget FindRootNode(string rootField)
    {
        foreach (var target in EnumerateMergeTargets())
        {
            if (target.Target.IsRoot && target.ResultSelectionSet.ResponseNames.Contains(rootField))
            {
                return target;
            }
        }

        throw new InvalidOperationException($"No root node selects '{rootField}'.");
    }

    private MergeTarget FindLookupNode(string target)
    {
        foreach (var candidate in EnumerateMergeTargets())
        {
            if (!candidate.IsRepresentation
                && candidate.Target.ToString() == target
                && candidate.ResultSelectionSet.ResponseNames.Contains("reviews"))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"No reviews lookup node targets '{target}'.");
    }

    private MergeTarget FindApolloNode(string target)
    {
        foreach (var candidate in EnumerateMergeTargets())
        {
            if (candidate.IsRepresentation
                && candidate.Target.ToString() == target
                && candidate.ResultSelectionSet.ResponseNames.Contains("reviews"))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException($"No Apollo reviews lookup node targets '{target}'.");
    }

    /// <summary>
    /// Lists the merge inputs of every operation of the plan the way its node applies them:
    /// the source path and the result selection set, and whether the result is a list of entities.
    /// </summary>
    private IEnumerable<MergeTarget> EnumerateMergeTargets()
    {
        var entitiesSource = SelectionPath.Root.AppendField("_entities");

        foreach (var node in _plan.AllNodes)
        {
            switch (node)
            {
                case OperationExecutionNode operationNode:
                    yield return new MergeTarget(
                        operationNode.Target,
                        operationNode.Source,
                        operationNode.ResultSelectionSet,
                        IsRepresentation: false);
                    break;

                case ApolloOperationExecutionNode apolloNode:
                    yield return new MergeTarget(
                        apolloNode.Target,
                        entitiesSource,
                        apolloNode.ResultSelectionSet,
                        IsRepresentation: true);
                    break;

                case OperationBatchExecutionNode batchNode:
                    foreach (var operation in batchNode.Operations.ToArray())
                    {
                        if (operation is SingleOperationDefinition single)
                        {
                            yield return new MergeTarget(
                                single.Target,
                                single.Source,
                                single.ResultSelectionSet,
                                IsRepresentation: false);
                        }
                    }

                    break;

                case ApolloOperationBatchExecutionNode apolloBatchNode:
                    foreach (var operation in apolloBatchNode.Operations.ToArray())
                    {
                        if (operation is SingleOperationDefinition single)
                        {
                            yield return new MergeTarget(
                                single.Target,
                                entitiesSource,
                                single.ResultSelectionSet,
                                IsRepresentation: true);
                        }
                    }

                    break;
            }
        }
    }

    private static OperationPlan Plan(FusionSchemaDefinition schema, string queryText)
    {
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var document = Utf8GraphQLParser.Parse(queryText);
        var rewritten = new DocumentRewriter(schema).RewriteDocument(document, operationName: "TestQuery");
        var operation = rewritten.Definitions.OfType<OperationDefinitionNode>().First();
        var compiler = new OperationCompiler(schema, pool);
        var planner = new OperationPlanner(schema, compiler);
        return planner.CreatePlan("TestQuery", "TestQuery", "TestQuery", operation);
    }

    private static IEnumerable<byte[]> SplitLines(byte[] body)
    {
        var start = 0;

        for (var i = 0; i < body.Length; i++)
        {
            if (body[i] == (byte)'\n')
            {
                if (i > start)
                {
                    yield return body.AsSpan(start, i - start).ToArray();
                }

                start = i + 1;
            }
        }

        if (start < body.Length)
        {
            yield return body.AsSpan(start).ToArray();
        }
    }

    private static string GetResourceDirectory([CallerFilePath] string sourceFile = "")
        => System.IO.Path.Combine(System.IO.Path.GetDirectoryName(sourceFile)!, "__resources__", "merge-program");

    private sealed record MergeTarget(
        SelectionPath Target,
        SelectionPath Source,
        ResultSelectionSet ResultSelectionSet,
        bool IsRepresentation);

    private sealed record RootResult(MergeTarget Node, byte[] Body, SourceResultDocument Document);

    private sealed record RecordResult(
        MergeTarget Node,
        int RequestIndex,
        int VariableIndex,
        byte[] Body,
        SourceResultDocument Document);

    private sealed record EntityResult(
        MergeTarget Node,
        string TargetField,
        byte[] Body,
        SourceResultDocument Document);

    /// <summary>
    /// Keeps a document parsed once alive across merges: the owner never reaches zero references.
    /// </summary>
    private static SourceResultDocumentOwner KeepAlive(SourceResultDocument document)
        => new(document, int.MaxValue);
}
