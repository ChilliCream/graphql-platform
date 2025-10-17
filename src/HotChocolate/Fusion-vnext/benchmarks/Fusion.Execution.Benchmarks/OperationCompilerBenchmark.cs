using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.dotMemory;
using BenchmarkDotNet.Jobs;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Language;
using Microsoft.Extensions.ObjectPool;

namespace Fusion.Execution.Benchmarks;

[DotMemoryDiagnoser]
[MemoryDiagnoser]
[ShortRunJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class OperationCompilerBenchmark : FusionBenchmarkBase
{
    private const string Id = "123456789101112";

    private OperationCompiler _compiler = null!;

    private OperationDefinitionNode _simpleQueryWithRequirements = null!;
    private OperationDefinitionNode _complexQuery = null!;
    private OperationDefinitionNode _conditionalRedundancyQuery = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var schema = CreateFusionSchema();

        var documentRewriter = new DocumentRewriter(schema);

        _simpleQueryWithRequirements = documentRewriter.RewriteOperation(CreateSimpleQueryWithRequirementsDocument());
        _complexQuery = documentRewriter.RewriteOperation(CreateComplexDocument());
        _conditionalRedundancyQuery = documentRewriter.RewriteOperation(CreateConditionalRedundancyDocument());

        var pool = new NoOpObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>();
        _compiler = new OperationCompiler(schema, pool);
    }

    [Benchmark]
    public Operation Compile_Simple_Query_With_Requirements()
    {
        return _compiler.Compile(Id, Id, _simpleQueryWithRequirements);
    }

    [Benchmark]
    public Operation Compile_Complex_Query()
    {
        return _compiler.Compile(Id, Id, _complexQuery);
    }

    [Benchmark]
    public Operation Compile_ConditionalRedundancy_Query()
    {
        return _compiler.Compile(Id, Id, _conditionalRedundancyQuery);
    }

    private sealed class NoOpObjectPool<T> : ObjectPool<T> where T : class, new()
    {
        public override T Get()
        {
            return new T();
        }

        public override void Return(T obj)
        {
        }
    }
}
