using System.Text;
using Xunit;

namespace HotChocolate.Language;

public static class SyntaxParserHelperTests
{
    [Fact]
    public static void ParseField()
    {
        var field = Utf8GraphQLParser.Syntax.ParseField("foo");
        Assert.Equal("foo", field.Name.Value);
    }

    [Fact]
    public static void ParseCompositeField()
    {
        var field = Utf8GraphQLParser.Syntax.ParseField("foo { bar }");
        Assert.Equal("foo", field.Name.Value);
    }

    [Fact]
    public static void ParseSelectionSet()
    {
        var selectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet("{ bar }");
        Assert.Collection(
            selectionSet.Selections,
            field => Assert.Equal("bar",
                Assert.IsType<FieldNode>(field).Name.Value));
    }

    [Fact]
    public static void ParseValueLiteral()
    {
        var literal = Utf8GraphQLParser.Syntax.ParseValueLiteral("BAZ");
        Assert.IsType<EnumValueNode>(literal);
    }

    [Fact]
    public static void ParseVariable()
    {
        var literal = Utf8GraphQLParser.Syntax.ParseValueLiteral("$foo", false);
        Assert.IsType<VariableNode>(literal);
    }

    [Fact]
    public static void ParseTypeReference()
    {
        // arrange
        var sourceText = "[[String!]]";

        // act
        var type = Utf8GraphQLParser.Syntax.ParseTypeReference(sourceText);

        // assert
        Assert.Equal(
            "String",
            Assert.IsType<NamedTypeNode>(
                Assert.IsType<NonNullTypeNode>(
                    Assert.IsType<ListTypeNode>(
                        Assert.IsType<ListTypeNode>(type).Type).Type).Type).Name.Value);
    }

    [Fact]
    public static void ParseTypeReference_Span()
    {
        // arrange
        var sourceText = Encoding.UTF8.GetBytes("[[String!]]");

        // act
        var type = Utf8GraphQLParser.Syntax.ParseTypeReference(sourceText);

        // assert
        Assert.Equal(
            "String",
            Assert.IsType<NamedTypeNode>(
                Assert.IsType<NonNullTypeNode>(
                    Assert.IsType<ListTypeNode>(
                        Assert.IsType<ListTypeNode>(type).Type).Type).Type).Name.Value);
    }

    [Fact]
    public static void ParseTypeReference_Reader()
    {
        // arrange
        var sourceText = Encoding.UTF8.GetBytes("[[String!]]");
        var reader = new Utf8GraphQLReader(sourceText);
        reader.MoveNext();

        // act
        var type = Utf8GraphQLParser.Syntax.ParseTypeReference(reader);

        // assert
        Assert.Equal(
            "String",
            Assert.IsType<NamedTypeNode>(
                Assert.IsType<NonNullTypeNode>(
                    Assert.IsType<ListTypeNode>(
                        Assert.IsType<ListTypeNode>(type).Type).Type).Type).Name.Value);
    }
}
