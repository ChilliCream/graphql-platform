using HotChocolate.Fusion.Collections;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class RequirementsValidatorTests
{
    [Fact]
    public void SplitCompositeKey()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                }
                """,
                """
                # Schema B
                type Query {
                    productById(id: ID!): Product @lookup
                }

                type Product {
                    id: ID!
                    keyField1: Int!
                }
                """,
                """
                # Schema C
                type Query {
                    productByKeyField1(keyField1: Int!): Product @lookup
                }

                type Product {
                    keyField2: Int!
                }
                """,
                """
                # Schema D
                type Query {
                    productByKey(keyField1: Int!, keyField2: Int!): Product @lookup
                }

                type Product {
                    keyField1: Int!
                    keyField2: Int!
                    specialField: Int!
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var queryType = (MutableObjectTypeDefinition)schema.Types["Query"];
        var productByIdField = queryType.Fields["productById"];
        var lookupCache = new FusionLookupDirectiveCache(schema);
        var requirementsValidator =
            new RequirementsValidator(
                schema,
                lookupCache,
                new SatisfiabilityFactsBuilder(schema, lookupCache).Build(),
                includeSatisfiabilityPaths: false);
        var selectedValue = new FieldSelectionMapParser("specialField").Parse();
        var selectionSet =
            new ValueSelectionToSelectionSetRewriter(schema).Rewrite(
                selectedValue,
                productByIdField.Type.AsTypeDefinition());
        var parentPathItem = new SatisfiabilityPathItem(productByIdField, queryType, "A");

        // act
        var errors =
            requirementsValidator.Validate(
                selectionSet,
                (MutableObjectTypeDefinition)productByIdField.Type,
                parentPathItem,
                ancestors: [],
                excludeSchemaName: "A");

        // assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_Should_Fail_When_NestedRequirementLeafIsOnlyInRequiringSchema()
    {
        // arrange
        var merger = new SourceSchemaMerger(
            CreateSchemaDefinitions(
            [
                """
                # Schema A
                type Query {
                    e0: E0 @shareable
                    aE0ById(id: ID!): E0 @lookup @internal
                    aE2ById(id: ID!): E2 @lookup @internal
                }

                type E0 {
                    id: ID! @shareable
                    next: E1 @shareable
                    r(req: String @require(field: "next.next.f_a")): String
                }

                type E1 {
                    id: ID! @shareable
                    next: E2 @shareable
                }

                type E2 {
                    id: ID! @shareable
                    f_a: String
                }
                """,
                """
                # Schema B
                type Query {
                    e0: E0 @shareable
                    bE1ById(id: ID!): E1 @lookup @internal
                }

                type E0 {
                    id: ID! @shareable
                    next: E1 @shareable
                }

                type E1 {
                    id: ID! @shareable
                    next: E2 @shareable
                }

                type E2 {
                    id: ID! @shareable
                }
                """
            ]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false });

        var schema = merger.Merge().Value;
        var log = new CompositionLog();
        var options = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true };
        var satisfiabilityValidator = new SatisfiabilityValidator(schema, log, options);

        // act
        var result = satisfiabilityValidator.Validate();

        // assert
        Assert.True(result.IsFailure);
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'E0.r' on path 'A:Query.e0<E0>'.
              Unable to satisfy the requirement '{ next { next { f_a } } }' on field 'A:E0.r<String>'.
                Unable to satisfy the requirement 'next { next { f_a } }'.
                  Unable to access the required field 'E0.next' on path 'A:Query.e0<E0>'.
                    Unable to access the required field 'E1.next' on path 'A:Query.e0<E0> -> A:E0.next<E1>'.
                      Unable to access the required field 'E2.f_a' on path 'A:Query.e0<E0> -> A:E0.next<E1> -> A:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
                      Unable to access the required field 'E2.f_a' on path 'A:Query.e0<E0> -> A:E0.next<E1> -> B:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
                    Unable to access the required field 'E1.next' on path 'A:Query.e0<E0> -> B:E0.next<E1>'.
                      Unable to access the required field 'E2.f_a' on path 'A:Query.e0<E0> -> B:E0.next<E1> -> A:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
                      Unable to access the required field 'E2.f_a' on path 'A:Query.e0<E0> -> B:E0.next<E1> -> B:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
            """,
            """
            Unable to access the field 'E0.r' on path 'B:Query.e0<E0>'.
              Unable to satisfy the requirement '{ next { next { f_a } } }' on field 'A:E0.r<String>'.
                Unable to satisfy the requirement 'next { next { f_a } }'.
                  Unable to access the required field 'E0.next' on path 'B:Query.e0<E0>'.
                    Unable to access the required field 'E1.next' on path 'B:Query.e0<E0> -> A:E0.next<E1>'.
                      Unable to access the required field 'E2.f_a' on path 'B:Query.e0<E0> -> A:E0.next<E1> -> A:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
                      Unable to access the required field 'E2.f_a' on path 'B:Query.e0<E0> -> A:E0.next<E1> -> B:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
                    Unable to access the required field 'E1.next' on path 'B:Query.e0<E0> -> B:E0.next<E1>'.
                      Unable to access the required field 'E2.f_a' on path 'B:Query.e0<E0> -> B:E0.next<E1> -> A:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
                      Unable to access the required field 'E2.f_a' on path 'B:Query.e0<E0> -> B:E0.next<E1> -> B:E1.next<E2>'.
                        No other schemas contain the field 'E2.f_a'.
            """
        ]);
    }
}
