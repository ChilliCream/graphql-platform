using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class SatisfiabilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Compose_Should_Fail_When_ValueTypeFieldIsUnreachableThroughUnsharedRootField(
        bool unsharedRootFieldFirst)
    {
        // arrange
        var (firstRootField, secondRootField) = unsharedRootFieldFirst
            ? ("objectUnShared: Object!", "objectShared: Object! @shareable")
            : ("objectShared: Object! @shareable", "objectUnShared: Object!");

        var a =
            $$"""
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query {
                {{firstRootField}}
                {{secondRootField}}
            }

            type Object {
                a: ID
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.3", import: ["@key", "@shareable"])

            type Query {
                objectShared: Object! @shareable
            }

            type Object {
                b: ID
            }
            """;

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a), new SourceSchemaText("B", b)],
            new SchemaComposerOptions
            {
                Satisfiability = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true }
            },
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        string.Join(
            "\n\n",
            log.Where(e => e.Code == LogEntryCodes.UnsatisfiableQueryPath).Select(e => e.Message))
            .MatchInlineSnapshot(
                """
                Unable to access the field 'Object.b' on path 'A:Query.objectUnShared<Object>'.
                  Unable to transition between schemas 'A' and 'B' for access to field 'B:Object.b<ID>'.
                    No lookups found for type 'Object' in schema 'B'.
                """);
    }
}
