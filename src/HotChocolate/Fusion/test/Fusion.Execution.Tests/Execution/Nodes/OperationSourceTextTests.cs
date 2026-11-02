using System.Runtime.InteropServices;
using System.Text;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes;

public class OperationSourceTextTests
{
    [Fact]
    public void Create_Should_RenderCompactText_When_SyntaxNodeIsProvided()
    {
        // arrange
        var node = Utf8GraphQLParser.Parse("query Test { field }");

        // act
        var source = OperationSourceText.Create("Test", OperationType.Query, node);

        // assert
        Encoding.UTF8.GetString(source.Value.Span).MatchInlineSnapshot("query Test { field }");
        Assert.Equal(OperationSourceTextHash.Compute(source.Value.Span), source.Hash);
    }

    [Theory]
    [InlineData("query Test { node(id: \"a\\\"b\") { id } }")]
    [InlineData("query Test { field(value: \"non-ascii ü 日本 😀\\n\\t\\\\\") }")]
    [InlineData("query Test { field(value: \"\"\"\n  first line\n  second line\n\"\"\") }")]
    public void Create_Should_PreserveStringValues_When_TextIsCompacted(string text)
    {
        // arrange
        var node = Utf8GraphQLParser.Parse(text);

        // act
        var source = OperationSourceText.Create("Test", OperationType.Query, node);
        var parsed = Utf8GraphQLParser.Parse(source.Value.Span);

        // assert
        Assert.Equal(node.ToString(indented: true), parsed.ToString(indented: true));
        Assert.Equal(OperationSourceTextHash.Compute(source.Value.Span), source.Hash);
    }

    [Theory]
    [InlineData("query Stored {\n  field\n}")]
    [InlineData("query Stored { field(value: \"a\") }")]
    public void Constructor_Should_PreserveStoredTextAndHash_When_SourceIsDeconstructed(string text)
    {
        // arrange
        var bytes = Encoding.UTF8.GetBytes(text);
        var suppliedHash = new OperationSourceTextHash("supplied-sha256", "supplied-short", 123);

        // act
        var source = new OperationSourceText("Stored", OperationType.Query, bytes, suppliedHash);
        var (name, type, value, hash) = source;

        // assert
        Assert.Equal(("Stored", OperationType.Query), (name, type));
        Assert.Equal(text, Encoding.UTF8.GetString(value.Span));
        Assert.Equal(suppliedHash, hash);
        Assert.True(MemoryMarshal.TryGetArray(value, out var array));
        Assert.Same(bytes, array.Array);
    }
}
