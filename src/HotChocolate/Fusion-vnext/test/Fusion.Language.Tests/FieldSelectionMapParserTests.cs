namespace HotChocolate.Fusion;

public sealed class FieldSelectionMapParserTests
{
    [Test]
    public void Parse_PathSingleFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_PathNestedFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_PathWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_PathWithTwoTypeNames_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2<Type2>.field3");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public async Task Parse_PathWithTypeNameNoNestedField_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("field1<Type1>").Parse();

        // assert
        await Assert
            .That(Assert.Throws<SyntaxException>(Act).Message)
            .IsEqualTo("Expected a `Period`-token, but found a `EndOfFile`-token.");
    }

    [Test]
    public void Parse_SelectedObjectValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1: field1 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_SelectedValueMultiplePaths_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2 | field1<Type2>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_SelectedValueMultipleSelectedObjectValues_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1: field1 } | { field2: field2 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
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

    [Test]
    public async Task Parse_WithNodeLimitExceeded_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var parser = new FieldSelectionMapParser(
                "field1.field2",
                new FieldSelectionMapParserOptions(maxAllowedNodes: 2));

            parser.Parse();
        }

        // assert
        await Assert
            .That(Assert.Throws<SyntaxException>(Act).Message)
            .IsEqualTo("Source text contains more than 2 nodes. Parsing aborted.");
    }
}
