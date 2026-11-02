using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion;

public sealed class SatisfiabilityValidatorInterfaceObjectTests
{
    // Tests from the specification "Validate Satisfiability" additions for @interfaceObject.
    // https://graphql.github.io/composite-schemas-spec/draft/#sec-Validate-Satisfiability

    [Fact]
    public void Validate_Should_Succeed_When_CoveringLookupResolvesOpaqueValue()
    {
        // arrange
        // Source schema A defines the Media interface and a covering interface lookup; source
        // schema B contributes the projected field "reviews" through a stand-in and provides its
        // own lookup to resolve it.
        var (result, log) = ValidateSatisfiability(
        [
            """
            # name: a
            interface Media { id: ID! title: String! }
            type Book implements Media { id: ID! title: String! author: String! }
            type Movie implements Media { id: ID! title: String! director: String! }
            type Query { mediaById(id: ID!): Media @lookup }
            """,
            """
            # name: b
            type Media @interfaceObject @key(fields: "id") { id: ID! reviews: [Review!]! }
            type Review { body: String! }
            type Query {
              mediaById(id: ID!): Media @lookup @internal
              topReviewed(limit: Int = 10): [Media!]!
            }
            """
        ]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(log.IsEmpty);
    }

    [Fact]
    public void Validate_Should_Succeed_When_StandInLookupHasDistinctName()
    {
        // arrange
        // The stand-in's lookup is named distinctly from the covering lookup and marked @internal;
        // the projected field "views" is fetched through it while identity is recovered through A.
        var (result, log) = ValidateSatisfiability(
        [
            """
            # name: a
            type Query { mediaById(id: ID!): Media @lookup }
            interface Media { id: ID! title: String! }
            type Book implements Media @key(fields: "id") { id: ID! title: String! isbn: String! }
            type Movie implements Media @key(fields: "id") { id: ID! title: String! runtime: Int! }
            """,
            """
            # name: b
            type Query {
              trendingMedia: [Media!]!
              mediaByKey(id: ID!): Media @lookup @internal
            }
            type Media @interfaceObject @key(fields: "id") { id: ID! views: Int! }
            """
        ]);

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(log.IsEmpty);
    }

    [Fact]
    public void Validate_Should_Fail_When_StandInContributesFieldWithoutLookup()
    {
        // arrange
        // The stand-in contributes the non-key field "reviews" but declares no lookup, so no plan
        // can ever fetch the projected field.
        var (result, log) = ValidateSatisfiability(
        [
            """
            # name: a
            interface Media { id: ID! title: String! }
            type Book implements Media { id: ID! title: String! }
            type Movie implements Media { id: ID! title: String! }
            type Query { mediaById(id: ID!): Media @lookup }
            """,
            """
            # name: b
            type Media @interfaceObject @key(fields: "id") { id: ID! reviews: [Review!]! }
            type Review { body: String! }
            """
        ]);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        Assert.Contains(
            log,
            e => e.Message == "Source schema 'B' contributes fields to 'Media' but provides no lookup to resolve them.");
    }

    [Fact]
    public void Validate_Should_Fail_When_NoSingleSchemaCoversPossibleTypes()
    {
        // arrange
        // Source schema C adds "Photo", an implementing type source schema A does not define, so
        // no single schema's interface lookup covers the composite possible-type set.
        var (result, log) = ValidateSatisfiability(
        [
            """
            # name: a
            interface Media { id: ID! title: String! }
            type Book implements Media { id: ID! title: String! author: String! }
            type Movie implements Media { id: ID! title: String! director: String! }
            type Query { mediaById(id: ID!): Media @lookup }
            """,
            """
            # name: b
            type Media @interfaceObject @key(fields: "id") { id: ID! reviews: [Review!]! }
            type Review { body: String! }
            type Query {
              mediaById(id: ID!): Media @lookup @internal
              topReviewed(limit: Int = 10): [Media!]!
            }
            """,
            """
            # name: c
            interface Media { id: ID! title: String! }
            type Photo implements Media { id: ID! title: String! width: Int! }
            type Query { photoById(id: ID!): Photo @lookup }
            """
        ]);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            The query path 'Query.topReviewed' cannot be satisfied: values of 'Media' produced by source schema 'B' are opaque, and no source schema provides a lookup for 'Media' that covers the possible type(s) 'Photo' introduced by source schema(s) 'C'.
            """);
    }

    private static (CompositionResult Result, CompositionLog Log) ValidateSatisfiability(string[] sdl)
    {
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(sdl),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        return (satisfiabilityValidator.Validate(), log);
    }
}
