using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public class OperationSourceTextTests
{
    [Fact]
    public void Create_Should_ShareLiteralArray_When_TextNeedsNoEscaping()
    {
        // arrange
        var node = Utf8GraphQLParser.Parse("query Test { field }");

        // act
        var source = OperationSourceText.Create("Test", OperationType.Query, node);
        var hasValueArray = MemoryMarshal.TryGetArray(source.Value, out var value);
        var hasLiteralArray = MemoryMarshal.TryGetArray(source.JsonLiteral, out var literal);

        // assert
        Assert.True(hasValueArray && hasLiteralArray);
        Assert.Same(literal.Array, value.Array);
        Assert.Equal(literal.Offset + 1, value.Offset);
        Assert.Equal(literal.Count - 2, value.Count);
        Assert.Equal(OperationSourceTextHash.Compute(source.Value.Span), source.Hash);
        Encoding.UTF8.GetString(source.JsonLiteral.Span).MatchInlineSnapshot(
            "\"query Test { field }\"");
    }

    [Fact]
    public void Create_Should_UseSeparateArray_When_TextContainsQuote()
    {
        // arrange
        var node = Utf8GraphQLParser.Parse("""query Test { node(id: "a\"b") { id } }""");

        // act
        var source = OperationSourceText.Create("Test", OperationType.Query, node);
        var hasValueArray = MemoryMarshal.TryGetArray(source.Value, out var value);
        var hasLiteralArray = MemoryMarshal.TryGetArray(source.JsonLiteral, out var literal);
        using var json = JsonDocument.Parse(source.JsonLiteral);

        // assert
        Assert.True(hasValueArray && hasLiteralArray);
        Assert.NotSame(literal.Array, value.Array);
        Assert.Equal(Encoding.UTF8.GetString(source.Value.Span), json.RootElement.GetString());
        Assert.Equal(OperationSourceTextHash.Compute(source.Value.Span), source.Hash);
        Encoding.UTF8.GetString(source.Value.Span).MatchInlineSnapshot(
            """query Test { node(id: "a\"b") { id } }""");
    }

    [Fact]
    public void Create_Should_ShareLiteralArray_When_StoredTextNeedsNoEscaping()
    {
        // arrange
        var bytes = "query Stored { field }"u8.ToArray();
        var hash = OperationSourceTextHash.Compute(bytes);

        // act
        var source = OperationSourceText.Create("Stored", OperationType.Query, bytes, hash);
        var hasValueArray = MemoryMarshal.TryGetArray(source.Value, out var value);
        var hasLiteralArray = MemoryMarshal.TryGetArray(source.JsonLiteral, out var literal);

        // assert
        Assert.True(hasValueArray && hasLiteralArray);
        Assert.Same(literal.Array, value.Array);
        Assert.Equal(literal.Offset + 1, value.Offset);
        Assert.Equal(bytes, source.Value.ToArray());
        Assert.Equal(hash, source.Hash);
    }

    [Theory]
    [InlineData("query Stored {\n  field\n}")]
    [InlineData("query Stored { field(value: \"a\") }")]
    public void Create_Should_PreserveStoredTextAndHash_When_PlanIsPersisted(string text)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(text);
        var hash = OperationSourceTextHash.Compute(bytes);

        // act
        var source = OperationSourceText.Create("Stored", OperationType.Query, bytes, hash);
        using var json = JsonDocument.Parse(source.JsonLiteral);

        // assert
        Assert.Equal(bytes, source.Value.ToArray());
        Assert.Equal(hash, source.Hash);
        Assert.Equal(text, json.RootElement.GetString());
        Assert.True(MemoryMarshal.TryGetArray(source.Value, out var value));
        Assert.Same(bytes, value.Array);
    }
}
