using System.Diagnostics.CodeAnalysis;
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
    public void Plan_Should_Throw_When_NoAncestorSetsTheContext()
    {
        // arrange
        // composition rejects this schema, so the context is removed from the composed schema
        var schema = ComposeSchemaWithoutContext(
            "User",
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
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(
                schema,
                """
                {
                  me {
                    transactions {
                      amount
                    }
                  }
                }
                """));

        // assert
        Assert.Equal(
            "The context 'userContext' required by field 'Transaction.amount' is not set on the query path.",
            exception.Message);
    }

    [Fact]
    public void Plan_Should_Throw_When_OnlySomePossibleTypesOfTheNearestAncestorSetTheContext()
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
        var exception = Assert.Throws<InvalidOperationException>(
            () => PlanOperation(
                schema,
                """
                {
                  owner {
                    transactions {
                      amount
                    }
                  }
                }
                """));

        // assert
        Assert.Equal(
            "The context 'ownerContext' required by field 'Transaction.amount' is set for only some of the possible types on the query path.",
            exception.Message);
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
