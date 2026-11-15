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
    public void Compose_Should_LogUnsatisfiableQueryPath_When_TheDeclaringSchemaHasNoLookupForTheTransaction()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    me: User
                }

                type User @context(name: "userContext") {
                    id: ID!
                    userCurrency: Currency!
                    transactions: [Transaction!]!
                }

                type Currency {
                    isoCode: String!
                }

                type Transaction {
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
            Unable to access the field 'Transaction.amount' on path 'A:Query.me<User> -> A:User.transactions<Transaction>'.
              The field 'Transaction.amount' in schema 'A' reads from a context and needs a lookup for type 'Transaction' in schema 'A'.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TheContextFieldIsServedOnlyByTheDeclaringSchema()
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
                    userById(id: ID!): User @lookup @internal
                }

                type User @key(fields: "id") {
                    id: ID!
                    name: String!
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
    public void Compose_Should_Succeed_When_TheContextFieldIsServedOnlyByAnotherSchema()
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
                    transactions: [Transaction!]!
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
                    userById(id: ID!): User @lookup @internal
                }

                type User @key(fields: "id") {
                    id: ID!
                    userCurrency: Currency!
                }

                type Currency {
                    isoCode: String!
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
    public void Compose_Should_LogUnsatisfiableQueryPath_When_NoSchemaResolvesTheContextFieldFromTheValue()
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
                    transactions: [Transaction!]!
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
                    userByEmail(email: String!): User @lookup @internal
                }

                type User @key(fields: "email") {
                    email: String!
                    userCurrency: Currency!
                }

                type Currency {
                    isoCode: String!
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
            Unable to access the field 'User.email' on path 'A:Query.me<User>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:User.email<String>'.
                Unable to satisfy the requirement '{ email }' for lookup 'userByEmail' in schema 'B'.
                  Unable to satisfy the requirement 'email'.
                    Unable to access the required field 'User.email' on path 'A:Query.me<User>'.
                      No other schemas contain the field 'User.email'.
            """,
            """
            Unable to access the field 'User.userCurrency' on path 'A:Query.me<User>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:User.userCurrency<Currency>'.
                Unable to satisfy the requirement '{ email }' for lookup 'userByEmail' in schema 'B'.
                  Unable to satisfy the requirement 'email'.
                    Unable to access the required field 'User.email' on path 'A:Query.me<User>'.
                      No other schemas contain the field 'User.email'.
            """,
            """
            Unable to access the field 'Transaction.amount' on path 'A:Query.me<User> -> A:User.transactions<Transaction>'.
              The field 'Transaction.amount' in schema 'A' reads 'userCurrency { isoCode }' from the context 'userContext', which cannot be resolved from type 'User'.
                Unable to satisfy the requirement 'userCurrency { isoCode }'.
                  Unable to access the required field 'User.userCurrency' on path 'A:Query.me<User>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:User.userCurrency<Currency>'.
                      No lookups found for type 'User' in schema 'B'.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TheContextPathHasAnObjectSelection()
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

                type Currency @key(fields: "id") {
                    id: ID!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(
                        currency: CurrencyInput! @require(field: "$userContext.userCurrency.{ isoCode: isoCode }")
                    ): Int!
                }

                input CurrencyInput {
                    isoCode: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    currencyById(id: ID!): Currency @lookup @internal
                }

                type Currency @key(fields: "id") {
                    id: ID!
                    isoCode: String!
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
    public void Compose_Should_LogUnsatisfiableQueryPath_When_NoSchemaResolvesAFieldInTheObjectSelectionOfAContextPath()
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

                type Currency @key(fields: "id") {
                    id: ID!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(
                        currency: CurrencyInput! @require(field: "$userContext.userCurrency.{ isoCode: isoCode }")
                    ): Int!
                }

                input CurrencyInput {
                    isoCode: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    currencyByCode(code: String!): Currency @lookup @internal
                }

                type Currency @key(fields: "code") {
                    code: String!
                    isoCode: String!
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
            Unable to access the field 'Currency.code' on path 'A:Query.me<User> -> A:User.userCurrency<Currency>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Currency.code<String>'.
                Unable to satisfy the requirement '{ code }' for lookup 'currencyByCode' in schema 'B'.
                  Unable to satisfy the requirement 'code'.
                    Unable to access the required field 'Currency.code' on path 'A:User.userCurrency<Currency>'.
                      No other schemas contain the field 'Currency.code'.
            """,
            """
            Unable to access the field 'Currency.isoCode' on path 'A:Query.me<User> -> A:User.userCurrency<Currency>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Currency.isoCode<String>'.
                Unable to satisfy the requirement '{ code }' for lookup 'currencyByCode' in schema 'B'.
                  Unable to satisfy the requirement 'code'.
                    Unable to access the required field 'Currency.code' on path 'A:User.userCurrency<Currency>'.
                      No other schemas contain the field 'Currency.code'.
            """,
            """
            Unable to access the field 'Transaction.amount' on path 'A:Query.me<User> -> A:User.transactions<Transaction>'.
              The field 'Transaction.amount' in schema 'A' reads 'userCurrency { isoCode }' from the context 'userContext', which cannot be resolved from type 'User'.
                Unable to satisfy the requirement 'userCurrency { isoCode }'.
                  Unable to access the required field 'User.userCurrency' on path 'A:Query.me<User>'.
                    Unable to access the required field 'Currency.isoCode' on path 'A:Query.me<User> -> A:User.userCurrency<Currency>'.
                      Unable to transition between schemas 'A' and 'B' for access to required field 'B:Currency.isoCode<String>'.
                        Unable to satisfy the requirement '{ code }' for lookup 'currencyByCode' in schema 'B'.
                          Unable to satisfy the requirement 'code'.
                            Unable to access the required field 'Currency.code' on path 'A:User.userCurrency<Currency>'.
                              No other schemas contain the field 'Currency.code'.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_LogUnsatisfiableQueryPath_When_NoSchemaResolvesAFieldInTheListSelectionOfAContextPath()
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
                    currencies: [Currency!]!
                    transactions: [Transaction!]!
                }

                type Currency @key(fields: "id") {
                    id: ID!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(
                        currency: [CurrencyInput!]! @require(field: "$userContext.currencies[{ isoCode: isoCode }]")
                    ): Int!
                }

                input CurrencyInput {
                    isoCode: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    currencyByCode(code: String!): Currency @lookup @internal
                }

                type Currency @key(fields: "code") {
                    code: String!
                    isoCode: String!
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
            Unable to access the field 'Currency.code' on path 'A:Query.me<User> -> A:User.currencies<Currency>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Currency.code<String>'.
                Unable to satisfy the requirement '{ code }' for lookup 'currencyByCode' in schema 'B'.
                  Unable to satisfy the requirement 'code'.
                    Unable to access the required field 'Currency.code' on path 'A:User.currencies<Currency>'.
                      No other schemas contain the field 'Currency.code'.
            """,
            """
            Unable to access the field 'Currency.isoCode' on path 'A:Query.me<User> -> A:User.currencies<Currency>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Currency.isoCode<String>'.
                Unable to satisfy the requirement '{ code }' for lookup 'currencyByCode' in schema 'B'.
                  Unable to satisfy the requirement 'code'.
                    Unable to access the required field 'Currency.code' on path 'A:User.currencies<Currency>'.
                      No other schemas contain the field 'Currency.code'.
            """,
            """
            Unable to access the field 'Transaction.amount' on path 'A:Query.me<User> -> A:User.transactions<Transaction>'.
              The field 'Transaction.amount' in schema 'A' reads 'currencies { isoCode }' from the context 'userContext', which cannot be resolved from type 'User'.
                Unable to satisfy the requirement 'currencies { isoCode }'.
                  Unable to access the required field 'User.currencies' on path 'A:Query.me<User>'.
                    Unable to access the required field 'Currency.isoCode' on path 'A:Query.me<User> -> A:User.currencies<Currency>'.
                      Unable to transition between schemas 'A' and 'B' for access to required field 'B:Currency.isoCode<String>'.
                        Unable to satisfy the requirement '{ code }' for lookup 'currencyByCode' in schema 'B'.
                          Unable to satisfy the requirement 'code'.
                            Unable to access the required field 'Currency.code' on path 'A:User.currencies<Currency>'.
                              No other schemas contain the field 'Currency.code'.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TheListSelectionOfAnEntityPathReadsOnlyAContext()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    tenant: Tenant
                    orderById(id: ID!): Order @lookup @internal
                }

                type Tenant @context(name: "tenant") {
                    id: ID!
                    currency: String!
                    settings: TenantSettings!
                    users: [User!]!
                }

                type TenantSettings {
                    locale: String!
                }

                type User @context(name: "user") {
                    id: ID!
                    locale: String!
                    orders: [Order!]!
                }

                type Order @key(fields: "id") {
                    id: ID!
                    itemCurrencies(
                        lines: [CurrencyInput!]! @require(field: "items[{ currency: $tenant.currency }]")
                    ): String!
                }

                input CurrencyInput {
                    currency: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    orderById(id: ID!): Order @lookup @internal
                }

                type Order @key(fields: "id") {
                    id: ID!
                    items: [OrderItem!]!
                }

                type OrderItem {
                    weight: Int!
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
    public void Compose_Should_LogUnsatisfiableQueryPath_When_ThePathOfAContextOnlySelectionCannotBeResolved()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    tenant: Tenant
                    orderById(id: ID!): Order @lookup @internal
                }

                type Tenant @context(name: "tenant") {
                    id: ID!
                    currency: String!
                    settings: TenantSettings!
                    users: [User!]!
                }

                type TenantSettings {
                    locale: String!
                }

                type User @context(name: "user") {
                    id: ID!
                    locale: String!
                    orders: [Order!]!
                }

                type Order @key(fields: "id") {
                    id: ID!
                    itemCurrencies(
                        lines: [CurrencyInput!]! @require(field: "items[{ currency: $tenant.currency }]")
                    ): String!
                }

                input CurrencyInput {
                    currency: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    latestOrder: Order
                }

                type Order @key(fields: "id") {
                    id: ID!
                    items: [OrderItem!]!
                }

                type OrderItem {
                    weight: Int!
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
            Unable to access the field 'Order.itemCurrencies' on path 'B:Query.latestOrder<Order>'.
              The field 'Order.itemCurrencies' in schema 'A' reads from the context 'tenant', which is not set on this path.
            """,
            """
            Unable to access the field 'Order.itemCurrencies' on path 'A:Query.tenant<Tenant> -> A:Tenant.users<User> -> A:User.orders<Order>'.
              Unable to satisfy the requirement '{ items { __typename } }' on field 'A:Order.itemCurrencies<String>'.
                Unable to satisfy the requirement 'items { __typename }'.
                  Unable to access the required field 'Order.items' on path 'A:User.orders<Order>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:Order.items<OrderItem>'.
                      No lookups found for type 'Order' in schema 'B'.
            """,
            """
            Unable to access the field 'Order.items' on path 'A:Query.tenant<Tenant> -> A:Tenant.users<User> -> A:User.orders<Order>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Order.items<OrderItem>'.
                No lookups found for type 'Order' in schema 'B'.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_AContextPathIsInTheListSelectionOfAnEntityPath()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    tenant: Tenant
                    orderById(id: ID!): Order @lookup @internal
                }

                type Tenant @context(name: "tenant") {
                    id: ID!
                    currency: String!
                    settings: TenantSettings!
                    users: [User!]!
                }

                type TenantSettings {
                    locale: String!
                }

                type User @context(name: "user") {
                    id: ID!
                    locale: String!
                    orders: [Order!]!
                }

                type Order @key(fields: "id") {
                    id: ID!
                    itemCurrencies(
                        lines: [LineInput!]! @require(field: "items[{ weight: weight, currency: $tenant.currency }]")
                    ): String!
                }

                input LineInput {
                    weight: Int!
                    currency: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    orderById(id: ID!): Order @lookup @internal
                }

                type Order @key(fields: "id") {
                    id: ID!
                    items: [OrderItem!]!
                }

                type OrderItem {
                    weight: Int!
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
    public void Compose_Should_LogUnsatisfiableQueryPath_When_NoSchemaResolvesTheNestedContextField()
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

                type Organization @context(name: "organizationContext") {
                    id: ID!
                    settings: Settings!
                    users: [User!]!
                }

                type Settings {
                    region: String!
                }

                type User @key(fields: "id") @context(name: "userContext") {
                    id: ID!
                    transactions: [Transaction!]!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(
                        conversion: ConversionInput!
                            @require(
                                field: "$organizationContext.settings.{ region: region, currency: $userContext.userCurrency.isoCode }"
                            )
                    ): Int!
                }

                input ConversionInput {
                    region: String!
                    currency: String!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    userByEmail(email: String!): User @lookup @internal
                }

                type User @key(fields: "email") {
                    email: String!
                    userCurrency: Currency!
                }

                type Currency {
                    isoCode: String!
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
            Unable to access the field 'User.email' on path 'A:Query.organization<Organization> -> A:Organization.users<User>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:User.email<String>'.
                Unable to satisfy the requirement '{ email }' for lookup 'userByEmail' in schema 'B'.
                  Unable to satisfy the requirement 'email'.
                    Unable to access the required field 'User.email' on path 'A:Organization.users<User>'.
                      No other schemas contain the field 'User.email'.
            """,
            """
            Unable to access the field 'User.userCurrency' on path 'A:Query.organization<Organization> -> A:Organization.users<User>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:User.userCurrency<Currency>'.
                Unable to satisfy the requirement '{ email }' for lookup 'userByEmail' in schema 'B'.
                  Unable to satisfy the requirement 'email'.
                    Unable to access the required field 'User.email' on path 'A:Organization.users<User>'.
                      No other schemas contain the field 'User.email'.
            """,
            """
            Unable to access the field 'Transaction.amount' on path 'A:Query.organization<Organization> -> A:Organization.users<User> -> A:User.transactions<Transaction>'.
              The field 'Transaction.amount' in schema 'A' reads 'userCurrency { isoCode }' from the context 'userContext', which cannot be resolved from type 'User'.
                Unable to satisfy the requirement 'userCurrency { isoCode }'.
                  Unable to access the required field 'User.userCurrency' on path 'A:Organization.users<User>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:User.userCurrency<Currency>'.
                      No lookups found for type 'User' in schema 'B'.
            """
        ]);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TypedAlternativesReadTheContextOfUserAndOrganization()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    user: User
                    organization: Organization
                    orderById(id: ID!): Order @lookup @internal
                }

                type User @context(name: "account") {
                    id: ID!
                    locale: String!
                    orders: [Order!]!
                }

                type Organization @context(name: "account") {
                    id: ID!
                    defaultLocale: String!
                    orders: [Order!]!
                }

                type Order @key(fields: "id") {
                    id: ID!
                    total(
                        locale: String!
                            @require(field: "$account<User>.locale | $account<Organization>.defaultLocale")
                    ): Int!
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
    public void Compose_Should_Succeed_When_TheContextTypeIsReachedAgainThroughACycle()
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
                    userById(id: ID!): User @lookup @internal
                }

                type User @context(name: "userContext") @key(fields: "id") {
                    id: ID!
                    userCurrency: Currency!
                    friends: [User!]!
                    transactions: [Transaction!]!
                }

                type Currency {
                    isoCode: String!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    owner: User!
                    amount(
                        currency: String! @require(field: "$userContext.userCurrency.isoCode")
                    ): Int!
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
    public void Compose_Should_Succeed_When_TheContextTypeIsReachedAgainThroughACycleAcrossSchemas()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    me: User
                }

                type User @context(name: "userContext") @key(fields: "id") {
                    id: ID!
                    userCurrency: Currency!
                    friends: [User!]!
                    transactions: [Transaction!]!
                }

                type Currency {
                    isoCode: String!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    owner: User!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    transactionById(id: ID!): Transaction @lookup @internal
                    userById(id: ID!): User @lookup @internal
                }

                type User @context(name: "userContext") @key(fields: "id") {
                    id: ID!
                }

                type Transaction @key(fields: "id") {
                    id: ID!
                    amount(
                        currency: String! @require(field: "$userContext.userCurrency.isoCode")
                    ): Int!
                }
                """)
        ];

        // act
        var (result, log) = ComposeSourceSchemas(sourceSchemas);

        // assert
        Assert.Empty(log);
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
    }

    [Fact]
    public void Compose_Should_LogUnsatisfiableQueryPath_When_TheAlternativeOfTheValueTypeCannotBeResolved()
    {
        // arrange
        SourceSchemaText[] sourceSchemas =
        [
            new SourceSchemaText(
                "A",
                """
                type Query {
                    user: User
                    organization: Organization
                    orderById(id: ID!): Order @lookup @internal
                }

                type User @context(name: "account") {
                    id: ID!
                    locale: String!
                    orders: [Order!]!
                }

                type Organization @context(name: "account") {
                    id: ID!
                    orders: [Order!]!
                }

                type Order @key(fields: "id") {
                    id: ID!
                    total(
                        locale: String!
                            @require(field: "$account<User>.locale | $account<Organization>.defaultLocale")
                    ): Int!
                }
                """),
            new SourceSchemaText(
                "B",
                """
                type Query {
                    organizationByCode(code: String!): Organization @lookup @internal
                }

                type Organization @key(fields: "code") {
                    code: String!
                    defaultLocale: String!
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
            Unable to access the field 'Organization.code' on path 'A:Query.organization<Organization>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Organization.code<String>'.
                Unable to satisfy the requirement '{ code }' for lookup 'organizationByCode' in schema 'B'.
                  Unable to satisfy the requirement 'code'.
                    Unable to access the required field 'Organization.code' on path 'A:Query.organization<Organization>'.
                      No other schemas contain the field 'Organization.code'.
            """,
            """
            Unable to access the field 'Organization.defaultLocale' on path 'A:Query.organization<Organization>'.
              Unable to transition between schemas 'A' and 'B' for access to field 'B:Organization.defaultLocale<String>'.
                Unable to satisfy the requirement '{ code }' for lookup 'organizationByCode' in schema 'B'.
                  Unable to satisfy the requirement 'code'.
                    Unable to access the required field 'Organization.code' on path 'A:Query.organization<Organization>'.
                      No other schemas contain the field 'Organization.code'.
            """,
            """
            Unable to access the field 'Order.total' on path 'A:Query.organization<Organization> -> A:Organization.orders<Order>'.
              The field 'Order.total' in schema 'A' reads '... on User { locale } ... on Organization { defaultLocale }' from the context 'account', which cannot be resolved from type 'Organization'.
                Unable to satisfy the requirement '... on Organization { defaultLocale }'.
                  Unable to access the required field 'Organization.defaultLocale' on path 'A:Query.organization<Organization>'.
                    Unable to transition between schemas 'A' and 'B' for access to required field 'B:Organization.defaultLocale<String>'.
                      No lookups found for type 'Organization' in schema 'B'.
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
        Assert.Equal(new ContextValue(organization, "A", null), organizationValue);
        Assert.True(scopes.TryGetValue(belowTeam, "A", "scope", out var teamValue));
        Assert.Equal(new ContextValue(team, "A", null), teamValue);
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
