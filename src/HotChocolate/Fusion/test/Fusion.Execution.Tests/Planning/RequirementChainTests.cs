using HotChocolate.Fusion.Types;

namespace HotChocolate.Fusion.Planning;

public class RequirementChainTests : FusionTestBase
{
    [Fact]
    public void Requires_Requires_One()
    {
        // arrange
        var schema = CreateRequiresRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              product {
                canAffordWithDiscount
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Requires_Many()
    {
        // arrange
        var schema = CreateRequiresRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              product {
                id
                price
                hasDiscount
                isExpensive
                isExpensiveWithDiscount
                canAfford
                canAfford2
                canAffordWithDiscount
                canAffordWithDiscount2
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_MergeRequirementLookups_When_EqualCostPlanHasFewerSurvivingSteps()
    {
        // arrange
        var schema = CreateRequiresRequiresSchema();
        var options = new OperationPlannerOptions { OperationWeight = 100.0 };

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              product {
                id
                price
                hasDiscount
                isExpensive
                isExpensiveWithDiscount
                canAfford
                canAfford2
                canAffordWithDiscount
                canAffordWithDiscount2
              }
            }
            """,
            options);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_AliasMergedLookupFields_When_RequirementVariablesDiffer()
    {
        // arrange
        var schema = CreateRequiresRequiresSchema();
        var options = new OperationPlannerOptions { OperationWeight = 100.0 };

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              product {
                isExpensive
                isExpensiveWithDiscount
                canAffordWithDiscount
                canAffordWithDiscount2
              }
            }
            """,
            options);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Requires_Two_Fields_Same_Requirement_Different_Order()
    {
        // arrange
        var schema = CreateRequiresRequiresSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            query {
              product {
                canAffordWithAndWithoutDiscount
                canAffordWithAndWithoutDiscount2
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Circular_1()
    {
        // arrange
        var schema = CreateRequiresCircularSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byNovice
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Requires_Circular_2()
    {
        // arrange
        var schema = CreateRequiresCircularSchema();

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              feed {
                byExpert
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_FetchAllPrerequisites_When_RequirementMixesNativeAndComputedFields()
    {
        // arrange
        var schema = CreateNativeAndComputedRequirementSchema(includeRaw: true);

        // act
        var plan = PlanOperation(schema, "{ product { total } }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_FetchAllPrerequisites_When_RequirementContainsOnlyComputedField()
    {
        // arrange
        var schema = CreateNativeAndComputedRequirementSchema(includeRaw: false);

        // act
        var plan = PlanOperation(schema, "{ product { total } }");

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void CreatePlan_Should_NotFetchEntryKeyAgain_When_LookupRequirementIsOnlyComputed()
    {
        // arrange
        var schema = CreateComputedOnlyRequirementSchema();

        // act
        var plan = PlanOperation(schema, "{ product { onlyComputed } }");

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition CreateNativeAndComputedRequirementSchema(bool includeRaw)
    {
        return ComposeSchema(
            """
            # name: a
            type Query {
              product: Product
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              raw: Int!
              computed(value: Int! @require(field: "value")): Int!
            }
            """,
            """
            # name: b
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              value: Int!
            }
            """,
            includeRaw
                ? """
                # name: c
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  total(input: TotalInput! @require(field: "{ raw computed }")): Int!
                }

                input TotalInput {
                  raw: Int!
                  computed: Int!
                }
                """
                : """
                # name: c
                type Query {
                  productById(id: ID!): Product @lookup @internal
                }

                type Product @key(fields: "id") {
                  id: ID!
                  total(input: TotalInput! @require(field: "{ computed }")): Int!
                }

                input TotalInput {
                  computed: Int!
                }
                """);
    }

    private static FusionSchemaDefinition CreateComputedOnlyRequirementSchema()
    {
        return ComposeSchema(
            """
            # name: a
            type Query {
              product: Product
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              raw: Int!
              computed(value: Int! @require(field: "value")): Int!
            }
            """,
            """
            # name: b
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              value: Int!
            }
            """,
            """
            # name: c
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              onlyComputed(computed: Int! @require(field: "computed")): Int!
            }
            """);
    }

    private static FusionSchemaDefinition CreateRequiresRequiresSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Float!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              product: Product
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              hasDiscount: Boolean!
            }
            """,
            """
            # name: c
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              isExpensive(price: Float! @require(field: "price")): Boolean!
              isExpensiveWithDiscount(
                hasDiscount: Boolean! @require(field: "hasDiscount")): Boolean!
            }
            """,
            """
            # name: d
            schema {
              query: Query
            }

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              canAfford(isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
              canAfford2(isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
              canAffordWithDiscount(
                isExpensiveWithDiscount: Boolean!
                  @require(field: "isExpensiveWithDiscount")): Boolean!
              canAffordWithDiscount2(
                isExpensiveWithDiscount: Boolean!
                  @require(field: "isExpensiveWithDiscount")): Boolean!
              canAffordWithAndWithoutDiscount(
                isExpensiveWithDiscount: Boolean!
                  @require(field: "isExpensiveWithDiscount")
                isExpensive: Boolean! @require(field: "isExpensive")): Boolean!
              canAffordWithAndWithoutDiscount2(
                isExpensive: Boolean! @require(field: "isExpensive")
                isExpensiveWithDiscount: Boolean!
                  @require(field: "isExpensiveWithDiscount")): Boolean!
            }
            """);
    }

    private static FusionSchemaDefinition CreateRequiresCircularSchema()
    {
        return ComposeSchema(
            """
            # name: a
            schema {
              query: Query
            }

            type Query {
              feed: [Post]
              postById(id: ID!): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              author: Author!
              byExpert(byNovice: Boolean! @require(field: "byNovice")): Boolean!
            }

            type Author @key(fields: "id") {
              id: ID!
              name: String!
              yearsOfExperience: Int!
            }
            """,
            """
            # name: b
            schema {
              query: Query
            }

            type Query {
              postById(id: ID!): Post @lookup @internal
            }

            type Post @key(fields: "id") {
              id: ID!
              byNovice(
                yearsOfExperience: Int!
                  @require(field: "author.yearsOfExperience")): Boolean!
            }
            """);
    }
}
