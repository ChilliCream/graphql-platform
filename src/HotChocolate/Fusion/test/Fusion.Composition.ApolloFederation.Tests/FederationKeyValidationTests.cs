using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class FederationKeyValidationTests
{
    [Fact]
    public void Compose_Should_ReportKeyInvalidSyntax_When_FederationKeyFieldsAreUnparseable()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Item @key(fields: "id {") {
                id: ID!
            }

            type Query {
                item: Item
            }
            """;

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a)],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.False(result.IsSuccess);
        log.Select(entry => entry.ToString()).MatchInlineSnapshots(
            [
                """
                {
                  "message": "A @key directive on type 'Item' in schema 'A' contains invalid syntax in the 'fields' argument.",
                  "code": "KEY_INVALID_SYNTAX",
                  "severity": "Error",
                  "coordinate": "Item",
                  "member": "key",
                  "schema": "A",
                  "extensions": {}
                }
                """
            ]);
    }
}
