using Xunit;

namespace HotChocolate.Language
{
    public class BlockStringTokenReaderTests
    {
        [Fact]
        private void ReadToken()
        {
            // arrange
            string sourceBody = "\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"";
            Source source = new Source(sourceBody);
            Lexer lexer = new Lexer();

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
            Assert.Equal(30, blockStringToken.End);
            Assert.Equal(TokenKind.StartOfFile, blockStringToken.Previous.Kind);
        }

        [Fact]
        private void ReadToken_WithEscapedTrippleQuote1_EscapeIsReplacedWithActualQuotes()
        {
            // arrange
            string sourceBody = "\"\"\"\\\"\"\"helloWorld_123\r\n\t\tfoo\r\n\tbar\"\"\"";
            Source source = new Source(sourceBody);
            Lexer lexer = new Lexer();

            // act
            SyntaxToken token = lexer.Read(source);

            // assert
            Assert.NotNull(token);
            Assert.NotNull(token.Next);
            Assert.NotNull(token.Next.Next);
            Assert.Null(token.Next.Next.Next);

            SyntaxToken blockStringToken = token.Next;
            Assert.Equal(TokenKind.BlockString, blockStringToken.Kind);
            Assert.Equal("\"\"\"helloWorld_123\n\tfoo\nbar", blockStringToken.Value);
            Assert.Equal(1, blockStringToken.Line);
            Assert.Equal(1, blockStringToken.Column);
            Assert.Equal(0, blockStringToken.Start);
            Assert.Equal(34, blockStringToken.End);
            Assert.Equal(TokenKind.StartOfFile, blockStringToken.Previous.Kind);
        }

        [Fact]
        private void ReadToken_WithEscapedTrippleQuote2_EscapeIsReplacedWithActualQuotes()
        {
            // arrange
            string sourceBody = "\"\"\"hello\\\"\"\"World_123\r\n\t\tfoo\r\n\tbar\"\"\"";
            Source source = new Source(sourceBody);
            Lexer lexer = new Lexer();

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
            Source source = new Source(sourceBody);
            Lexer lexer = new Lexer();

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
            Source source = new Source(sourceBody);
            Lexer lexer = new Lexer();

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
            Source source = new Source(sourceBody);
            Lexer lexer = new Lexer();

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
