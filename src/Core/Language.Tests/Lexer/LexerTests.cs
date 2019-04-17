using System;
using System.Text;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Language
{
    public class LexerTests
    {
        [Fact]
        public void EnsureTokensAreDoublyLinked()
        {
            // arrange
            Source source = new Source(@"type foo");
            Lexer lexer = new Lexer();

            // act
            SyntaxToken token = lexer.Read(source);

            // assert
            Assert.Equal(TokenKind.StartOfFile, token.Kind);
            Assert.Null(token.Previous);
            Assert.NotNull(token.Next);

            Assert.Equal(TokenKind.Name, token.Next.Kind);
            Assert.Equal(token, token.Next.Previous);
            Assert.NotNull(token.Next.Next);

            Assert.Equal(TokenKind.Name, token.Next.Next.Kind);
            Assert.Equal(token.Next, token.Next.Next.Previous);
            Assert.NotNull(token.Next.Next.Next);

            Assert.Equal(TokenKind.EndOfFile, token.Next.Next.Next.Kind);
            Assert.Equal(token.Next.Next, token.Next.Next.Next.Previous);
            Assert.Null(token.Next.Next.Next.Next);
        }

        [Fact]
        public void SimpleReaderTest()
        {
            var source = new ReadOnlySpan<byte>(
                Encoding.UTF8.GetBytes("type foo"));
            var lexer = new Utf8GraphQLReader(source);

            Assert.Equal(TokenKind.StartOfFile, lexer.Kind);

            Assert.True(lexer.Read());
            Assert.Equal(TokenKind.Name, lexer.Kind);
            Assert.Equal("type",
                Encoding.UTF8.GetString(lexer.Value.ToArray()));

            Assert.True(lexer.Read());
            Assert.Equal(TokenKind.Name, lexer.Kind);
            Assert.Equal("foo",
                Encoding.UTF8.GetString(lexer.Value.ToArray()));

            Assert.True(lexer.Read());
            Assert.Equal(TokenKind.EndOfFile, lexer.Kind);
        }

        [Fact]
        public void IntrospectionQueryV11()
        {
            var source = new ReadOnlySpan<byte>(
                Encoding.UTF8.GetBytes(
                    FileResource.Open("IntrospectionQuery.graphql")));
            var reader = new Utf8GraphQLReader(source);
            while (reader.Read()) { }
        }
    }
}
