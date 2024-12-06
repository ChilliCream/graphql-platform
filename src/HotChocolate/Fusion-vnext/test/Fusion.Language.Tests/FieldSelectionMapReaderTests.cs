using SyntaxTokenInfo = HotChocolate.Fusion.FieldSelectionMapSyntaxTokenInfo;

namespace HotChocolate.Fusion;

public sealed class FieldSelectionMapReaderTests
{
    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
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

    [Test]
    public async Task Read_WithTokenLimitExceeded_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("field1.field2", maxAllowedTokens: 2);

            while (reader.Read()) { }
        }

        // assert
        await Assert
            .That(Assert.Throws<SyntaxException>(Act).Message)
            .IsEqualTo("Source text contains more than 2 tokens. Parsing aborted.");
    }

    [Test]
    public async Task Read_UnexpectedCharacter_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("field1*field2");

            while (reader.Read()) { }
        }

        // assert
        await Assert
            .That(Assert.Throws<SyntaxException>(Act).Message)
            .IsEqualTo("Unexpected character `*`.");
    }
}
