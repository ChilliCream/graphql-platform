namespace HotChocolate.Fusion;

public sealed class FieldSelectionMapParserTests
{
    [Test]
    public void Parse_PathSegmentSingleFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_PathSegmentNestedFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_PathSegmentWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_PathSegmentWithTwoTypeNames_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2<Type2>.field3");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public async Task Parse_PathSegmentWithTypeNameNoNestedField_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("field1<Type1>").Parse();

        // assert
        await Assert
            .That(Assert.Throws<SyntaxException>(Act).Message)
            .IsEqualTo("Expected a `Period`-token, but found a `EndOfFile`-token.");
    }

    [Test]
    public void Parse_PathWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("<Type1>.field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-Path
    [Arguments("book.title")]
    [Arguments("mediaById<Book>.isbn")]
    public async Task ParseAndPrint_PathValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        await Assert.That(result).IsEqualTo(sourceText);
    }

    [Test]
    public async Task Parse_PathWithTypeNameNoPathSegment_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("<Type1>").Parse();

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
    public void Parse_SelectedObjectValueNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    public void Parse_SelectedObjectValueMultipleFieldsNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1 field2 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Test]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedObjectValue
    [Arguments("dimension.{ size weight }")]
    [Arguments("{ size: dimensions.size weight: dimensions.weight }")]
    public async Task ParseAndPrint_SelectedObjectValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        await Assert.That(result).IsEqualTo(sourceText);
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
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedValue
    [Arguments("mediaById<Book>.title | mediaById<Movie>.movieTitle")]
    [Arguments("{ movieId: <Movie>.id } | { productId: <Product>.id }")]
    [Arguments("{ nested: { movieId: <Movie>.id } | { productId: <Product>.id } }")]
    public async Task ParseAndPrint_SelectedValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        await Assert.That(result).IsEqualTo(sourceText);
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
