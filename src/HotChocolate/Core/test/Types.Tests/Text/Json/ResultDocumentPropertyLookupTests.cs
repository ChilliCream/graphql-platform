using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Text.Json;

public class ResultDocumentPropertyLookupTests
{
    [Fact]
    public void TryGetProperty_Should_ReturnSelectedProperty_When_ObjectHoldsFullSelectionSet()
    {
        // arrange
        var operation = CreateOperation("{ first second third }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        document.Data.GetProperty("first").SetStringValue("a"u8);
        document.Data.GetProperty("second").SetStringValue("b"u8);
        document.Data.GetProperty("third").SetStringValue("c"u8);

        // act
        var found = document.Data.TryGetProperty("second", out var value);

        // assert
        Assert.True(found);
        Assert.Equal("b", value.GetString());
    }

    [Fact]
    public void TryGetProperty_Should_ReturnSelectedUtf8Property_When_ObjectHoldsFullSelectionSet()
    {
        // arrange
        var operation = CreateOperation("{ first second third }");
        using var document = new ResultDocument(CommonTestExtensions.CreateArena(), operation, 0);
        document.Data.GetProperty("first").SetStringValue("a"u8);
        document.Data.GetProperty("second").SetStringValue("b"u8);
        document.Data.GetProperty("third").SetStringValue("c"u8);

        // act
        var found = document.Data.TryGetProperty("third"u8, out var value);

        // assert
        Assert.True(found);
        Assert.Equal("c", value.GetString());
    }

    [Fact]
    public void TryGetProperty_Should_ReturnFalse_When_ObjectHoldsFewerPropertiesThanItsSelectionSet()
    {
        // arrange
        // deferred fragment document holding only the deferred selection
        using var document = CreateDeferredDocument("{ first ... @defer { second } third }");

        // act
        var found = document.Data.TryGetProperty("first", out _);

        // assert
        Assert.False(found);
    }

    [Fact]
    public void TryGetProperty_Should_ReturnFalse_When_UnselectedUtf8PropertyRowIsPastTheObject()
    {
        // arrange
        using var document = CreateDeferredDocument("{ first ... @defer { second } third }");

        // act
        var found = document.Data.TryGetProperty("third"u8, out _);

        // assert
        Assert.False(found);
    }

    [Fact]
    public void TryGetProperty_Should_ReturnSelectedProperty_When_ObjectHoldsSubsetOfItsSelectionSet()
    {
        // arrange
        using var document = CreateDeferredDocument("{ first ... @defer { second } third }");
        document.Data.GetProperty("second").SetStringValue("b"u8);

        // act
        var found = document.Data.TryGetProperty("second", out var value);

        // assert
        Assert.True(found);
        Assert.Equal("b", value.GetString());
    }

    private static ResultDocument CreateDeferredDocument(string query)
    {
        var operation = CreateOperation(query);
        var deferFlags = new ConditionFlags(1);
        var selectionSet = operation.RootSelectionSet;
        var deferred = selectionSet.Selections.ToArray().Single(s => s.IsDeferred(deferFlags));
        var deferUsage = deferred.GetPrimaryDeferUsage(deferFlags)!;

        return new ResultDocument(
            CommonTestExtensions.CreateArena(),
            operation,
            selectionSet,
            Path.Root,
            new ConditionFlags(0),
            deferFlags,
            deferUsage);
    }

    private static Operation CreateOperation(string query)
    {
        var schema =
            SchemaBuilder.New()
                .AddDocumentFromString(
                    """
                    type Query {
                      first: String
                      second: String
                      third: String
                    }
                    """)
                .Use(_ => _)
                .Create();

        return OperationCompiler.Compile("abc", Utf8GraphQLParser.Parse(query), schema);
    }
}
