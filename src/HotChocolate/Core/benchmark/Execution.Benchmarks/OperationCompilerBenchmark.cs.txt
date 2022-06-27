using System;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Execution.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class OperationCompilerBenchmark
{
    private readonly ISchema _schema;
    private readonly ISchema _cryptoSchema;
    private readonly DocumentNode _smallQuery;
    private readonly DocumentNode _largeQuery;
    private readonly DocumentNode _cryptoListQuery;
    private readonly DocumentNode _cryptoDetailQuery;
    private readonly OperationDefinitionNode _smallOp;
    private readonly OperationDefinitionNode _largeOp;
    private readonly OperationDefinitionNode _cryptoList;
    private readonly OperationDefinitionNode _cryptoDetail;
    private readonly OperationCompilerPool _compilerPool = new();

    public OperationCompilerBenchmark()
    {
        var resources = new ResourceHelper();

        _schema = SchemaBuilder.New()
            .AddStarWarsTypes()
            .Create();

        _smallQuery = Utf8GraphQLParser.Parse(
            @"{
                hero(episode: EMPIRE) {
                    name
                    ... abc
                    ... def
                }
              }

              fragment abc on Droid {
                  primaryFunction
              }

              fragment def on Human {
                  homePlanet
              }");
        _smallOp = _smallQuery.Definitions.OfType<OperationDefinitionNode>().Single();

        _largeQuery = Utf8GraphQLParser.Parse(resources.GetResourceString("LargeQuery.graphql"));
        _largeOp = _largeQuery.Definitions.OfType<OperationDefinitionNode>().Single();

        _cryptoSchema = SchemaBuilder.New()
            .AddDocumentFromString(resources.GetResourceString("Crypto.graphql"))
            .Use(next => next)
            .Create();

        _cryptoListQuery = Utf8GraphQLParser.Parse(resources.GetResourceString("CryptoQuery.graphql"));
        _cryptoList = _cryptoListQuery.Definitions.OfType<OperationDefinitionNode>().Single();

        _cryptoDetailQuery = Utf8GraphQLParser.Parse(resources.GetResourceString("CryptoDetailQuery.graphql"));
        _cryptoDetail = _cryptoDetailQuery.Definitions.OfType<OperationDefinitionNode>().Single();
    }

    [Benchmark]
    public IPreparedOperation HC12_SmallQuery_Compiler()
        => Execute_Compile_12(_smallOp, _smallQuery, _schema);

    [Benchmark]
    public IPreparedOperation2 HC13_SmallQuery_Compiler()
        => Execute_Compile_13(_smallOp, _smallQuery, _schema);

    [Benchmark]
    public IPreparedOperation HC12_LargeQuery_Compiler()
        => Execute_Compile_12(_largeOp, _largeQuery, _schema);

    [Benchmark]
    public IPreparedOperation2 HC13_LargeQuery_Compiler()
        => Execute_Compile_13(_largeOp, _largeQuery, _schema);

    [Benchmark]
    public IPreparedOperation HC12_CryptoDetail_Compiler()
        => Execute_Compile_12(_cryptoDetail, _cryptoDetailQuery, _cryptoSchema);

    [Benchmark]
    public IPreparedOperation2 HC13_CryptoDetail_Compiler()
        => Execute_Compile_13(_cryptoDetail, _cryptoDetailQuery, _cryptoSchema);

    [Benchmark]
    public IPreparedOperation HC12_CryptoList_Compiler()
        => Execute_Compile_12(_cryptoList, _cryptoListQuery, _cryptoSchema);

    [Benchmark]
    public IPreparedOperation2 HC13_CryptoList_Compiler()
        => Execute_Compile_13(_cryptoList, _cryptoListQuery, _cryptoSchema);

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IPreparedOperation Execute_Compile_12(
        OperationDefinitionNode op,
        DocumentNode doc,
        ISchema schema)
        => OperationCompiler.Compile(
            "a",
            doc,
            op,
            schema,
            schema.QueryType,
            new InputParser());

    [MethodImpl(MethodImplOptions.NoInlining)]
    private IPreparedOperation2 Execute_Compile_13(
        OperationDefinitionNode op,
        DocumentNode doc,
        ISchema schema)
    {
        var compiler = _compilerPool.Get();
        var operation =compiler.Compile("a", op, schema.QueryType, doc, schema);
        _compilerPool.Return(compiler);
        return operation;
    }


    private sealed class OperationCompilerPool : DefaultObjectPool<OperationCompiler2>
    {
        public OperationCompilerPool()
            : base(new OperationCompilerPolicy())
        {
        }

        private sealed class OperationCompilerPolicy : IPooledObjectPolicy<OperationCompiler2>
        {
            public OperationCompiler2 Create()
                => new(new InputParser());

            public bool Return(OperationCompiler2 obj)
                => true;
        }
    }
}


