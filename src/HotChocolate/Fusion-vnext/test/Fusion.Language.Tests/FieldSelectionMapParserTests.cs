namespace HotChocolate.Fusion.Language;

public sealed class FieldSelectionMapParserTests
{
    [Fact]
    public void Parse_PathSegmentSingleFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentNestedFieldName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentWithTwoTypeNames_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1<Type1>.field2<Type2>.field3");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_PathSegmentWithTypeNameNoNestedField_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("field1<Type1>").Parse();

        // assert
        Assert.Equal(
            "Expected a `Period`-token, but found a `EndOfFile`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_PathWithTypeName_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("<Type1>.field1");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-Path
    [InlineData("book.title")]
    [InlineData("mediaById<Book>.isbn")]
    public void ParseAndPrint_PathValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Fact]
    public void Parse_PathWithTypeNameNoPathSegment_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act() => new FieldSelectionMapParser("<Type1>").Parse();

        // assert
        Assert.Equal(
            "Expected a `Period`-token, but found a `EndOfFile`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Parse_SelectedListValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("field1[field2]");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedListValue
    [InlineData("parts[id]")]
    [InlineData("parts[{ id, name }]")]
    [InlineData("parts[[{ id, name }]]")]
    [InlineData("{ coordinates: coordinates[{ lat: x, lon: y }] }")]
    public void ParseAndPrint_SelectedListValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedListValue
    [InlineData("parts[id, name]")]
    public void Parse_SelectedListValueInvalidExamples_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act() => new FieldSelectionMapParser(sourceText).Parse();

        // assert
        Assert.Equal(
            "Expected a `RightSquareBracket`-token, but found a `Name`-token.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
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
    public void Parse_SelectedObjectValueNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Fact]
    public void Parse_SelectedObjectValueMultipleFieldsNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var parser = new FieldSelectionMapParser("{ field1, field2 }");

        // act
        var selectedValueNode = parser.Parse();

        // assert
        selectedValueNode.MatchSnapshot();
    }

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedObjectValue
    [InlineData("dimension.{ size, weight }")]
    [InlineData("{ size: dimensions.size, weight: dimensions.weight }")]
    public void ParseAndPrint_SelectedObjectValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
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

    [Theory]
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-SelectedValue
    [InlineData("mediaById<Book>.title | mediaById<Movie>.movieTitle")]
    [InlineData("{ movieId: <Movie>.id } | { productId: <Product>.id }")]
    [InlineData("{ nested: { movieId: <Movie>.id } | { productId: <Product>.id } }")]
    public void ParseAndPrint_SelectedValueValidExamples_Matches(string sourceText)
    {
        // arrange & act
        var result = new FieldSelectionMapParser(sourceText).Parse().Print(indented: false);

        // assert
        Assert.Equal(sourceText, result);
    }

    [Fact]
    public void Parse_WithNodeLimitExceeded_ThrowsSyntaxException()
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
        Assert.Equal(
            "Source text contains more than 2 nodes. Parsing aborted.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }
}
