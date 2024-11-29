using System.Text;
using Xunit;

namespace HotChocolate.Language;

public class PunctuatorTokenReaderTests
{
    [Fact]
    public void ReadBangToken()
    {
        ReadToken('!', TokenKind.Bang);
    }

    [Fact]
    public void ReadDollarToken()
    {
        ReadToken('$', TokenKind.Dollar);
    }

    [Fact]
    public void ReadAmpersandToken()
    {
        ReadToken('&', TokenKind.Ampersand);
    }

    [Fact]
    public void ReadLeftParenthesisToken()
    {
        ReadToken('(', TokenKind.LeftParenthesis);
    }

    [Fact]
    public void ReadRightParenthesisToken()
    {
        ReadToken(')', TokenKind.RightParenthesis);
    }

    [Fact]
    public void ReadColonToken()
    {
        ReadToken(':', TokenKind.Colon);
    }

    [Fact]
    public void ReadEqualToken()
    {
        ReadToken('=', TokenKind.Equal);
    }

    [Fact]
    public void ReadAtToken()
    {
        ReadToken('@', TokenKind.At);
    }

    [Fact]
    public void ReadLeftBracketToken()
    {
        ReadToken('[', TokenKind.LeftBracket);
    }

    [Fact]
    public void ReadRightBracketToken()
    {
        ReadToken(']', TokenKind.RightBracket);
    }

    [Fact]
    public void ReadLeftBraceToken()
    {
        ReadToken('{', TokenKind.LeftBrace);
    }

    [Fact]
    public void ReadRightBraceToken()
    {
        ReadToken('}', TokenKind.RightBrace);
    }

    [Fact]
    public void ReadPipeToken()
    {
        ReadToken('|', TokenKind.Pipe);
    }

    [Fact]
    public void ReadSpreadToken()
    {
        ReadToken("...", TokenKind.Spread);
    }

    [InlineData("..!")]
    [InlineData("..1")]
    [InlineData(".._")]
    [InlineData("..a")]
    [Theory]
    public void SpreadExpected(string s)
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(s);

        // act
        void Fail() => new Utf8GraphQLReader(source).Read();

        // assert
        Assert.Throws<SyntaxException>(Fail).Message.MatchSnapshot();
    }

    private void ReadToken(char code, TokenKind kind)
    {
        ReadToken(code.ToString(), kind);
    }

    private void ReadToken(string sourceBody, TokenKind kind)
    {
        // arrange
        var source = Encoding.UTF8.GetBytes(sourceBody);
        var reader = new Utf8GraphQLReader(source);

        // act
        reader.Read();

        // assert
        Assert.Equal(kind, reader.Kind);
        Assert.Equal(1, reader.Line);
        Assert.Equal(1, reader.Column);
        Assert.Equal(0, reader.Start);
        Assert.Equal(sourceBody.Length, reader.End);
    }
}
