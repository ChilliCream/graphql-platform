using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class ContextCompositionTests
{
    [Fact]
    public void Compose_Should_Succeed_When_ContextFieldIsReadThroughPathOfFields()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@context", "@fromContext"])

            type Query {
                user: User
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
                amount: Float!
                amountInUserCurrency(
                    currencyCode: String @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float!
            }
            """;

        // act
        var (result, log) = Compose(new SourceSchemaText("A", a));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        PrintTypes(result.Value, "User", "Transaction").MatchInlineSnapshot(
            """
            type User
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "userContext")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_userById(id: ID!): User"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              userCurrency: Currency! @fusion__field(schema: A)
              transactions: [Transaction!]! @fusion__field(schema: A)
            }

            type Transaction
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_transactionById(id: ID!): Transaction"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              amount: Float! @fusion__field(schema: A)
              amountInUserCurrency: Float!
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: A
                  field: "amountInUserCurrency(currencyCode: String): Float!"
                  map: ["$userContext.userCurrency.isoCode"]
                )
            }
            """);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ContextFieldSelectsPerTypeCondition()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@context", "@fromContext"])

            type Query {
                a: A
                b: B
                c: C
            }

            type A @key(fields: "id") @context(name: "context1") {
                id: ID!
                someField: String
                child: Child
            }

            type B @key(fields: "id") @context(name: "context1") {
                id: ID!
                someField: String
                child: Child
            }

            type C @key(fields: "id") @context(name: "context1") {
                id: ID!
                someOtherField: String
                child: Child
            }

            type Child @key(fields: "id") {
                id: ID!
                label(
                    value: String @fromContext(
                        field: "$context1 ... on A { someField } ... on B { someField } ... on C { someOtherField }")): String
            }
            """;

        // act
        var (result, log) = Compose(new SourceSchemaText("A", a));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        PrintTypes(result.Value, "A", "B", "C", "Child").MatchInlineSnapshot(
            """
            type A
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "context1")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_aById(id: ID!): A"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              someField: String @fusion__field(schema: A)
              child: Child @fusion__field(schema: A)
            }

            type B
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "context1")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_bById(id: ID!): B"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              someField: String @fusion__field(schema: A)
              child: Child @fusion__field(schema: A)
            }

            type C
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "context1")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_cById(id: ID!): C"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              someOtherField: String @fusion__field(schema: A)
              child: Child @fusion__field(schema: A)
            }

            type Child
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_childById(id: ID!): Child"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              label: String
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: A
                  field: "label(value: String): String"
                  map: [
                    "$context1<A>.someField | $context1<B>.someField | $context1<C>.someOtherField"
                  ]
                )
            }
            """);
    }

    [Fact]
    public void Compose_Should_Succeed_When_TwoTypesOnThePathSetTheSameContext()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@context", "@fromContext"])

            type Query {
                a: A
            }

            type A @key(fields: "id") @context(name: "context1") {
                id: ID!
                prop: String!
                b: B
            }

            type B @key(fields: "id") @context(name: "context1") {
                id: ID!
                prop: String!
                c: C
            }

            type C @key(fields: "id") {
                id: ID!
                value(arg: String @fromContext(field: "$context1 { prop }")): String
            }
            """;

        // act
        var (result, log) = Compose(new SourceSchemaText("A", a));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        PrintTypes(result.Value, "A", "B", "C").MatchInlineSnapshot(
            """
            type A
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "context1")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_aById(id: ID!): A"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              prop: String! @fusion__field(schema: A)
              b: B @fusion__field(schema: A)
            }

            type B
              @fusion__type(schema: A)
              @fusion__context(schema: A, name: "context1")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_bById(id: ID!): B"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              prop: String! @fusion__field(schema: A)
              c: C @fusion__field(schema: A)
            }

            type C
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_cById(id: ID!): C"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              value: String
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: A
                  field: "value(arg: String): String"
                  map: ["$context1.prop"]
                )
            }
            """);
    }

    [Fact]
    public void Compose_Should_Succeed_When_ContextFieldIsOwnedByAnotherSubgraph()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@context", "@fromContext", "@external", "@shareable"])

            type Query {
                user: User
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency! @external
                transactions: [Transaction!]!
            }

            type Currency @shareable {
                isoCode: String! @external
            }

            type Transaction @key(fields: "id") {
                id: ID!
                amountInUserCurrency(
                    currencyCode: String @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float!
            }
            """;

        const string b =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@shareable"])

            type User @key(fields: "id") {
                id: ID!
                userCurrency: Currency!
            }

            type Currency @shareable {
                isoCode: String!
            }
            """;

        // act
        var (result, log) = Compose(new SourceSchemaText("A", a), new SourceSchemaText("B", b));

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        PrintTypes(result.Value, "User", "Transaction").MatchInlineSnapshot(
            """
            type User
              @fusion__type(schema: A)
              @fusion__type(schema: B)
              @fusion__context(schema: A, name: "userContext")
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_userById(id: ID!): User"
                map: ["id"]
                path: null
                internal: true
              )
              @fusion__lookup(
                schema: B
                key: "id"
                field: "fusion__lookup_userById(id: ID!): User"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A) @fusion__field(schema: B)
              userCurrency: Currency!
                @fusion__field(schema: A, partial: true)
                @fusion__field(schema: B)
              transactions: [Transaction!]! @fusion__field(schema: A)
            }

            type Transaction
              @fusion__type(schema: A)
              @fusion__lookup(
                schema: A
                key: "id"
                field: "fusion__lookup_transactionById(id: ID!): Transaction"
                map: ["id"]
                path: null
                internal: true
              ) {
              id: ID! @fusion__field(schema: A)
              amountInUserCurrency: Float!
                @fusion__field(schema: A)
                @fusion__requires(
                  schema: A
                  field: "amountInUserCurrency(currencyCode: String): Float!"
                  map: ["$userContext.userCurrency.isoCode"]
                )
            }
            """);
    }

    [Fact]
    public void Compose_Should_Fail_When_FromContextNamesContextThatNoTypeSets()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@context", "@fromContext"])

            type Query {
                user: User
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
                amountInUserCurrency(
                    currencyCode: String @fromContext(field: "$missingContext { userCurrency { isoCode } }")): Float!
            }
            """;

        // act
        var (result, log) = Compose(new SourceSchemaText("A", a));

        // assert
        Assert.True(result.IsFailure);
        FormatLog(log).MatchInlineSnapshot(
            """
                REQUIRE_CONTEXT_NOT_SET: The @require directive on argument 'Transaction.amountInUserCurrency(currencyCode:)' in schema 'A' references the context 'missingContext', which is not set by any type in the schema.
            """);
    }

    [Fact]
    public void Compose_Should_Fail_When_TypeWithContextualArgumentHasOnlyNonResolvableKey()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@context", "@fromContext"])

            type Query {
                user: User
            }

            type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
                transactions: [Transaction!]!
            }

            type Currency {
                isoCode: String!
            }

            type Transaction @key(fields: "id", resolvable: false) {
                id: ID!
                amountInUserCurrency(
                    currencyCode: String @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float!
            }
            """;

        // act
        var (result, log) = Compose(new SourceSchemaText("A", a));

        // assert
        Assert.True(result.IsFailure);
        FormatLog(log).MatchInlineSnapshot(
            """
                UNSATISFIABLE_QUERY_PATH: Unable to access the field 'Transaction.amountInUserCurrency' on path 'A:Query.user<User> -> A:User.transactions<Transaction>'.
                  The field 'Transaction.amountInUserCurrency' in schema 'A' reads from a context and needs a lookup for type 'Transaction' in schema 'A'.
            """);
    }

    [Fact]
    public void Compose_Should_Fail_When_ContextualArgumentHasDefaultValue()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@context", "@fromContext"])

            type Query {
                user: User
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
                amountInUserCurrency(
                    currencyCode: String = "USD" @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float!
            }
            """;

        // act
        var (result, log) = Compose(new SourceSchemaText("A", a));

        // assert
        Assert.True(result.IsFailure);
        FormatLog(log).MatchInlineSnapshot(
            """
                REQUIRE_CONTEXT_DEFAULT_VALUE: The argument 'Transaction.amountInUserCurrency(currencyCode:)' in schema 'A' reads from a context and must not have a default value.
            """);
    }

    private static string PrintTypes(MutableSchemaDefinition schema, params string[] typeNames)
        => string.Join("\n\n", typeNames.Select(n => schema.Types[n].ToString()));

    private static string FormatLog(CompositionLog log)
        => string.Join("\n", log.Select(e => $"{e.Code}: {e.Message}"));

    private static (CompositionResult<MutableSchemaDefinition> Result, CompositionLog Log) Compose(
        params SourceSchemaText[] sourceSchemas)
    {
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            sourceSchemas,
            new SchemaComposerOptions
            {
                Satisfiability = new SatisfiabilityOptions { IncludeSatisfiabilityPaths = true }
            },
            log);
        return (composer.Compose(), log);
    }
}
