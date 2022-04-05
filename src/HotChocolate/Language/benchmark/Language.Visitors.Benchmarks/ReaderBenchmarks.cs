using System.Text;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language.Visitors.Benchmarks.Resources;

namespace HotChocolate.Language.Visitors.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ReaderBenchmarks
{
    private readonly byte[] _introspectionBytes;
    private readonly byte[] _kitchenSinkSchemaBytes;
    private readonly byte[] _kitchenSinkBytes;

    public ReaderBenchmarks()
    {
        var resources = new ResourceHelper();
        var introspectionString = resources.GetResourceString("IntrospectionQuery.graphql");
        _introspectionBytes = Encoding.UTF8.GetBytes(introspectionString);
        var kitchenSinkSchemaString = resources.GetResourceString("schema-kitchen-sink.graphql");
        _kitchenSinkSchemaBytes = Encoding.UTF8.GetBytes(kitchenSinkSchemaString);
        var kitchenSinkString = resources.GetResourceString("kitchen-sink-nullability.graphql");
        _kitchenSinkBytes = Encoding.UTF8.GetBytes(kitchenSinkString);
    }

    [Benchmark]
    public void Introspection_Read_Bytes()
    {
        var reader = new Utf8GraphQLReader(_introspectionBytes);

        while (reader.Read())
        {
        }
    }

    [Benchmark]
    public void KitchenSink_Schema_Read_Bytes()
    {
        var reader = new Utf8GraphQLReader(_kitchenSinkSchemaBytes);

        while (reader.Read())
        {
        }
    }

    [Benchmark]
    public void KitchenSink_Query_Read_Bytes()
    {
        var reader = new Utf8GraphQLReader(_kitchenSinkBytes);

        while (reader.Read())
        {
        }
    }
}

