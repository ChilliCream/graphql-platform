using System.Collections.Immutable;
using HotChocolate.Fusion.Comparers;
using HotChocolate.Fusion.Extensions;
using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Serialization;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;
using static HotChocolate.Fusion.WellKnownTypeNames;

namespace HotChocolate.Fusion;

public sealed class SourceSchemaMergerTests
{
    [Fact]
    public void Merge_WithOperationTypes_SetsOperationTypes()
    {
        // arrange
        var intType = BuiltIns.Int.Create();
        var queryType = new MutableObjectTypeDefinition(Query);
        queryType.Fields.Add(
            new MutableOutputFieldDefinition("field", intType)
            {
                DeclaringMember = queryType
            });
        var mutationType = new MutableObjectTypeDefinition(Mutation);
        mutationType.Fields.Add(
            new MutableOutputFieldDefinition("field", intType)
            {
                DeclaringMember = mutationType
            });
        var subscriptionType = new MutableObjectTypeDefinition(Subscription);
        subscriptionType.Fields.Add(
            new MutableOutputFieldDefinition("field", intType)
            {
                DeclaringMember = subscriptionType
            });
        var schema = new MutableSchemaDefinition
        {
            Types =
            {
                queryType,
                mutationType,
                subscriptionType
            }
        };
        new SourceSchemaEnricher(schema, [schema]).Enrich();
        var merger = new SourceSchemaMerger([schema]);

        // act
        var (isSuccess, _, mergedSchema, _) = merger.Merge();

        // assert
        Assert.True(isSuccess);
        Assert.NotNull(mergedSchema.QueryType);
        Assert.NotNull(mergedSchema.MutationType);
        Assert.NotNull(mergedSchema.SubscriptionType);
    }

    [Fact]
    public void Merge_WithEmptyMutationAndSubscriptionType_RemovesEmptyOperationTypes()
    {
        // arrange
        var queryType = new MutableObjectTypeDefinition(Query);
        queryType.Fields.Add(
            new MutableOutputFieldDefinition("field", BuiltIns.Int.Create())
            {
                DeclaringMember = queryType
            });
        var schema = new MutableSchemaDefinition
        {
            Types =
            {
                queryType,
                new MutableObjectTypeDefinition(Mutation),
                new MutableObjectTypeDefinition(Subscription)
            }
        };
        new SourceSchemaEnricher(schema, [schema]).Enrich();
        var merger = new SourceSchemaMerger([schema]);

        // act
        var (isSuccess, _, mergedSchema, _) = merger.Merge();

        // assert
        Assert.True(isSuccess);
        Assert.NotNull(mergedSchema.QueryType);
        Assert.False(mergedSchema.Types.ContainsName(Mutation));
        Assert.Null(mergedSchema.MutationType);
        Assert.False(mergedSchema.Types.ContainsName(Subscription));
        Assert.Null(mergedSchema.SubscriptionType);
    }

    [Theory]
    [InlineData("fusion__gatewayField")]
    [InlineData("fusion__gateway_field")]
    public void HasFusionGatewayFieldDirective_Should_RecognizeApplication_When_NameIsSupported(string name)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(
        [
            $$"""
            # Schema A
            directive @{{name}} on FIELD_DEFINITION
            type Query { ping: String @{{name}} }
            """
        ]);
        var field = schemas.Single().QueryType!.Fields["ping"];

        // act
        var isGatewayField = field.HasFusionGatewayFieldDirective();

        // assert
        Assert.True(isGatewayField);
    }

    [Theory]
    [InlineData("fusion__costOptions")]
    [InlineData("fusion__cost_options")]
    [InlineData("fusion__gatewayField")]
    [InlineData("fusion__gateway_field")]
    [InlineData("fusion__schemaMetadata")]
    [InlineData("fusion__schema_metadata")]
    public void Merge_Should_EmitCanonicalDefinitions_When_SourceDefinesFusionDirective(string name)
    {
        // arrange
        var schemas = CreateSchemaDefinitions(
        [
            $$"""
            # Schema A
            directive @{{name}} on FIELD | SCHEMA
            type Query { ping: String }
            """
        ]);
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        result.Value.DirectiveDefinitions.AsEnumerable()
            .Where(d => d.Name is "fusion__costOptions" or "fusion__cost_options"
                or "fusion__gatewayField" or "fusion__gateway_field"
                or "fusion__schemaMetadata" or "fusion__schema_metadata")
            .Select(d => d.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToArray()
            .MatchInlineSnapshot(
                """
                [
                  "fusion__costOptions",
                  "fusion__gatewayField",
                  "fusion__schemaMetadata"
                ]
                """);
    }

    [Fact]
    public void Merge_Should_IgnoreCostOptions_When_SourceAppliesLegacyDirective()
    {
        // arrange
        var schemas = CreateSchemaDefinitions(
        [
            """
            # Schema A
            directive @fusion__cost_options(defaultListSize: Int) on SCHEMA
            directive @fusion__gateway_field on FIELD_DEFINITION
            directive @fusion__schema_metadata(name: String!) on ENUM_VALUE
            schema @fusion__cost_options(defaultListSize: 7) { query: Query }
            type Query { ping: String @fusion__gateway_field }
            enum Example { VALUE @fusion__schema_metadata(name: "A") }
            """
        ]);
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        result.Value.Directives.AsEnumerable()
            .Where(d => d.Name is "fusion__costOptions" or "fusion__cost_options")
            .Select(d => d.ToString())
            .ToArray()
            .MatchInlineSnapshots([]);
    }

    [Fact]
    public void Merge_FourNamedSchemas_AddsFusionDefinitions()
    {
        // arrange
        IEnumerable<MutableSchemaDefinition> schemas =
        [
            new() { Name = "ExampleOne" },
            new() { Name = "Example_Two" },
            new() { Name = "Example__Three" },
            new() { Name = "ExampleFourFive" }
        ];

        var merger = new SourceSchemaMerger(
            schemas.ToImmutableSortedSet(
                new SchemaByNameComparer<MutableSchemaDefinition>()));

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void Merge_WithRequireCustomScalar_RetainsScalarType()
    {
        // arrange
        var sourceSchemaTextA =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    product: Product
                }

                type Product {
                    weight: Int!
                }
                """);
        var sourceSchemaTextB =
            new SourceSchemaText(
                "B",
                """
                type Product {
                    deliveryEstimate(
                        zip: String!
                        weight: Weight! @require(field: "weight")
                    ): Int!
                }

                scalar Weight
                """);
        var compositionLog = new CompositionLog();
        var sourceSchemaParser1 = new SourceSchemaParser(sourceSchemaTextA, compositionLog);
        var sourceSchemaParser2 = new SourceSchemaParser(sourceSchemaTextB, compositionLog);
        var schema1 = sourceSchemaParser1.Parse().Value;
        var schema2 = sourceSchemaParser2.Parse().Value;
        var schemas =
            ImmutableSortedSet.Create(
                new SchemaByNameComparer<MutableSchemaDefinition>(), schema1, schema2);
        new SourceSchemaEnricher(schema1, schemas).Enrich();
        new SourceSchemaEnricher(schema2, schemas).Enrich();
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Types.ContainsName("Weight"));
    }

    [Fact]
    public void Merge_WithRequireInputObject_RetainsInputObjectType()
    {
        // arrange
        var sourceSchemaTextA =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    product: Product
                }

                type Product {
                    weight: Int!
                }
                """);
        var sourceSchemaTextB =
            new SourceSchemaText(
                "B",
                """
                type Product {
                    deliveryEstimate(
                        zip: String!
                        dimension: ProductDimensionInput! @require(field: "{ weight }")
                    ): Int!
                }

                input ProductDimensionInput @inaccessible {
                    weight: Int!
                }
                """);
        var compositionLog = new CompositionLog();
        var sourceSchemaParser1 = new SourceSchemaParser(sourceSchemaTextA, compositionLog);
        var sourceSchemaParser2 = new SourceSchemaParser(sourceSchemaTextB, compositionLog);
        var schema1 = sourceSchemaParser1.Parse().Value;
        var schema2 = sourceSchemaParser2.Parse().Value;
        var schemas =
            ImmutableSortedSet.Create(
                new SchemaByNameComparer<MutableSchemaDefinition>(), schema1, schema2);
        new SourceSchemaEnricher(schema1, schemas).Enrich();
        new SourceSchemaEnricher(schema2, schemas).Enrich();
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Types.ContainsName("ProductDimensionInput"));
    }

    [Fact]
    public void Merge_WithRequireInputObject_ThatUsesCustomScalar_RetainsScalarDependency()
    {
        // arrange
        var sourceSchemaTextA =
            new SourceSchemaText(
                "A",
                """
                type Query {
                    product: Product
                }

                type Product {
                    weight: Int!
                }
                """);
        var sourceSchemaTextB =
            new SourceSchemaText(
                "B",
                """
                type Product {
                    deliveryEstimate(
                        zip: String!
                        dimension: ProductDimensionInput! @require(field: "{ weight }")
                    ): Int!
                }

                input ProductDimensionInput @inaccessible {
                    coordinate: Position!
                }

                scalar Position
                """);
        var compositionLog = new CompositionLog();
        var sourceSchemaParser1 = new SourceSchemaParser(sourceSchemaTextA, compositionLog);
        var sourceSchemaParser2 = new SourceSchemaParser(sourceSchemaTextB, compositionLog);
        var schema1 = sourceSchemaParser1.Parse().Value;
        var schema2 = sourceSchemaParser2.Parse().Value;
        var schemas =
            ImmutableSortedSet.Create(
                new SchemaByNameComparer<MutableSchemaDefinition>(), schema1, schema2);
        new SourceSchemaEnricher(schema1, schemas).Enrich();
        new SourceSchemaEnricher(schema2, schemas).Enrich();
        var merger = new SourceSchemaMerger(schemas);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Types.ContainsName("ProductDimensionInput"));
        Assert.True(result.Value.Types.ContainsName("Position"));
    }

    [Fact]
    public void Merge_Should_RetainListWrappedInputTypes_When_UsedByRequireAndLookupArguments()
    {
        // arrange
        // The @require argument and the internal lookup are removed from the client-facing
        // schema, so their list-wrapped input types are only referenced inside directive strings.
        var schemas = CreateSchemaDefinitions(
        [
            """
            type Query {
                products: [Product]
                productById(id: ID! @is(field: "id")): Product @lookup @internal
                productByVariantPrices(
                    prices: [VariantPriceInput!]! @is(field: "variants[{ price }]")
                ): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                variants: [Variant]
            }

            type Variant {
                price: Float!
            }

            input VariantPriceInput {
                price: Float!
            }
            """,
            """
            type Query {
                productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
                id: ID!
                discountedPrice(prices: [PriceInput!]! @require(field: "variants[{ price: price }]")): Float!
            }

            input PriceInput {
                price: Float!
            }
            """
        ]);
        var options = new SourceSchemaMergerOptions
        {
            AddFusionDefinitions = false
        };
        var merger = new SourceSchemaMerger(schemas, options);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        SchemaFormatter.FormatAsString(result.Value).MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query @fusion__type(schema: A) @fusion__type(schema: B) {
              products: [Product] @fusion__field(schema: A)
            }

            type Product
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "productById(id: ID!): Product"
                map: ["id"]
                path: null
                internal: true
              )
              @fusion__lookup(
                schema: A
                key: "variants { price }"
                field: "productByVariantPrices(prices: [VariantPriceInput!]!): Product"
                map: ["variants[{ price }]"]
                path: null
                internal: true
              )
              @fusion__lookup(
                schema: B
                key: "id"
                field: "productById(id: ID!): Product"
                map: ["id"]
                path: null
                internal: true
              ) {
              discountedPrice: Float!
                @fusion__field(schema: B)
                @fusion__requires(
                  schema: B
                  requirements: "variants { price }"
                  field: "discountedPrice(prices: [PriceInput!]!): Float!"
                  map: ["variants[{ price: price }]"]
                )
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              variants: [Variant] @fusion__field(schema: A)
            }

            type Variant @fusion__type(schema: A) {
              price: Float! @fusion__field(schema: A)
            }

            input PriceInput @fusion__type(schema: B) {
              price: Float! @fusion__inputField(schema: B)
            }

            input VariantPriceInput @fusion__type(schema: A) {
              price: Float! @fusion__inputField(schema: A)
            }
            """);
    }

    [Fact]
    public void Merge_DirectiveDefinitionWithDifferentArgumentOrder_MergesSuccessfully()
    {
        // arrange
        // The canonical @cacheControl definition has arguments in this order:
        // maxAge, sharedMaxAge, inheritMaxAge, scope, vary
        // and locations: OBJECT | FIELD_DEFINITION | INTERFACE | UNION.
        // This source schema defines both in a different order.
        var schemas = CreateSchemaDefinitions(
        [
            """
            enum CacheControlScope { PUBLIC PRIVATE }

            directive @cacheControl(
                vary: [String]
                scope: CacheControlScope
                inheritMaxAge: Boolean
                sharedMaxAge: Int
                maxAge: Int
            ) on UNION | INTERFACE | FIELD_DEFINITION | OBJECT

            type Foo {
                field: Int @cacheControl(maxAge: 500)
            }
            """
        ]);
        var options = new SourceSchemaMergerOptions
        {
            CacheControlMergeBehavior = DirectiveMergeBehavior.Include,
            RemoveUnreferencedDefinitions = false
        };
        var merger = new SourceSchemaMerger(schemas, options);

        // act
        var result = merger.Merge();

        // assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value.DirectiveDefinitions.ContainsName("cacheControl"));
    }
}
