using System.IO;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Benchmarks;

// Parsing benchmarks for the composed corpus (745 sources).
//
// This isolates the document-parsing layer for the schema SDL and both operations.
// Full plan creation on the same corpus is measured by CorpusPlanningBenchmark
// (reachable now that PlannerTopologyCache scales to the 745-source graph).
[MemoryDiagnoser]
[Config(typeof(InProcessConfig))]
public class CorpusParsingBenchmark
{
    private static readonly string SchemaPath = CorpusPaths.SchemaPath;
    private static readonly string Query1Path = CorpusPaths.Query1Path;
    private static readonly string Query2Path = CorpusPaths.Query2Path;

    private byte[] _schemaSdl = null!;
    private byte[] _query1 = null!;
    private byte[] _query2 = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _schemaSdl = File.ReadAllBytes(SchemaPath);
        _query1 = File.ReadAllBytes(Query1Path);
        _query2 = File.ReadAllBytes(Query2Path);
    }

    [Benchmark]
    public int Parse_Schema_Sdl()
        => Utf8GraphQLParser.Parse(_schemaSdl).Definitions.Count;

    [Benchmark]
    public int Parse_Query1()
        => Utf8GraphQLParser.Parse(_query1).Definitions.Count;

    [Benchmark]
    public int Parse_Query2()
        => Utf8GraphQLParser.Parse(_query2).Definitions.Count;

    private sealed class InProcessConfig : ManualConfig
    {
        public InProcessConfig()
        {
            AddJob(Job.ShortRun.WithToolchain(InProcessEmitToolchain.Instance));
        }
    }
}
