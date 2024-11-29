using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class BlockStringTokenReaderTests
{
    [Fact]
    private void ReadToken()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"");
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
    private void ReadToken_WithEscapedTrippleQuote1_EscapeIsReplacedWithActualQuotes()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            "\"\"\"\\\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"");
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
    private void ReadToken_WithEscapedTrippleQuote2_EscapeIsReplacedWithActualQuotes()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            "\"\"\"hello\\\"\"\"World_123\r\n\t\tfoo\r\n\tbar\"\"\"");
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
    private void ReadToken_WithLeadingBlanks_BlanksAreRemoved()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            "\"\"\"\r\n\t\r\n\t\r\n\thelloWorld_123" +
            "\r\n\t\tfoo\r\n\tbar\"\"\"");
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
    private void ReadToken_WithTrailingBlanks_BlanksAreRemoved()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar" +
            "\r\n\t\r\n\t\r\n\t\r\n\t\"\"\"");
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
    private void ReadToken_SingleLine_ParsesCorrectly()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(
            "\"\"\"helloWorld_123\"\"\"");
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
    private void UnescapeEmpty()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("\"\"");
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
    private void UnescapeString()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("\"abc\"");
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
    private void UnexpectedSyntaxException()
    {
        // arrange
        var source = new byte[] { 187, };
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
    private void NoDigitAfterZeroException()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("01");
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
    private void InvalidDigit()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("123.F");
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
    private void Zero()
    {
        // arrange
        var source = Encoding.UTF8.GetBytes("0 ");
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal("0", reader.GetScalarValue());
        Assert.Equal(TokenKind.Integer, reader.Kind);
    }
}
