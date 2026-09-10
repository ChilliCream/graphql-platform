using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Satisfiability;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class QueryLessSubgraphLookupTests
{
    [Fact]
    public void GenerateLookup_Should_EmitEntityLookup_When_SubgraphHasNoQueryType()
    {
        // arrange
        // Schema A exposes Thing and owns the key. Schema B declares the same @key entity plus a
        // Mutation but NO Query type. The entity in B must still be routable: its @key implies a
        // lookup, which must be hosted even though B has no author-declared Query root.
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

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
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Mutation {
                doThing: Boolean
            }

            type Thing @key(fields: "k") @shareable {
                k: Int!
                b: String
            }
            """;

        var options = new SchemaComposerOptions();
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a), new SourceSchemaText("B", b)],
            options,
            new CompositionLog());

        // act
        var result = composer.Compose();

        // assert
        // The merged Thing must carry a lookup for source schema B, otherwise B's fields are
        // unreachable via any entity call.
        var schema = result.Value!;
        var thing = (MutableObjectTypeDefinition)schema.Types["Thing"];
        var lookupSchemas = new FusionLookupDirectiveCache(schema)
            .GetPossibleFusionLookupDirectives(thing)
            .Select(l => (string)l.Arguments[WellKnownArgumentNames.Schema].Value!)
            .Distinct()
            .ToList();

        Assert.Contains("B", lookupSchemas);
    }
}
