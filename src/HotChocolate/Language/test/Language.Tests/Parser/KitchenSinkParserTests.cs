using System.Text;
using Xunit;
using static CookieCrumble.Formatters.SnapshotValueFormatters;

namespace HotChocolate.Language;

public class KitchenSinkParserTests
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
        var document = parser.Parse();

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(document, "SDL:");
        snapshot.Add(document, "AST:", Json);
        snapshot.Match();
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
        var document = parser.Parse();

        // assert
        var snapshot = new Snapshot();
        snapshot.Add(document, "SDL:");
        snapshot.Add(document, "AST:", Json);
        snapshot.Match();
    }
}
