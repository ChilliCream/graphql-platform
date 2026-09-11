using HotChocolate.Language;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Verifies the maximum response-size algebra and its combined traversal
/// with IBM cost.
/// </summary>
public class ResponseSizeAlgebraTests
{
    [Fact]
    public void Evaluate_Should_CountObjectFieldsAndIgnoreScalarContents_When_Selected()
    {
        // arrange
        const string sdl =
            """
            type Query { book: Book }
            type Book { title: String }
            """;

        // act
        var responseSize = EvaluateResponseSize(sdl, "{ book { title } }");

        // assert
        Assert.Equal(2.0, responseSize);
    }

    [Fact]
    public void Evaluate_Should_RepeatObjectContentsButNotScalars_When_ListIsSelected()
    {
        // arrange
        const string sdl =
            """
            type Query {
              books(first: Int): [Book] @listSize(slicingArguments: ["first"])
              tags(first: Int): [String] @listSize(slicingArguments: ["first"])
            }
            type Book { title: String }
            """;

        // act
        var objectListSize = EvaluateResponseSize(sdl, "{ books(first: 3) { title } }");
        var scalarListSize = EvaluateResponseSize(sdl, "{ tags(first: 3) }");

        // assert
        Assert.Equal(4.0, objectListSize);
        Assert.Equal(1.0, scalarListSize);
    }

    [Fact]
    public void Evaluate_Should_PreferInheritedSizedFields_When_ResolvingAChildList()
    {
        // arrange
        const string sdl =
            """
            type Query {
              connection(first: Int): Connection
                @listSize(slicingArguments: ["first"], sizedFields: ["edges"])
            }
            type Connection { edges: [Book] @listSize(assumedSize: 2) }
            type Book { title: String }
            """;

        // act
        var responseSize = EvaluateResponseSize(sdl, "{ connection(first: 4) { edges { title } } }");

        // assert
        Assert.Equal(6.0, responseSize);
    }

    [Fact]
    public void Evaluate_Should_SaturateToInfinity_When_DefaultListSizeIsInfinite()
    {
        // arrange
        const string sdl =
            """
            type Query { books: [Book] }
            type Book { title: String }
            """;

        // act
        var responseSize = EvaluateResponseSize(sdl, "{ books { title } }");

        // assert
        Assert.Equal(double.PositiveInfinity, responseSize);
    }

    [Fact]
    public void Evaluate_Should_MultiplySelectedChildrenForEveryListWrapper_When_ListIsNested()
    {
        // arrange
        // max_response_size.rs: every_list_wrapper_multiplies_selected_children
        const string sdl =
            """
            type Query { nodes: [[Node]] }
            type Node { name: String }
            """;

        // act
        var responseSize = EvaluateResponseSize(sdl, "{ nodes { name } }", defaultListSize: 3.0);

        // assert
        Assert.Equal(10.0, responseSize);
    }

    [Fact]
    public void Field_Should_ReturnOne_When_ListMultiplierIsInfiniteAndChildIsZero()
    {
        // arrange
        var outputType = ParseFieldType("[String]");

        // act
        var responseSize = ResponseSizeFieldRule.Field(outputType, double.PositiveInfinity, child: 0.0);

        // assert
        Assert.Equal(1.0, responseSize);
    }

    [Fact]
    public void Field_Should_ReturnOne_When_ListMultiplierIsZeroAndChildIsInfinite()
    {
        // arrange
        var outputType = ParseFieldType("[String]");

        // act
        var responseSize = ResponseSizeFieldRule.Field(outputType, listMultiplier: 0.0, double.PositiveInfinity);

        // assert
        Assert.Equal(1.0, responseSize);
    }

    [Fact]
    public void TupledAlgebra_Should_ComputeCostAndResponseSize_When_EvaluatedOnce()
    {
        // arrange
        const string sdl =
            """
            type Query { books(first: Int): [Book] @listSize(slicingArguments: ["first"]) }
            type Book { title: String }
            """;
        var snapshot = BuildSnapshot(sdl);
        var document = Utf8GraphQLParser.Parse("{ books(first: 3) { title } }");
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");
        var algebra = new TupledAlgebra(snapshot);

        // act
        var estimate = ExactCasesTraversal
            .Evaluate(snapshot, fragments, tree, algebra, new CaseBudget(4096))
            .Resolve(_ => false);

        // assert
        Assert.Equal(new CostEstimate(1.0, 4.0, 4.0), estimate);
    }

    [Fact]
    public void TupledAlgebra_Should_MultiplyOnlyResponseSizeForEveryListWrapper_When_ListIsNested()
    {
        // arrange
        const string sdl =
            """
            type Query { nodes: [[Node]] }
            type Node { name: String }
            """;
        var snapshot = BuildSnapshot(sdl, defaultListSize: 3.0);
        var document = Utf8GraphQLParser.Parse("{ nodes { name } }");
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");
        var algebra = new TupledAlgebra(snapshot);

        // act
        var estimate = ExactCasesTraversal
            .Evaluate(snapshot, fragments, tree, algebra, new CaseBudget(4096))
            .Resolve(_ => false);

        // assert
        Assert.Equal(new CostEstimate(1.0, 4.0, 10.0), estimate);
    }

    private static double EvaluateResponseSize(
        string sdl,
        string operationText,
        double defaultListSize = double.PositiveInfinity)
    {
        var snapshot = BuildSnapshot(sdl, defaultListSize);
        var document = Utf8GraphQLParser.Parse(operationText);
        var operation = ConditionTreeTestHelpers.ParseOperation(document);
        var fragments = ConditionTreeExtractor.IndexFragments(document);
        var tree = ConditionTreeExtractor.ExtractOperation(snapshot, document, operation, "Query");
        var algebra = new ResponseSizeAlgebra(snapshot);

        return ExactCasesTraversal
            .Evaluate(snapshot, fragments, tree, algebra, new CaseBudget(4096))
            .Resolve(_ => false);
    }

    private static IType ParseFieldType(string type)
    {
        var schema = SchemaParser.Parse($"type Query {{ field: {type} }}");
        return ((MutableObjectTypeDefinition)schema.Types["Query"]).Fields["field"].Type;
    }

    private static CostSchemaSnapshot BuildSnapshot(
        string sdl,
        double defaultListSize = double.PositiveInfinity)
        => CostSchemaSnapshot.Create(
            SchemaParser.Parse(sdl),
            new CostEngineOptions { DefaultListSize = defaultListSize });
}
