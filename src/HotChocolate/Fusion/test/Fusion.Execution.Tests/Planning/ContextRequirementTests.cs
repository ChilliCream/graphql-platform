using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

public class ContextRequirementTests : FusionTestBase
{
    [Fact]
    public void Plan_Should_FetchContextAtTheRootAndLookUpTheField_When_ContextIsSetByTheParent()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                transactions {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_LookUpTheContextFieldInTheSecondSchema_When_OnlyTheSecondSchemaServesIt()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }

            type Currency {
                isoCode: String!
            }
            """,
            """
            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                userCurrency: Currency!
            }

            type Currency {
                isoCode: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                transactions {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_UseTheNearestAncestorAsRoot_When_TwoNestedTypesSetTheSameContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                organization: Organization
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type Organization @context(name: "scope") {
                id: ID!
                label: String!
                teams: [Team!]!
            }

            type Team @context(name: "scope") {
                id: ID!
                label: String!
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                note(label: String! @require(field: "$scope.label")): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              organization {
                teams {
                  transactions {
                    note
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ReadTheContextAndTheEntityRequirement_When_TheMapMixesBoth()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                productById(id: ID!): Product @lookup @internal
            }

            type User @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                products: [Product!]!
            }

            type Currency {
                isoCode: String!
            }

            input ConversionInput {
                currency: String!
                rate: Float!
            }

            type Product @key(fields: "id") {
                id: ID!
                amount(
                    conversion: ConversionInput!
                        @require(
                            field: "{ currency: $userContext.userCurrency.isoCode, rate: exchange.rate }"
                        )
                ): Int!
            }
            """,
            """
            type Query {
                productById(id: ID!): Product @lookup @internal
            }

            type Exchange {
                rate: Float!
            }

            type Product @key(fields: "id") {
                id: ID!
                exchange: Exchange!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                products {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SelectOnlyTheAlternativeOfTheRootType_When_TheContextPathIsTypeConditioned()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                organization: Organization
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @context(name: "account") {
                id: ID!
                locale: String!
                transactions: [Transaction!]!
            }

            type Organization @context(name: "account") {
                id: ID!
                defaultLocale: String!
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                label(
                    locale: String!
                        @require(field: "$account<User>.locale | $account<Organization>.defaultLocale")
                ): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                transactions {
                  label
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ShareTheLookupAndTheContextSelection_When_TwoFieldsReadTheSameContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
                fee(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                transactions {
                  amount
                  fee
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_UseTheInterfaceAsRoot_When_AllPossibleTypesSetTheContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                owner: Owner
                transactionById(id: ID!): Transaction @lookup @internal
            }

            interface Owner @context(name: "ownerContext") {
                id: ID!
                currency: String!
                transactions: [Transaction!]!
            }

            type Customer implements Owner {
                id: ID!
                currency: String!
                transactions: [Transaction!]!
            }

            type Business implements Owner {
                id: ID!
                currency: String!
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$ownerContext.currency")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              owner {
                transactions {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_UseTheTypeConditionAsRoot_When_OnlyThatTypeSetsTheContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                owner: Owner
                transactionById(id: ID!): Transaction @lookup @internal
            }

            interface Owner {
                id: ID!
            }

            type Customer implements Owner @context(name: "ownerContext") {
                id: ID!
                currency: String!
                transactions: [Transaction!]!
            }

            type Business implements Owner {
                id: ID!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$ownerContext.currency")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              owner {
                ... on Customer {
                  transactions {
                    amount
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_UseTheSchemaWithoutContextRequirement_When_NoAncestorSetsTheContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int! @shareable
            }
            """,
            """
            type Query {
                latest: [Transaction!]!
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount: Int! @shareable
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              latest {
                amount
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_FallBackToTheSchemaWithoutContextRequirement_When_TheContextSchemaServesASibling()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int! @shareable
                note: String!
            }
            """,
            """
            type Query {
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount: Int! @shareable
            }
            """,
            """
            type Query {
                latest: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              latest {
                amount
                note
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ReadTheContext_When_TheSameFieldIsAlsoServedWithoutContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int! @shareable
            }
            """,
            """
            type Query {
                latest: [Transaction!]!
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount: Int! @shareable
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                transactions {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_UseTheOperationRootAsRoot_When_TheQueryTypeSetsTheContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query @context(name: "tenant") {
                tenantSettings: TenantSettings!
                products: [Product!]!
                productById(id: ID!): Product @lookup @internal
            }

            type TenantSettings {
                currency: String!
            }

            type Product @key(fields: "id") {
                id: ID!
                price(currency: String! @require(field: "$tenant.tenantSettings.currency")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_FetchTheContextInTheRequiringSchema_When_TheRootStepIsInAnotherSchema()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query @context(name: "tenant") {
                tenantSettings: TenantSettings!
                products: [Product!]!
                productById(id: ID!): Product @lookup @internal
            }

            type TenantSettings {
                currency: String!
            }

            type Product @key(fields: "id") {
                id: ID!
                price(currency: String! @require(field: "$tenant.tenantSettings.currency")): Int!
            }
            """,
            """
            type Query {
                reviews: [Review!]!
            }

            type Review {
                product: Product!
            }

            type Product @key(fields: "id") {
                id: ID!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              reviews {
                product {
                  price
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_InlineTheContextOnlyInTheRequiringSchema_When_TwoSchemasHaveRootSteps()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query @context(name: "tenant") {
                tenantSettings: TenantSettings!
                products: [Product!]!
                productById(id: ID!): Product @lookup @internal
            }

            type TenantSettings {
                currency: String!
            }

            type Product @key(fields: "id") {
                id: ID!
                price(currency: String! @require(field: "$tenant.tenantSettings.currency")): Int!
            }
            """,
            """
            type Query {
                reviews: [Review!]!
            }

            type Review {
                product: Product!
            }

            type Product @key(fields: "id") {
                id: ID!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                price
              }
              reviews {
                product {
                  id
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_NotUseTheQueryContext_When_TheOperationIsAMutation()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query @context(name: "tenant") {
                tenantSettings: TenantSettings!
                products: [Product!]!
                productById(id: ID!): Product @lookup @internal
            }

            type TenantSettings {
                currency: String!
            }

            type Product @key(fields: "id") {
                id: ID!
                price(currency: String! @require(field: "$tenant.tenantSettings.currency")): Int! @shareable
            }
            """,
            """
            type Mutation {
                createProduct: Product!
            }

            type Product @key(fields: "id") {
                id: ID!
                price: Int! @shareable
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            mutation {
              createProduct {
                price
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ReadTheNearestSettingTypeAndThenTheAncestor_When_OnlySomeMembersSetTheContext()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                org: Organization
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type Organization @context(name: "account") {
                id: ID!
                currency: String!
                members: [Member!]!
            }

            interface Member {
                id: ID!
                transactions: [Transaction!]!
            }

            type User implements Member @context(name: "account") {
                id: ID!
                currency: String!
                transactions: [Transaction!]!
            }

            type Bot implements Member {
                id: ID!
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$account.currency")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              org {
                members {
                  transactions {
                    amount
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_RestrictTheRootToTheSettingTypes_When_NoAncestorSetsTheContextForAllTypes()
    {
        // arrange
        // composition rejects this schema, so the context is removed from one type of the composed schema
        var schema = ComposeSchemaWithoutContext(
            "Business",
            """
            type Query {
                owner: Owner
                transactionById(id: ID!): Transaction @lookup @internal
            }

            interface Owner {
                id: ID!
                transactions: [Transaction!]!
            }

            type Customer implements Owner @context(name: "ownerContext") {
                id: ID!
                currency: String!
                transactions: [Transaction!]!
            }

            type Business implements Owner @context(name: "ownerContext") {
                id: ID!
                currency: String!
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$ownerContext.currency")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              owner {
                transactions {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_PlanSeparateLookups_When_TheContextRootsHaveDifferentDistancesToTheTargets()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                users: [User!]!
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
                pending: [Transaction!]!
                account: Account!
            }

            type Account {
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              users {
                transactions {
                  amount
                }
                account {
                  transactions {
                    amount
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_PlanSeparateLookups_When_IndependentLookupsHaveDifferentDistancesToTheContextRoot()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                users: [User!]!
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """,
            """
            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                account: Account!
            }

            type Account {
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
            }
            """
        );

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              users {
                transactions {
                  amount
                }
                account {
                  transactions {
                    amount
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_PlanOneLookupWithTwoTargets_When_IndependentLookupsHaveTheSameDistanceToTheContextRoot()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                users: [User!]!
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """,
            """
            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                pending: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
            }
            """
        );

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              users {
                transactions {
                  amount
                }
                pending {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ReadTheAliasedContextField_When_AClientSiblingHasOtherArguments()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency(region: String): Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                userCurrency(region: "EU") {
                  isoCode
                }
                transactions {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_KeepEntityAndContextAliasesApart_When_BothPathsStartWithTheSameField()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                address(format: String): Address!
                transactions: [Transaction!]!
            }

            type Address {
                city: String! @shareable
            }

            input LabelInput {
                a: String!
                b: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                label(
                    input: LabelInput!
                        @require(field: "{ a: $userContext.address.city, b: address.city }")
                ): String!
            }
            """,
            """
            type Query {
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type Address {
                city: String! @shareable
            }

            type Transaction @key(fields: "id") {
                id: ID!
                address(format: String): Address!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                address(format: "short") {
                  city
                }
                transactions {
                  label
                }
              }
            }
            """);

        // assert
        Assert.All(
            plan.AllNodes.OfType<OperationExecutionNode>().SelectMany(n => n.GetRequirementsArray()),
            requirement => Assert.Null(requirement.InternalAlias));
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_NotAliasTheEntityPath_When_OnlyTheContextFieldCollides()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                address(format: String): Address!
                transactions: [Transaction!]!
            }

            type Address {
                city: String! @shareable
            }

            type Transaction @key(fields: "id") {
                id: ID!
                label(
                    a: String! @require(field: "$userContext.address.city")
                    b: String! @require(field: "address.city")
                ): String!
            }
            """,
            """
            type Query {
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type Address {
                city: String! @shareable
            }

            type Transaction @key(fields: "id") {
                id: ID!
                address(format: String): Address!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                address(format: "short") {
                  city
                }
                transactions {
                  label
                }
              }
            }
            """);

        // assert
        Assert.All(
            plan.AllNodes.OfType<OperationExecutionNode>().SelectMany(n => n.GetRequirementsArray()),
            requirement => Assert.Null(requirement.InternalAlias));
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ReadTheAliasedContextField_When_AnotherSchemaServesTheCollidingField()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """,
            """
            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                userCurrency(region: String): Currency!
            }

            type Currency {
                isoCode: String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                userCurrency(region: "EU") {
                  isoCode
                }
                transactions {
                  amount
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ReadTheAliasedContextField_When_TheContextPathIsTyped()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                organization: Organization
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @context(name: "account") {
                id: ID!
                locale(fallback: String): String!
                transactions: [Transaction!]!
            }

            type Organization @context(name: "account") {
                id: ID!
                defaultLocale: String!
                transactions: [Transaction!]!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                label(
                    locale: String!
                        @require(field: "$account<User>.locale | $account<Organization>.defaultLocale")
                ): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                locale(fallback: "en")
                transactions {
                  label
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_SelectSharedPrefixOnce_When_TwoContextPathsShareAPrefix()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
                symbol: String!
            }

            input CurrencyFormatInput {
                code: String!
                symbol: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amountText(
                    format: CurrencyFormatInput!
                        @require(field: "{ code: $userContext.userCurrency.isoCode, symbol: $userContext.userCurrency.symbol }")
                ): String!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                transactions {
                  amountText
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_PlanTheContextInTheIncrementalPlan_When_TheContextRootIsInsideTheDeferredFragment()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              ... @defer {
                me {
                  transactions {
                    amount
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_PlanTheContextInTheIncrementalPlan_When_TheContextRootIsTheAnchorOfTheDeferredFragment()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                userById(id: ID!): User @lookup @internal
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                ... @defer {
                  transactions {
                    amount
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ImportTheContextFromTheParentPlan_When_TheContextRootIsAnEntityAboveTheDeferredFragment()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
                me: User
                transactionById(id: ID!): Transaction @lookup @internal
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
                latestTransaction: Transaction!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amount(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              me {
                latestTransaction {
                  id
                  ... @defer {
                    amount
                  }
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    [Fact]
    public void Plan_Should_ImportTheContextFromTheParentPlan_When_TheQueryContextRootIsAboveTheDeferredFragment()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query @context(name: "tenant") {
                tenantSettings: TenantSettings!
                products: [Product!]!
                productById(id: ID!): Product @lookup @internal
            }

            type TenantSettings {
                currency: String!
            }

            type Product @key(fields: "id") {
                id: ID!
                price(currency: String! @require(field: "$tenant.tenantSettings.currency")): Int!
            }
            """);

        // act
        var plan = PlanOperation(
            schema,
            """
            {
              products {
                id
                ... @defer {
                  price
                }
              }
            }
            """);

        // assert
        MatchSnapshot(plan);
    }

    private static FusionSchemaDefinition ComposeSchemaWithoutContext(
        string typeName,
        [StringSyntax("graphql")] params string[] schemas)
    {
        var text = ComposeSchemaDocument(schemas).ToString();
        var typeStart = text.IndexOf($"type {typeName}", StringComparison.Ordinal);
        var directiveStart = text.IndexOf("@fusion__context(", typeStart, StringComparison.Ordinal);
        var directiveEnd = text.IndexOf(')', directiveStart) + 1;

        text = text.Remove(directiveStart, directiveEnd - directiveStart);

        return FusionSchemaDefinition.Create(Utf8GraphQLParser.Parse(text));
    }
}
