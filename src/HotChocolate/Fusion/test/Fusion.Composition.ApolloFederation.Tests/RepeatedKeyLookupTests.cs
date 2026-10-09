using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class RepeatedKeyLookupTests
{
    [Fact]
    public void Compose_Should_Succeed_When_SameKeyIsRepeatedOnTypeAndExtension()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key"])

            type Item @key(fields: "id") {
                id: ID!
                name: String
            }

            extend type Item @key(fields: "id") {
                quantity: Int
            }

            type Query {
                item(id: ID!): Item
            }
            """;

        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a)],
            new SchemaComposerOptions(),
            new CompositionLog());

        // act
        var result = composer.Compose();

        // assert
        Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : string.Join("\n", result.Errors.Select(e => e.Message)));
        result.Value.MatchSnapshot(extension: ".graphql");
    }
}
