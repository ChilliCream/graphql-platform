using System.Text;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8NameTokenReaderTests
    {
        [InlineData("    \nhelloWorld_123")]
        [InlineData("    \nhelloWorld_123\n     ")]
        [InlineData("helloWorld_123\n     ")]
        [InlineData("helloWorld_123")]
        [Theory]
        private void ReadToken(string sourceText)
        {
            // arrange
            string nameTokenValue = "helloWorld_123";
            byte[] source = Encoding.UTF8.GetBytes(sourceText);
            var reader = new Utf8GraphQLReader(source);

            // act
            reader.Read();

            // assert
            Assert.Equal(TokenKind.Name, reader.Kind);
            Assert.Equal(nameTokenValue, reader.GetName());
        }
    }
}
