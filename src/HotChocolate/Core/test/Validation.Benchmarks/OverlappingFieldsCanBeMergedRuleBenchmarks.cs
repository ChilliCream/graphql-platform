using BenchmarkDotNet.Attributes;
using HotChocolate.Language;
using HotChocolate.Validation.Rules;

namespace HotChocolate.Validation.Benchmarks;

[RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
public class OverlappingFieldsCanBeMergedRuleBenchmarks
{
    private readonly DocumentValidatorRule<FieldVisitor> _currentRule = new(new FieldVisitor());
    private readonly OverlappingFieldsCanBeMergedRule _newRule = new();
    private readonly DocumentValidatorContext _context = new();
    private readonly Dictionary<string, object?> _contextData = new();
    private readonly ISchema _schema;
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
        // clear last run data
        _context.Clear();
        _contextData.Clear();

        // prepare context for this run
        _context.Schema = _schema;
        _context.ContextData = _contextData;
        _context.Prepare(_document);

        // run the rule
        _currentRule.Validate(_context, _document);
    }

    [Benchmark]
    public void Field_Merge_New()
    {
        // clear last run data
        _context.Clear();
        _contextData.Clear();

        // prepare context for this run
        _context.Schema = _schema;
        _context.ContextData = _contextData;
        _context.Prepare(_document);

        // run the rule
        _newRule.Validate(_context, _document);
    }

    private static ISchema CreateTestSchema(string fileName) =>
        SchemaBuilder.New()
            .AddDocumentFromFile(fileName)
            .Use(next => next)
            .Create();
}
