using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class ExternalKeyLookupRepro
{
    [Fact]
    public void Compose_Should_Succeed_When_KeyFieldIsExternalInTargetSchema()
    {
        // arrange
        // Schema B declares the entity with an @external key field (the Fed-1 stub idiom): the key
        // value is caller-supplied, so the entity is still resolvable from B via its @key lookup.
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@shareable"])

            type Query {
                thing: Thing
            }

            type Thing @key(fields: "k") {
                k: Int!
                a: String
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@external", "@shareable"])

            type Query {
                other: String
            }

            type Thing @key(fields: "k") @shareable {
                k: Int! @external
                b: String
            }
            """;

        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a), new SourceSchemaText("B", b)],
            new SchemaComposerOptions(),
            new CompositionLog());

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsSuccess);

        var document = result.Value.ToSyntaxNode();
        var thing = document.Definitions
            .OfType<ObjectTypeDefinitionNode>()
            .Single(type => type.Name.Value == "Thing");
        var keyField = thing.Fields.Single(field => field.Name.Value == "k");
        var fieldDirectives = keyField.Directives
            .Where(directive => directive.Name.Value == "fusion__field")
            .ToDictionary(
                directive => ((EnumValueNode)directive.Arguments
                    .Single(argument => argument.Name.Value == "schema").Value).Value);

        Assert.Single(fieldDirectives["A"].Arguments);
        Assert.Equal(
            ["schema", "sourceExternal"],
            fieldDirectives["B"].Arguments.Select(argument => argument.Name.Value));
        Assert.True(
            ((BooleanValueNode)fieldDirectives["B"].Arguments
                .Single(argument => argument.Name.Value == "sourceExternal").Value).Value);

        var lookup = thing.Directives.Single(
            directive => directive.Name.Value == "fusion__lookup"
                && ((EnumValueNode)directive.Arguments
                    .Single(argument => argument.Name.Value == "schema").Value).Value == "B");
        Assert.Equal(
            "fusion__lookup_thingByK(k: Int!): Thing",
            ((StringValueNode)lookup.Arguments
                .Single(argument => argument.Name.Value == "field").Value).Value);
    }
}
