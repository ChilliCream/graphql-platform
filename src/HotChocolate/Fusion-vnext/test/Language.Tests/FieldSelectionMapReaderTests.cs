using CookieCrumble;
using SyntaxTokenInfo = HotChocolate.Fusion.FieldSelectionMapSyntaxTokenInfo;

namespace HotChocolate.Fusion;

public sealed class FieldSelectionMapReaderTests
{
    [Fact]
    public void Read_PathSingleFieldName_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("field1");
        List<SyntaxTokenInfo> readTokens = [];

        // act
        while (reader.Read())
        {
            readTokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        readTokens.MatchSnapshot();
    }

    [Fact]
    public void Read_PathNestedFieldName_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("field1.field2");
        List<SyntaxTokenInfo> readTokens = [];

        // act
        while (reader.Read())
        {
            readTokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        readTokens.MatchSnapshot();
    }

    [Fact]
    public void Read_PathWithTypeName_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("field1<Type1>.field2");
        List<SyntaxTokenInfo> readTokens = [];

        // act
        while (reader.Read())
        {
            readTokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        readTokens.MatchSnapshot();
    }

    [Fact]
    public void Read_SelectedObjectValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("{ field1: field1 }");
        List<SyntaxTokenInfo> readTokens = [];

        // act
        while (reader.Read())
        {
            readTokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        readTokens.MatchSnapshot();
    }

    [Fact]
    public void Read_SelectedValueMultiplePaths_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("field1<Type1>.field2 | field1<Type2>.field2");
        List<SyntaxTokenInfo> readTokens = [];

        // act
        while (reader.Read())
        {
            readTokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        readTokens.MatchSnapshot();
    }

    [Fact]
    public void Read_WithWhiteSpace_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader(" field1 . field2 ");
        List<SyntaxTokenInfo> readTokens = [];

        // act
        while (reader.Read())
        {
            readTokens.Add(SyntaxTokenInfo.FromReader(reader));
        }

        // assert
        readTokens.MatchSnapshot();
    }

    [Fact]
    public void Read_WithTokenLimitExceeded_ThrowsSyntaxException()
    {
        // arrange
        static void Act()
        {
            var reader = new FieldSelectionMapReader("field1.field2", maxAllowedTokens: 2);

            while (reader.Read()) { }
        }

        // act & assert
        Assert.Equal(
            "Source text contains more than 2 tokens. Parsing aborted.",
            Assert.Throws<SyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_UnexpectedCharacter_ThrowsSyntaxException()
    {
        // arrange
        static void Act()
        {
            var reader = new FieldSelectionMapReader("field1*field2");

            while (reader.Read()) { }
        }

        // act & assert
        Assert.Equal("Unexpected character `*`.", Assert.Throws<SyntaxException>(Act).Message);
    }
}
