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
            byte[] source = Encoding.UTF8.GetBytes(
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
            byte[] source = Encoding.UTF8.GetBytes(
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
            Assert.Equal(38, reader.End);
        }

        [Fact]
        private void ReadToken_WithTrailingBlanks_BlanksAreRemoved()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
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
            Assert.Equal(38, reader.End);
        }

        [Fact]
        private void ReadToken_SingleLine_ParsesCorrectly()
        {
            // arrange
            byte[] source = Encoding.UTF8.GetBytes(
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
    }
}
