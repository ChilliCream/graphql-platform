using System.Text;

namespace HotChocolate.Language;

public class BlockStringTokenReaderTests
{
    [Fact]
    public void ReadToken()
    {
        // arrange
        var source = "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal(
            "helloWorld_123\r\n\t\tfoo\r\n\tbar",
            Utf8GraphQLReader.GetString(reader.Value));

        Assert.Equal(
            "helloWorld_123\n\tfoo\nbar",
            reader.GetString());

        Assert.Equal(TokenKind.BlockString, reader.Kind);
        Assert.Equal(1, reader.Line);
        Assert.Equal(1, reader.Column);
        Assert.Equal(0, reader.Start);
        Assert.Equal(32, reader.End);
    }

    [Fact]
    public void ReadToken_WithEscapedTrippleQuote1_EscapeIsReplacedWithActualQuotes()
    {
        // arrange
        var source = "\"\"\"\\\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal(
            "\"\"\"helloWorld_123\n\tfoo\nbar",
            reader.GetString());

        Assert.Equal(TokenKind.BlockString, reader.Kind);
        Assert.Equal(1, reader.Line);
        Assert.Equal(1, reader.Column);
        Assert.Equal(0, reader.Start);
        Assert.Equal(36, reader.End);
    }

    [Fact]
    public void ReadToken_WithEscapedTrippleQuote2_EscapeIsReplacedWithActualQuotes()
    {
        // arrange
        var source = "\"\"\"hello\\\"\"\"World_123\r\n\t\tfoo\r\n\tbar\"\"\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal(
            "hello\"\"\"World_123\n\tfoo\nbar",
            reader.GetString());

        Assert.Equal(TokenKind.BlockString, reader.Kind);
        Assert.Equal(1, reader.Line);
        Assert.Equal(1, reader.Column);
        Assert.Equal(0, reader.Start);
        Assert.Equal(36, reader.End);
    }

    [Fact]
    public void ReadToken_WithLeadingBlanks_BlanksAreRemoved()
    {
        // arrange
        var source = "\"\"\"\r\n\t\r\n\t\r\n\thelloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal(
            "helloWorld_123\n\tfoo\nbar",
            reader.GetString());

        Assert.Equal(TokenKind.BlockString, reader.Kind);
        Assert.Equal(1, reader.Line);
        Assert.Equal(1, reader.Column);
        Assert.Equal(0, reader.Start);
        Assert.Equal(41, reader.End);
    }

    [Fact]
    public void ReadToken_WithTrailingBlanks_BlanksAreRemoved()
    {
        // arrange
        var source = "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\r\n\t\r\n\t\r\n\t\r\n\t\"\"\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal(
            "helloWorld_123\n\tfoo\nbar",
            reader.GetString());

        Assert.Equal(TokenKind.BlockString, reader.Kind);
        Assert.Equal(1, reader.Line);
        Assert.Equal(1, reader.Column);
        Assert.Equal(0, reader.Start);
        Assert.Equal(44, reader.End);
    }

    [Fact]
    public void ReadToken_SingleLine_ParsesCorrectly()
    {
        // arrange
        var source = "\"\"\"helloWorld_123\"\"\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal("helloWorld_123", reader.GetString());
        Assert.Equal(TokenKind.BlockString, reader.Kind);
        Assert.Equal(1, reader.Line);
        Assert.Equal(1, reader.Column);
        Assert.Equal(0, reader.Start);
        Assert.Equal(19, reader.End);
    }

    [Fact]
    public void UnescapeEmpty()
    {
        // arrange
        var source = "\"\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);
        reader.Read();

        // act
        var buffer = new byte[1];
        var span = buffer.AsSpan();
        reader.UnescapeValue(ref span);

        // assert
        Assert.Equal(0, span.Length);
    }

    [Fact]
    public void UnescapeString()
    {
        // arrange
        var source = "\"abc\""u8.ToArray();
        var reader = new Utf8GraphQLReader(source);
        reader.Read();

        // act
        var buffer = new byte[3 * 4];
        var span = buffer.AsSpan();
        reader.UnescapeValue(ref span);

        // assert
        Assert.Equal(3, span.Length);
        Assert.Equal("abc", Utf8GraphQLReader.GetString(span));
    }

    [Fact]
    public void UnexpectedSyntaxException()
    {
        // arrange
        var source = new byte[] { 187 };
        var reader = new Utf8GraphQLReader(source);
        var raised = false;

        // act
        try
        {
            reader.Read();
        }
        catch (SyntaxException ex)
        {
            raised = true;
            ex.Message.MatchSnapshot();
        }

        // assert
        Assert.True(raised);
    }

    [Fact]
    public void NoDigitAfterZeroException()
    {
        // arrange
        var source = "01"u8.ToArray();
        var reader = new Utf8GraphQLReader(source);
        var raised = false;

        // act
        try
        {
            reader.Read();
        }
        catch (SyntaxException ex)
        {
            raised = true;
            ex.Message.MatchSnapshot();
        }

        // assert
        Assert.True(raised);
    }

    [Fact]
    public void InvalidDigit()
    {
        // arrange
        var source = "123.F"u8.ToArray();
        var reader = new Utf8GraphQLReader(source);
        var raised = false;

        // act
        try
        {
            reader.Read();
        }
        catch (SyntaxException ex)
        {
            raised = true;
            ex.Message.MatchSnapshot();
        }

        // assert
        Assert.True(raised);
    }

    [Fact]
    public void Zero()
    {
        // arrange
        var source = "0 "u8.ToArray();
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal("0", reader.GetScalarValue());
        Assert.Equal(TokenKind.Integer, reader.Kind);
    }

    [Theory]
    [InlineData("\"foo\\", "Unescaped_Backslash")]
    [InlineData("\"\"\"foo\\\"\"\"", "Unescaped_Backslash_BlockString")]
    [InlineData("\"\"\"foo\\\"", "Unterminated_BlockString")]
    [InlineData("-", "Standalone_Minus")]
    [InlineData("..", "Incomplete_Spread")]
    [InlineData("1.", "Invalid_Decimal")]
    [InlineData("1e", "Invalid_Exponent")]
    [InlineData("1e-", "Invalid_Exponent_Minus")]
    [InlineData(".1", "Invalid_Decimal_NoPrefixNumber")]
    [InlineData("\0xEF\0xBB", "Incorrect_UTF8_BOM")]
    public void InvalidInput_ThrowsSyntaxException(string sourceText, string postFix)
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(sourceText);

        // act & assert
        var reader = new Utf8GraphQLReader(source);
        try
        {
            reader.Read();
            Assert.Fail("No exception was thrown.");
        }
        catch (SyntaxException ex)
        {
            ex.Message.MatchSnapshot(postFix: postFix);
        }
    }

    [Fact]
    public void Utf8Bom_IsSkipped()
    {
        // arrange
        var source = new byte[] { 0xEF, 0xBB, 0xBF, (byte)'a' };

        // act
        var reader = new Utf8GraphQLReader(source);
        var result = reader.Read();

        // assert
        Assert.True(result);
        Assert.Equal(TokenKind.Name, reader.Kind);
        Assert.Equal("a", Utf8GraphQLReader.GetString(reader.Value));
    }
}
