using System.Text;
using ChilliCream.Testing;
using HotChocolate.Language.Utilities;
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
            var schemaSource = FileResource.Open(
                "schema-kitchen-sink.graphql")
                .NormalizeLineBreaks();
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(schemaSource));

            // act
            DocumentNode document = parser.Parse();

            // assert
            document.ToString().MatchSnapshot(new SnapshotNameExtension("sdl"));
            document.MatchSnapshot();
        }

        [Fact]
        public void ParseFacebookKitchenSinkQuery()
        {
            // arrange
            var querySource =
                FileResource.Open("kitchen-sink.graphql")
                    .NormalizeLineBreaks();
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(querySource));

            // act
            DocumentNode document = parser.Parse();

            // assert
            document.ToString().MatchSnapshot(new SnapshotNameExtension("sdl"));
            document.MatchSnapshot();
        }
    }
}
