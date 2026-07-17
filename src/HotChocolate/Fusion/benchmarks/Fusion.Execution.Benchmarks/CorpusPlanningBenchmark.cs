using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Rewriters;
using HotChocolate.Fusion.Planning;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Benchmarks;

// Planning benchmarks for the composed corpus (edge0-v2, 745 sources).
//
// One iteration is a full plan creation: OperationPlanner.CreatePlan compiles the
// rewritten operation and searches the plan space from scratch (there is no plan
// cache at this layer, so every iteration plans anew). The FusionSchemaDefinition,
// which is expensive to build on this corpus, is built once in GlobalSetup; its
// cost is reported separately on the console rather than folded into the per-plan
// numbers. See CorpusPlanningProbe for the single-shot feasibility profile.
[MemoryDiagnoser]
[Config(typeof(InProcessConfig))]
public class CorpusPlanningBenchmark
{
    private static readonly string SchemaPath = CorpusPaths.SchemaPath;
    private static readonly string Query1Path = CorpusPaths.Query1Path;
    private static readonly string Query2Path = CorpusPaths.Query2Path;

    // A name-safe, at-least-eight-character identifier: it is threaded into the
    // names of synthesized lookup operations (Op_{shortHash}_{stepId}) and sliced
    // as hash[..8] by the plan formatter, mirroring FusionTestBase.
    private const string OperationId = "123456789101112";

    private OperationPlanner _planner = null!;
    private OperationDefinitionNode _operation1 = null!;
    private OperationDefinitionNode _operation2 = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var schemaDoc = Utf8GraphQLParser.Parse(File.ReadAllText(SchemaPath));

        var sw = Stopwatch.StartNew();
        var schema = FusionSchemaDefinition.Create(schemaDoc);
        sw.Stop();
        Console.WriteLine($"[setup] FusionSchemaDefinition build: {sw.Elapsed.TotalMilliseconds:F1} ms");

        var rewriter = new DocumentRewriter(schema);
        var pool = new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(
            new DefaultPooledObjectPolicy<OrderedDictionary<string, List<FieldSelectionNode>>>());
        var compiler = new OperationCompiler(schema, pool);
        _planner = new OperationPlanner(schema, compiler);

        _operation1 = LoadOperation(rewriter, Query1Path);
        _operation2 = LoadOperation(rewriter, Query2Path);
    }

    [Benchmark]
    public int Plan_Query1()
        => _planner.CreatePlan(OperationId, OperationId, OperationId, _operation1).AllNodes.Length;

    [Benchmark]
    public int Plan_Query2()
        => _planner.CreatePlan(OperationId, OperationId, OperationId, _operation2).AllNodes.Length;

    private static OperationDefinitionNode LoadOperation(DocumentRewriter rewriter, string path)
    {
        var document = Utf8GraphQLParser.Parse(File.ReadAllText(path));
        return rewriter.RewriteDocument(document).GetOperation(operationName: null);
    }

    private sealed class InProcessConfig : ManualConfig
    {
        public InProcessConfig()
        {
            AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }
}
