using System.Text;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8BlockStringTokenReaderTests
    {
        [Fact]
        private void ReadToken()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
                "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"");
            var reader = new Utf8GraphQLReader(source);

            // act
            reader.Read();

            // assert
            Assert.Equal(
                "helloWorld_123\r\n\t\tfoo\r\n\tbar",
                reader.GetString(reader.Value));

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
            byte[] source = Encoding.UTF8.GetBytes(
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
            Assert.Equal(34, reader.End);
        }

        [Fact]
        private void ReadToken_WithEscapedTrippleQuote2_EscapeIsReplacedWithActualQuotes()
        {
            // arrange
            string sourceBody = "\"\"\"hello\\\"\"\"World_123\r\n\t\tfoo\r\n\tbar\"\"\"";
            var source = new Source(sourceBody);
            var lexer = new Lexer();

            // act
            SyntaxToken token = lexer.Read(source);

            // assert
            Assert.NotNull(token);
            Assert.NotNull(token.Next);
            Assert.NotNull(token.Next.Next);
            Assert.Null(token.Next.Next.Next);

            SyntaxToken blockStringToken = token.Next;
            Assert.Equal(TokenKind.BlockString, blockStringToken.Kind);
            Assert.Equal("hello\"\"\"World_123\n\tfoo\nbar", blockStringToken.Value);
            Assert.Equal(1, blockStringToken.Line);
            Assert.Equal(1, blockStringToken.Column);
            Assert.Equal(0, blockStringToken.Start);
            Assert.Equal(34, blockStringToken.End);
            Assert.Equal(TokenKind.StartOfFile, blockStringToken.Previous.Kind);
        }

        [Fact]
        private void ReadToken_WithLeadingBlanks_BlanksAreRemoved()
        {
            // arrange
            string sourceBody = "\"\"\"\r\n\t\r\n\t\r\n\thelloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"";
            var source = new Source(sourceBody);
            var lexer = new Lexer();

            // act
            SyntaxToken token = lexer.Read(source);

            // assert
            Assert.NotNull(token);
            Assert.NotNull(token.Next);
            Assert.NotNull(token.Next.Next);
            Assert.Null(token.Next.Next.Next);

            SyntaxToken blockStringToken = token.Next;
            Assert.Equal(TokenKind.BlockString, blockStringToken.Kind);
            Assert.Equal("helloWorld_123\n\tfoo\nbar", blockStringToken.Value);
            Assert.Equal(1, blockStringToken.Line);
            Assert.Equal(1, blockStringToken.Column);
            Assert.Equal(0, blockStringToken.Start);
            Assert.Equal(36, blockStringToken.End);
            Assert.Equal(TokenKind.StartOfFile, blockStringToken.Previous.Kind);
        }

        [Fact]
        private void ReadToken_WithTrailingBlanks_BlanksAreRemoved()
        {
            // arrange
            string sourceBody = "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\r\n\t\r\n\t\r\n\t\r\n\t\"\"\"";
            var source = new Source(sourceBody);
            var lexer = new Lexer();

            // act
            SyntaxToken token = lexer.Read(source);

            // assert
            Assert.NotNull(token);
            Assert.NotNull(token.Next);
            Assert.NotNull(token.Next.Next);
            Assert.Null(token.Next.Next.Next);

            SyntaxToken blockStringToken = token.Next;
            Assert.Equal(TokenKind.BlockString, blockStringToken.Kind);
            Assert.Equal("helloWorld_123\n\tfoo\nbar", blockStringToken.Value);
            Assert.Equal(1, blockStringToken.Line);
            Assert.Equal(1, blockStringToken.Column);
            Assert.Equal(0, blockStringToken.Start);
            Assert.Equal(38, blockStringToken.End);
            Assert.Equal(TokenKind.StartOfFile, blockStringToken.Previous.Kind);
        }

        [Fact]
        private void ReadToken_SingleLine_ParsesCorrectly()
        {
            // arrange
            string sourceBody = "\"\"\"helloWorld_123\"\"\"";
            var source = new Source(sourceBody);
            var lexer = new Lexer();

            // act
            SyntaxToken token = lexer.Read(source);

            // assert
            Assert.NotNull(token);
            Assert.NotNull(token.Next);
            Assert.NotNull(token.Next.Next);
            Assert.Null(token.Next.Next.Next);

            SyntaxToken blockStringToken = token.Next;
            Assert.Equal(TokenKind.BlockString, blockStringToken.Kind);
            Assert.Equal("helloWorld_123", blockStringToken.Value);
            Assert.Equal(1, blockStringToken.Line);
            Assert.Equal(1, blockStringToken.Column);
            Assert.Equal(0, blockStringToken.Start);
            Assert.Equal(19, blockStringToken.End);
            Assert.Equal(TokenKind.StartOfFile, blockStringToken.Previous.Kind);
        }
    }
}
