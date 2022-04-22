using System.Text;
using BenchmarkDotNet.Attributes;
using HotChocolate.Language.Visitors.Benchmarks.Resources;

namespace HotChocolate.Language.Visitors.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class ParserBenchmarks
{
    private readonly byte[] _introspectionBytes;
    private readonly string _introspectionString;
    private readonly byte[] _kitchenSinkSchemaBytes;
    private readonly string _kitchenSinkSchemaString;
    private readonly byte[] _kitchenSinkBytes;
    private readonly string _kitchenSinkString;

    public ParserBenchmarks()
    {
        var resources = new ResourceHelper();
        _introspectionString =  resources.GetResourceString("IntrospectionQuery.graphql");
        _introspectionBytes = Encoding.UTF8.GetBytes(_introspectionString);
        _kitchenSinkSchemaString =  resources.GetResourceString("schema-kitchen-sink.graphql");
        _kitchenSinkSchemaBytes = Encoding.UTF8.GetBytes(_kitchenSinkSchemaString);
        _kitchenSinkString =  resources.GetResourceString("kitchen-sink-nullability.graphql");
        _kitchenSinkBytes = Encoding.UTF8.GetBytes(_kitchenSinkString);
    }

    [Benchmark]
    public DocumentNode Introspection_Parse_String()
        => Utf8GraphQLParser.Parse(_introspectionString);

    [Benchmark]
    public DocumentNode Introspection_Parse_Bytes()
        => Utf8GraphQLParser.Parse(_introspectionBytes);

    [Benchmark]
    public DocumentNode KitchenSink_Schema_Parse_String()
        => Utf8GraphQLParser.Parse(_kitchenSinkSchemaString);

    [Benchmark]
    public DocumentNode KitchenSink_Schema_Parse_Bytes()
        => Utf8GraphQLParser.Parse(_kitchenSinkSchemaBytes);

    [Benchmark]
    public DocumentNode KitchenSink_Query_Parse_String()
        => Utf8GraphQLParser.Parse(_kitchenSinkString);

    [Benchmark]
    public DocumentNode KitchenSink_Query_Parse_Bytes()
        => Utf8GraphQLParser.Parse(_kitchenSinkBytes);
}
