using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.Validation.Rules;

namespace HotChocolate.Validation.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class OverlappingFieldsCanBeMergedRuleBenchmarks
{
    private readonly DocumentValidator _currentRule = DocumentValidatorBuilder.New()
        .AddVisitor<FieldVisitor>()
        .Build();
    private readonly DocumentValidator _newRule = DocumentValidatorBuilder.New()
        .AddRule<OverlappingFieldsCanBeMergedRule>()
        .Build();
    private readonly DocumentValidatorContext _context = new();
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly ISchemaDefinition _schema;
    private readonly DocumentNode _document;

    public OverlappingFieldsCanBeMergedRuleBenchmarks()
    {
        _schema = CreateTestSchema("/Users/michael/local/hc-0/src/HotChocolate/Core/test/Validation.Benchmarks/schema.graphqls");
        var query = File.ReadAllBytes("/Users/michael/local/hc-0/src/HotChocolate/Core/test/Validation.Benchmarks/query.graphql");
        _document = Utf8GraphQLParser.Parse(query);
    }

    [Benchmark]
    public void Field_Merge_Current()
    {
        _currentRule.Validate(_schema, _document);
    }

    [Benchmark]
    public void Field_Merge_New()
    {
        _newRule.Validate(_schema, _document);
    }

    private static ISchemaDefinition CreateTestSchema(string fileName) =>
        SchemaBuilder.New()
            .AddDocumentFromFile(fileName)
            .Use(next => next)
            .Create();
}
