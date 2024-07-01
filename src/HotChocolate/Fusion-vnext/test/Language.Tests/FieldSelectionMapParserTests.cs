using CookieCrumble;

namespace HotChocolate.Fusion;

public sealed class FieldSelectionMapParserTests
{
    [Fact]
    public void Parse_PathSingleFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathNestedFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathWithTwoTypeNames_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2<Type2>.field3");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathWithTypeNameNoNestedField_ThrowsSyntaxException()
    {
        // arrange
        static SelectedValueNode Act()
        {
            var parser = new FieldSelectionMapParser("field1<Type1>");

            return parser.Parse();
        }

        // act & assert
        Assert.Equal(
            "Expected a `Period`-token, but found a `EndOfFile`-token.",
            Assert.Throws<SyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_SelectedObjectValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1: field1 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedValueMultiplePaths_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2 | field1<Type2>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedValueMultipleSelectedObjectValues_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1: field1 } | { field2: field2 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedValueMultipleSelectedObjectValuesNested_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser(
            "{ nested: { field1: field1 } | { field2: field2 } }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_WithNodeLimitExceeded_ThrowsSyntaxException()
    {
        // arrange
        static void Act()
        {
            var parser = new FieldSelectionMapParser(
                "field1.field2",
                new FieldSelectionMapParserOptions(maxAllowedNodes: 2));

            parser.Parse();
        }

        // act & assert
        Assert.Equal(
            "Source text contains more than 2 nodes. Parsing aborted.",
            Assert.Throws<SyntaxException>(Act).Message);
    }
}
