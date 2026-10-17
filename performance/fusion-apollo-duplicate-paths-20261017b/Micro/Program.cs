using System.Buffers;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Execution;
using HotChocolate.Execution.Errors;
using HotChocolate.Fusion;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.ApolloFederation;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Text.Json;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

if (args.Length != 5)
{
    throw new ArgumentException("Arguments: check|timing|allocation case|all warmup_requests observations requests_per_observation");
}

var mode = args[0];
var warmup = int.Parse(args[2]);
var observations = int.Parse(args[3]);
var requests = int.Parse(args[4]);
if (warmup < 0 || observations < 1 || requests < 1 || mode is not ("check" or "timing" or "allocation"))
{
    throw new ArgumentException("Invalid explicit benchmark settings.");
}

var cases = new Dictionary<string, (int Count, int Unique)[]>
{
    ["unique1"] = [(1, 1)],
    ["unique5"] = [(5, 5)],
    ["unique6"] = [(6, 6)],
    ["unique9"] = [(9, 9)],
    ["duplicate96"] = [(96, 1)],
    ["duplicate1026"] = [(1026, 1)],
    ["unique96"] = [(96, 96)],
    ["unique1024"] = [(1024, 1024)],
    ["mixed96"] = [(96, 8)],
    ["testquery_sequence"] = [(5, 5), (6, 6), (5, 5), (12, 1), (96, 1), (22, 1), (48, 1), (11, 1), (12, 1), (96, 1), (22, 1)],
    ["large_then_small"] = [(1024, 1024), (1, 1), (96, 1), (5, 5)],
    ["snapshot96"] = [(96, 8)],
    ["null96"] = [(96, 0)]
};
var chosen = args[1] == "all" ? cases : new Dictionary<string, (int Count, int Unique)[]> { [args[1]] = cases[args[1]] };
var assembly = typeof(RepresentationValue).Assembly;
var results = new List<object>();
long sink = 0;
foreach (var pair in chosen)
{
    using var fixture = new Fixture(pair.Value, pair.Key.StartsWith("snapshot", StringComparison.Ordinal));
    fixture.Prepare();
    var oracle = fixture.Check();
    fixture.Clean();
    fixture.ReleaseArenas();
    if (mode == "check")
    {
        results.Add(new { @case = pair.Key, oracle });
        continue;
    }

    for (var i = 0; i < warmup; i++)
    {
        fixture.Prepare();
        sink ^= fixture.Execute();
        fixture.Clean();
        fixture.ReleaseArenas();
    }

    var values = new double[observations];
    long calibrationBefore = 0;
    long calibrationAfter = 0;
    var gcBefore = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
    var noGc = mode == "allocation";
    if (noGc && !GC.TryStartNoGCRegion(512L * 1024 * 1024, disallowFullBlockingGC: false))
    {
        throw new InvalidOperationException("Cannot establish the controlled allocation region.");
    }

    var gcRegionStart = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
    try
    {
        if (noGc)
        {
            calibrationBefore = Calibrate();
        }

        for (var sample = 0; sample < observations; sample++)
        {
            long total = 0;
            for (var i = 0; i < requests; i++)
            {
                fixture.Prepare();
                var start = noGc ? GC.GetAllocatedBytesForCurrentThread() : Stopwatch.GetTimestamp();
                sink ^= fixture.Execute();
                fixture.Clean();
                var end = noGc ? GC.GetAllocatedBytesForCurrentThread() : Stopwatch.GetTimestamp();
                if (end < start)
                {
                    throw new InvalidOperationException("Counter decreased.");
                }
                total += end - start;
                fixture.ReleaseArenas();
            }
            values[sample] = noGc ? (double)total / requests : total * 1_000_000_000.0 / Stopwatch.Frequency / requests;
        }

        if (noGc)
        {
            calibrationAfter = Calibrate();
            if (Enumerable.Range(0, 3).Any(i => GC.CollectionCount(i) != gcRegionStart[i]))
            {
                throw new InvalidOperationException("Collection occurred inside the controlled allocation region.");
            }
        }
    }
    finally
    {
        if (noGc)
        {
            GC.EndNoGCRegion();
        }
    }

    results.Add(new
    {
        @case = pair.Key, oracle, values, arithmeticMean = values.Average(),
        unit = noGc ? "bytes/request" : "ns/request", calibrationBefore, calibrationAfter,
        gcBefore, gcAfter = new[] { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) }
    });
}

Console.WriteLine(JsonSerializer.Serialize(new
{
    utc = DateTimeOffset.UtcNow, mode, warmup, observations, requests, sink,
    runtime = Environment.Version.ToString(), serverGc = GCSettings.IsServerGC,
    assembly = assembly.Location, mvid = assembly.ManifestModule.ModuleVersionId,
    sha256 = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(assembly.Location))),
    structureSizes = new { compactPath = Unsafe.SizeOf<CompactPath>(), variableValues = Unsafe.SizeOf<VariableValues>(), entityResultPath = Unsafe.SizeOf<EntityResultPath>() },
    results
}, new JsonSerializerOptions { WriteIndented = true }));

[MethodImpl(MethodImplOptions.NoInlining)]
static long Calibrate()
{
    var start = GC.GetAllocatedBytesForCurrentThread();
    for (var i = 0; i < 127; i++)
    {
        GC.KeepAlive(new byte[4096]);
    }
    var bytes = GC.GetAllocatedBytesForCurrentThread() - start;
    if (bytes != 127L * (4096 + 24))
    {
        throw new InvalidOperationException($"Allocation calibration failed: {bytes}.");
    }
    return bytes;
}

sealed class Fixture : IDisposable
{
    private readonly FusionSchemaDefinition _schema;
    private readonly OperationPlan _plan;
    private readonly OperationExecutionNode _node;
    private readonly FetchResultStore _store = new();
    private readonly FetchResultStore _snapshotSource = new();
    private readonly byte[] _payload;
    private readonly OperationRequirement[] _requirements;
    private readonly ImmutableArray<RepresentationShapeNode> _shape;
    private readonly SelectionPath[] _paths;
    private readonly ImmutableArray<VariableValues>[] _snapshots;
    private readonly HashSet<string> _importedKeys = new(["__fusion_id"], StringComparer.Ordinal);
    private readonly bool _snapshot;
    private readonly int[] _expectedUnique;
    private readonly int[] _expectedPaths;
    private MemoryArena? _resultArena;
    private MemoryArena? _sourceArena;

    public Fixture((int Count, int Unique)[] dimensions, bool snapshot)
    {
        _snapshot = snapshot;
        var source = "type Query { " + string.Join(" ", dimensions.Select((_, i) => $"f{i}: [Foo]")) + " } type Foo { id: ID }";
        var log = new CompositionLog();
        var composed = new SchemaComposer([new SourceSchemaText("test", source)], new SchemaComposerOptions(), log).Compose();
        if (!composed.IsSuccess)
        {
            throw new InvalidOperationException(composed.Errors[0].Message);
        }
        _schema = FusionSchemaDefinition.Create(composed.Value.ToSyntaxNode());
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var text = "{ " + string.Join(" ", dimensions.Select((_, i) => $"f{i} {{ id }}")) + " }";
        var rewritten = new DocumentRewriter(_schema).RewriteDocument(Utf8GraphQLParser.Parse(text));
        var definition = rewritten.Definitions.OfType<OperationDefinitionNode>().Single();
        _plan = new OperationPlanner(_schema, new OperationCompiler(_schema, pool)).CreatePlan("micro", "micro", "micro", definition);
        _node = (OperationExecutionNode)_plan.RootNodes.Single();
        _paths = dimensions.Select((_, i) => SelectionPath.Root.AppendField($"f{i}")).ToArray();
        _requirements = [new OperationRequirement("__fusion_id", new NonNullTypeNode(new NamedTypeNode("ID")), SelectionPath.Root, new HotChocolate.Fusion.Language.FieldSelectionMapParser("id").Parse(), null)];
        var lookup = (FieldNode)Utf8GraphQLParser.Parse("{ fooById(id: $__fusion_id) { id } }").Definitions.OfType<OperationDefinitionNode>().Single().SelectionSet.Selections[0];
        _shape = RepresentationShapeBuilder.Build(lookup, _requirements, _schema, "Foo");
        var data = new Dictionary<string, object>();
        _snapshots = new ImmutableArray<VariableValues>[dimensions.Length];
        _expectedUnique = dimensions.Select(x => x.Unique).ToArray();
        _expectedPaths = dimensions.Select(x => x.Unique == 0 ? 0 : x.Count).ToArray();
        for (var i = 0; i < dimensions.Length; i++)
        {
            var (count, unique) = dimensions[i];
            var values = Enumerable.Range(0, count).Select(j => new { id = unique == 0 ? null : (j % unique).ToString() }).ToArray();
            data[$"f{i}"] = values;
            _snapshots[i] = values.Select((value, j) => _snapshotSource.CreateVariableValueSets(
                new CompactPath([1, ~j]), [new ObjectFieldNode("__fusion_id", value.id is null ? NullValueNode.Default : new StringValueNode(value.id))])).ToImmutableArray();
        }
        _payload = JsonSerializer.SerializeToUtf8Bytes(new { data });
    }

    public void Prepare()
    {
        _resultArena = new MemoryArena();
        _sourceArena = new MemoryArena();
        _store.Initialize(_resultArena, _schema, DefaultErrorHandler.Default, _plan.Operation, ErrorHandlingMode.Propagate, default, default, 16);
        var document = SourceResultDocument.Parse(_sourceArena, _payload, _payload.Length);
        if (!_store.AddPartialResult(SelectionPath.Root, new SourceSchemaResult(CompactPath.Root, document), _node.ResultSelectionSet, false))
        {
            throw new InvalidOperationException("Source result preparation failed.");
        }
    }

    private RepresentationValue Build(int index)
        => _snapshot
            ? _store.CreateRepresentationVariableValueFromSnapshot(_snapshots[index], _importedKeys, [], _requirements, _shape, "Foo")
            : _store.CreateRepresentationVariableValue(_paths[index], [], _requirements, _shape, "Foo");

    public object[] Check()
    {
        var observations = new object[_paths.Length];
        for (var i = 0; i < _paths.Length; i++)
        {
            var value = Build(i);
            var paths = value.ResultPaths.IsDefault ? 0 : value.ResultPaths.Sum(path => 1 + path.AdditionalPaths.Length);
            var distinct = value.ResultPaths.IsDefault ? 0 : value.ResultPaths.Length;
            if (paths != _expectedPaths[i] || distinct != _expectedUnique[i])
            {
                throw new InvalidOperationException("Representation path cardinality mismatch.");
            }
            var json = value.IsEmpty ? "" : Encoding.UTF8.GetString(value.Value.AsSequence().ToArray());
            observations[i] = new { distinct, paths, json };
        }
        return observations;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public long Execute()
    {
        long sink = 0;
        for (var i = 0; i < _paths.Length; i++)
        {
            var value = Build(i);
            sink += value.IsEmpty ? 0 : value.Value.Length + value.ResultPaths.Length;
            if (!value.ResultPaths.IsDefault)
            {
                foreach (var path in value.ResultPaths)
                {
                    sink += path.Path.Length + path.AdditionalPaths.Length;
                    foreach (var additional in path.AdditionalPaths)
                    {
                        sink += additional.Length;
                    }
                }
            }
        }
        return sink;
    }

    public void Clean() => _store.Clean(256, 256);

    public void ReleaseArenas()
    {
        _sourceArena?.Dispose();
        _resultArena?.Dispose();
        _sourceArena = null;
        _resultArena = null;
    }

    public void Dispose()
    {
        _store.Dispose();
        _snapshotSource.Dispose();
        ReleaseArenas();
    }
}
