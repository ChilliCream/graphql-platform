using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class ProvidesDeliverabilityTests
{
    [Fact]
    public void Validate_Should_Succeed_When_ProvidedFieldIsResolvableFromProvidingSchema()
    {
        // arrange
        // 'name' is resolvable on schema A (shareable), so the @provides is deliverable.
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                productById(id: ID!): Product @lookup
                featured: Product @provides(fields: "name")
            }

            type Product {
                id: ID! @shareable
                name: String @shareable
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
    public void Validate_Should_Fail_When_ProvidedFieldIsNotDeliverableFromProvidingSchema()
    {
        // arrange
        // 'price' is @external on A and owned on B, but Product has no lookup, so A cannot reach B to
        // deliver it. The @provides is therefore not deliverable by the providing schema.
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                featured: Product @provides(fields: "price")
            }

            type Product {
                id: ID! @shareable
                price: Int @external
            }
            """,
            """
            # Schema B
            type Query {
                products: [Product]
            }

            type Product {
                id: ID! @shareable
                price: Int
            }
            """
        ]);
        var log = new CompositionLog();

        // act
        var result = new SatisfiabilityValidator(schema, log).Validate();

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(log, e => e.Code == LogEntryCodes.ProvidesFieldsNotResolvable);
    }

    [Fact]
    public void Validate_Should_Succeed_When_NestedProvidedSelectionIsDeliverable()
    {
        // arrange
        // Both 'category' and its nested 'name' are resolvable on schema A, so the nested @provides
        // is deliverable end to end.
        var schema = CreateMergedSchema(
        [
            """
            # Schema A
            type Query {
                productById(id: ID!): Product @lookup
                featured: Product @provides(fields: "category { name }")
            }

            type Product {
                id: ID! @shareable
                category: Category @shareable
            }

            type Category {
                id: ID!
                name: String @shareable
            }
            """
        ]);
        var log = new CompositionLog();

        // act
        var result = new SatisfiabilityValidator(schema, log).Validate();

        // assert
        Assert.True(result.IsSuccess);
    }

    private static Types.Mutable.MutableSchemaDefinition CreateMergedSchema(string[] schemas)
    {
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(schemas),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        return merger.Merge().Value;
    }
}
