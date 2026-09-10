using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityCycleTests
{
    [Fact]
    public void Compose_Should_Fail_When_RequireFormsResolutionCycle()
    {
        // arrange
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        productById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        sku(description: String @require(field: "description")): String
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Query {
                        productInBById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        description(sku: String @require(field: "sku")): String
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        string.Join("\n\n", log.Select(e => e.Message)).MatchInlineSnapshot(
            """
            Unable to access the field 'Product.sku'.
              Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                Unable to satisfy the requirement 'description'.
                  Unable to access the required field 'Product.description'.
                    Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                      Unable to satisfy the requirement 'sku'.
                        Unable to access the required field 'Product.sku'.
                          Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                            Unable to satisfy the requirement 'description'.
                              Unable to access the required field 'Product.description'.
                                Cycle detected in requirement: B:Product.description<String> -> A:Product.sku<String> -> B:Product.description<String>.

            Unable to access the field 'Product.description'.
              Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                Unable to satisfy the requirement 'sku'.
                  Unable to access the required field 'Product.sku'.
                    Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                      Unable to satisfy the requirement 'description'.
                        Unable to access the required field 'Product.description'.
                          Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                            Unable to satisfy the requirement 'sku'.
                              Unable to access the required field 'Product.sku'.
                                Cycle detected in requirement: A:Product.sku<String> -> B:Product.description<String> -> A:Product.sku<String>.

            Unable to access the field 'Product.sku'.
              Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                Unable to satisfy the requirement 'description'.
                  Unable to access the required field 'Product.description'.
                    Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                      Unable to satisfy the requirement 'sku'.
                        Unable to access the required field 'Product.sku'.
                          Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                            Unable to satisfy the requirement 'description'.
                              Unable to access the required field 'Product.description'.
                                Cycle detected in requirement: B:Product.description<String> -> A:Product.sku<String> -> B:Product.description<String>.

            Unable to access the field 'Product.description'.
              Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                Unable to satisfy the requirement 'sku'.
                  Unable to access the required field 'Product.sku'.
                    Unable to satisfy the requirement '{ description }' on field 'A:Product.sku<String>'.
                      Unable to satisfy the requirement 'description'.
                        Unable to access the required field 'Product.description'.
                          Unable to satisfy the requirement '{ sku }' on field 'B:Product.description<String>'.
                            Unable to satisfy the requirement 'sku'.
                              Unable to access the required field 'Product.sku'.
                                Cycle detected in requirement: A:Product.sku<String> -> B:Product.description<String> -> A:Product.sku<String>.
            """);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ResolutionCycleHasNonCyclicEscape()
    {
        // arrange
        var log = new CompositionLog();
        var schemaComposer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "A",
                    """
                    type Query {
                        productById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        sku(description: String @require(field: "description")): String
                    }
                    """),
                new SourceSchemaText(
                    "B",
                    """
                    type Query {
                        productInBById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        description(sku: String @require(field: "sku")): String @shareable
                    }
                    """),
                new SourceSchemaText(
                    "C",
                    """
                    type Query {
                        productInCById(id: ID!): Product @lookup
                    }

                    type Product {
                        id: ID! @shareable
                        description: String @shareable
                    }
                    """)
            ],
            new SchemaComposerOptions { Merger = { AddFusionDefinitions = false } },
            log);

        // act
        var result = schemaComposer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }
}
