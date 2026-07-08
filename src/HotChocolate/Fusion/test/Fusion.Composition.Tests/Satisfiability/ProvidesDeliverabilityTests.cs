using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class ProvidesDeliverabilityTests
{
    [Fact]
    public void Validate_Should_Succeed_When_ProvidesReferencesExternalFieldOwnedElsewhere()
    {
        // arrange
        // The canonical @provides shape: Review.author.name is @external on the providing schema A
        // (schema A returns it by construction as part of resolving the review) and owned on schema B.
        // @provides is an optimization, never a resolvability requirement, so this composes clean and
        // satisfiability must not reject it.
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                reviewById(id: ID!): Review @lookup
            }

            type Review {
                id: ID!
                author: User @provides(fields: "name")
            }

            type User {
                id: ID! @shareable
                name: String @external
            }
            """,
            """
            # Schema B
            type Query {
                userById(id: ID!): User @lookup
            }

            type User {
                id: ID! @shareable
                name: String
            }
            """
        ]);
        var log = new CompositionLog();

        // act
        var result = new SatisfiabilityValidator(schema, log).Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_Should_Fail_When_ProvidesReferencesNonExternalFieldWithUnsatisfiableRequire()
    {
        // arrange
        // Review.product @provides(fields: "price"). On the providing schema A, price is NOT external:
        // A owns it, but it carries an @require on "cost" that can never be satisfied (cost is external
        // everywhere). A therefore cannot actually deliver price, so the @provides is not deliverable.
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                reviewById(id: ID!): Review @lookup
            }

            type Review {
                id: ID!
                product: Product @provides(fields: "price")
            }

            type Product {
                id: ID! @shareable
                price(hint: Float @require(field: "cost")): Float
                cost: Float @external
            }
            """,
            """
            # Schema B
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product {
                id: ID! @shareable
            }
            """
        ]);
        var log = new CompositionLog();

        // act
        var result = new SatisfiabilityValidator(schema, log).Validate();

        // assert
        Assert.False(result.IsSuccess);
        var entry = Assert.Single(log, e => e.Code == LogEntryCodes.ProvidesFieldsNotResolvable);
        Assert.Equal(
            "The non-external field 'price' selected by '@provides' on 'Review.product' is not "
            + "resolvable on source schema 'A'.",
            entry.Message);
    }

    private static MutableSchemaDefinition CreateMergedSchema(string[] schemas)
    {
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(schemas),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        return merger.Merge().Value;
    }
}
