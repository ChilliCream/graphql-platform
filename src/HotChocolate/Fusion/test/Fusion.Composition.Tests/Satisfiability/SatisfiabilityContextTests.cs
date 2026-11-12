using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Types.Mutable;
using static HotChocolate.Fusion.CompositionTestHelper;

namespace HotChocolate.Fusion.Satisfiability;

public sealed class SatisfiabilityContextTests
{
    [Fact]
    public void Compose_Should_Succeed_When_ContextIsSetByTheUserAboveTheTransaction()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
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
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_LogUnsatisfiableQueryPath_When_TransactionIsReachedWithoutTheUser()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    me: User
                    latest: [Transaction!]!
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
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Transaction.amount' on path 'A:Query.latest<Transaction>'.
              The field 'Transaction.amount' in schema 'A' reads from the context 'userContext', which is not set on this path.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_LogUnsatisfiableQueryPath_When_MixedRequirementReadsAnUnsetContext()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    me: User
                    latest: [Product!]!
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
                """),
            new SourceSchemaText(
                "B",
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
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Product.amount' on path 'A:Query.latest<Product>'.
              The field 'Product.amount' in schema 'A' reads from the context 'userContext', which is not set on this path.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_MixedRequirementReadsASetContext()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
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
                """),
            new SourceSchemaText(
                "B",
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
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TwoNestedTypesSetTheSameContext()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
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
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ContextIsSetThroughAnInterface()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    me: Customer
                    transactionById(id: ID!): Transaction @lookup @internal
                }

                interface Owner @context(name: "ownerContext") {
                    id: ID!
                    currency: String!
                }

                type Customer implements Owner {
                    id: ID!
                    currency: String!
                    transactions: [Transaction!]!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(currency: String! @require(field: "$ownerContext.currency")): Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_LogUnsatisfiableQueryPath_When_TypeBelowDoesNotImplementTheContextInterface()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    me: Customer
                    visitor: Visitor
                    transactionById(id: ID!): Transaction @lookup @internal
                }

                interface Owner @context(name: "ownerContext") {
                    id: ID!
                    currency: String!
                }

                type Customer implements Owner {
                    id: ID!
                    currency: String!
                    transactions: [Transaction!]!
                }

                type Visitor {
                    id: ID!
                    transactions: [Transaction!]!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(currency: String! @require(field: "$ownerContext.currency")): Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Transaction.amount' on path 'A:Query.visitor<Visitor> -> A:Visitor.transactions<Transaction>'.
              The field 'Transaction.amount' in schema 'A' reads from the context 'ownerContext', which is not set on this path.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ContextIsSetThroughAUnion()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    account: Account
                    transactionById(id: ID!): Transaction @lookup @internal
                }

                union Account @context(name: "accountContext") = Personal | Business

                type Personal {
                    id: ID!
                    currency: String!
                    transactions: [Transaction!]!
                }

                type Business {
                    id: ID!
                    currency: String!
                    transactions: [Transaction!]!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(currency: String! @require(field: "$accountContext<Personal>.currency | $accountContext<Business>.currency")): Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_LogUnsatisfiableQueryPath_When_ContextFieldSuppliesARequireInput()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
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
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    transactionById(id: ID!): Transaction @lookup @internal
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    fee(amount: Int! @require(field: "amount")): Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Transaction.fee' on path 'A:Query.me<User> -> A:User.transactions<Transaction>'.
              Unable to satisfy the requirement '{ amount }' on field 'B:Transaction.fee<Int>'.
                Unable to satisfy the requirement 'amount'.
                  Unable to access the required field 'Transaction.amount' on path 'A:User.transactions<Transaction>'.
                    The field 'Transaction.amount' in schema 'A' reads from the context 'userContext', which is not set on this path.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_PlainFieldSuppliesARequireInput()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
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
                    amount: Int!
                    converted(currency: String! @require(field: "$userContext.userCurrency.isoCode")): Int!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    transactionById(id: ID!): Transaction @lookup @internal
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    fee(amount: Int! @require(field: "amount")): Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_LogUnsatisfiableQueryPath_When_ContextFieldIsTheKeyOfALookup()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
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
                    reference(currency: String! @require(field: "$userContext.userCurrency.isoCode")): String! @shareable
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    transactionByReference(reference: String!): Transaction @lookup @internal
                }

                type Transaction @key(fields: "reference") {
                    reference: String! @shareable
                    fee: Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.True(result.IsFailure);
        Assert.All(log, e => Assert.Equal(LogEntryCodes.UnsatisfiableQueryPath, e.Code));
        log.Select(e => e.Message).MatchInlineSnapshots(
        [
            """
            Unable to access the field 'Transaction.fee' on path 'A:Query.me<User> -> A:User.transactions<Transaction>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Transaction.fee<Int>'.
                Unable to satisfy the requirement '{ reference }' for lookup 'transactionByReference' in schema 'B'.
                  Unable to satisfy the requirement 'reference'.
                    Unable to access the required field 'Transaction.reference' on path 'A:User.transactions<Transaction>'.
                      The field 'Transaction.reference' in schema 'A' reads from the context 'userContext', which is not set on this path.
            """
        ]);
    }

    [Fact]
    public void Extend_Should_ReplaceTheFartherValue_When_ANearerTypeSetsTheSameContext()
    {
        // arrange
        var schema = CreateMergedSchema(
            """
            type Query {
                organization: Organization
            }

            type Organization @context(name: "scope") {
                id: ID!
                teams: [Team!]!
            }

            type Team @context(name: "scope") {
                id: ID!
            }
            """);
        var scopes = new ContextScopes(schema, new SchemaSetInterner(["A"]));
        var organization = (MutableObjectTypeDefinition)schema.Types["Organization"];
        var team = (MutableObjectTypeDefinition)schema.Types["Team"];

        // act
        var belowOrganization = scopes.Extend(ContextScopeRef.Empty, organization, "A");
        var belowTeam = scopes.Extend(belowOrganization, team, "A");

        // assert
        Assert.False(scopes.Contains(ContextScopeRef.Empty, "A", "scope"));
        Assert.True(scopes.TryGetValue(belowOrganization, "A", "scope", out var organizationValue));
        Assert.Equal(new ContextValue(organization, "A"), organizationValue);
        Assert.True(scopes.TryGetValue(belowTeam, "A", "scope", out var teamValue));
        Assert.Equal(new ContextValue(team, "A"), teamValue);
    }

    [Fact]
    public void Extend_Should_ReturnTheSameScope_When_TheEntriesAreEqual()
    {
        // arrange
        var schema = CreateMergedSchema(
            """
            type Query {
                organization: Organization
            }

            type Organization @context(name: "scope") {
                id: ID!
                teams: [Team!]!
            }

            type Team {
                id: ID!
            }
            """);
        var scopes = new ContextScopes(schema, new SchemaSetInterner(["A"]));
        var organization = (MutableObjectTypeDefinition)schema.Types["Organization"];
        var team = (MutableObjectTypeDefinition)schema.Types["Team"];

        // act
        var belowOrganization = scopes.Extend(ContextScopeRef.Empty, organization, "A");
        var belowNestedOrganization = scopes.Extend(belowOrganization, organization, "A");
        var belowTeam = scopes.Extend(belowOrganization, team, "A");

        // assert
        Assert.Equal(belowOrganization, belowNestedOrganization);
        Assert.Equal(belowOrganization, belowTeam);
    }

    private static MutableSchemaDefinition CreateMergedSchema(string sourceSchema)
        => new SourceSchemaMerger(
            CreateSchemaDefinitions([sourceSchema]),
            new SourceSchemaMergerOptions { AddFusionDefinitions = false }).Merge().Value;
}
