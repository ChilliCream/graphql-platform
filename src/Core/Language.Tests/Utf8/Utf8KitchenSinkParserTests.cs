using System.Text;
using ChilliCream.Testing;
using Snapshooter;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8KitchenSinkParserTests
    {
        [Fact]
        public void ParseFacebookKitchenSinkSchema()
        {
            // arrange
            string schemaSource = FileResource.Open(
                "schema-kitchen-sink.graphql")
                .NormalizeLineBreaks();
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(schemaSource));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document)
                .MatchSnapshot(new SnapshotNameExtension("sdl"));
            document.MatchSnapshot();
        }

        [Fact]
        public void ParseFacebookKitchenSinkQuery()
        {
            // arrange
            string querySource =
                FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks();
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(querySource));

            // act
            DocumentNode document = parser.Parse();

            // assert
            QuerySyntaxSerializer.Serialize(document)
                .MatchSnapshot(new SnapshotNameExtension("sdl"));
            document.MatchSnapshot();
        }
    }
}
