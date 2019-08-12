using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaParserKitchenSinkTests
    {
        [Fact]
        public void ParseFacebookKitchenSinkSchema()
        {
            // arrange
            string schemaSource = FileResource.Open("schema-kitchen-sink.graphql");

            // act
            var parser = new Parser();
            DocumentNode document = parser.Parse(
                schemaSource, new ParserOptions(noLocations: true));

            // assert
            document.MatchSnapshot();
        }

        [Fact]
        public void ParseFacebookKitchenSinkQuery()
        {
            // arrange
            string querySource = FileResource.Open("kitchen-sink.graphql");

            // act
            var parser = new Parser();
            DocumentNode document = parser.Parse(
                querySource, new ParserOptions(
                    noLocations: true, allowFragmentVariables: true));

            // assert
            document.MatchSnapshot();
        }
    }
}
