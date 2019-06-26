using System.Text;
using ChilliCream.Testing;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8GraphQLRequestReaderTests
    {
        [Fact]
        public void Parse()
        {
            var source = Encoding.UTF8.GetBytes(
                FileResource.Open("SimpleRequest.json"));
            var parserOptions = new ParserOptions();
            var requestParser = new  Utf8GraphQLRequestReader(
                source, parserOptions);
            requestParser.Parse();
        }
    }
}
