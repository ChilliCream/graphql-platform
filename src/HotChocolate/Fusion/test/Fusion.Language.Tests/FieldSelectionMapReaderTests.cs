using SyntaxTokenInfo = HotChocolate.Fusion.Language.Utilities.FieldSelectionMapSyntaxTokenInfo;

namespace HotChocolate.Fusion.Language;

public sealed class FieldSelectionMapReaderTests
{
    [Fact]
    public void Read_PathSegmentSingleFieldName_MatchesSnapshot()
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
    public void Read_PathSegmentNestedFieldName_MatchesSnapshot()
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
    public void Read_PathSegmentWithTypeName_MatchesSnapshot()
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
    public void Read_PathWithTypeName_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("<Type1>.field1");
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
    public void Read_SelectedListValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("[field1]");
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
    public void Read_SelectedObjectValueNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("{ field1 }");
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
    public void Read_SelectedObjectValueMultipleFieldsNoSelectedValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("{ field1, field2 }");
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
    public void Read_WithoutComma_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("{ field1 field2 }");
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
    public void Read_IntValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("123");
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
    public void Read_NegativeIntValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("-123");
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
    public void Read_FloatValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("1.5");
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
    public void Read_FloatValueWithExponent_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("1.5e3");
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
    public void Read_StringValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("\"hello\"");
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
    public void Read_StringValueWithEscapeSequence_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("\"a\\\"b\"");
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
    public void Read_BlockStringValue_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("\"\"\"block\"\"\"");
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
    public void Read_Parentheses_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("()");
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
    public void Read_FieldWithArgument_MatchesSnapshot()
    {
        // arrange
        var reader = new FieldSelectionMapReader("width(unit: IMPERIAL)");
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
    public void Read_TokensAfterMultiLineBlockString_MatchesSnapshot()
    {
        // arrange
        // The block string spans two lines, so tokens after it must be tracked on line 2.
        var reader = new FieldSelectionMapReader("f(a: \"\"\"x\ny\"\"\", b: 1)");
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
    public void Read_UnterminatedString_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("\"abc");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Unterminated string value.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Theory]
    [InlineData("\"\"\"abc")]
    [InlineData("\"\"\"abc\"\"")]
    public void Read_UnterminatedBlockString_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act()
        {
            var reader = new FieldSelectionMapReader(sourceText);

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Unterminated string value.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_StringWithRawLineFeed_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("f(a: \"x\ny\")");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Unterminated string value.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_StringWithTrailingBackslash_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("\"abc\\");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Unterminated string value.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_NumberFollowedByPeriod_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("1.");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Invalid number, expected a digit but got ` `.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_FloatWithoutFractionDigits_ThrowsSyntaxException()
    {
        // arrange & act
        // The period is followed by a name character, so the fractional part has no digit.
        static void Act()
        {
            var reader = new FieldSelectionMapReader("1.a");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Invalid number, expected a digit but got `a`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_LeadingZero_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("01");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Invalid number, unexpected digit after 0: `1`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_NameCharacterFollowingNumber_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("1foo");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Unexpected character `f` following a number.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_PeriodFollowingNumber_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("1.5.x");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Unexpected character `.` following a number.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Theory]
    [InlineData("1e")]
    [InlineData("1e+")]
    public void Read_IncompleteExponent_ThrowsSyntaxException(string sourceText)
    {
        // arrange & act
        void Act()
        {
            var reader = new FieldSelectionMapReader(sourceText);

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Invalid number, expected a digit but got ` `.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_LoneMinus_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("f(a: -)");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Invalid number, expected a digit but got `)`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_WithTokenLimitExceeded_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("field1.field2", maxAllowedTokens: 2);

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Source text contains more than 2 tokens. Parsing aborted.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Fact]
    public void Read_UnexpectedCharacter_ThrowsSyntaxException()
    {
        // arrange & act
        static void Act()
        {
            var reader = new FieldSelectionMapReader("field1*field2");

            while (reader.Read())
            { }
        }

        // assert
        Assert.Equal(
            "Unexpected character `*`.",
            Assert.Throws<FieldSelectionMapSyntaxException>(Act).Message);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData(",")]
    [InlineData(", \n\t")]
    public void GetNextTokenKind_Should_ReturnEndOfFile_When_OnlyInsignificantCharactersRemain(
        string sourceText)
    {
        // arrange
        var reader = new FieldSelectionMapReader(sourceText);

        // act
        var tokenKind = reader.GetNextTokenKind();

        // assert
        Assert.Equal(FieldSelectionMapTokenKind.EndOfFile, tokenKind);
    }

    [Fact]
    public void GetNextTokenKind_Should_SkipLeadingInsignificantCharacters_When_TokenFollows()
    {
        // arrange
        var reader = new FieldSelectionMapReader(" , field");

        // act
        var tokenKind = reader.GetNextTokenKind();

        // assert
        Assert.Equal(FieldSelectionMapTokenKind.Name, tokenKind);
    }
}
