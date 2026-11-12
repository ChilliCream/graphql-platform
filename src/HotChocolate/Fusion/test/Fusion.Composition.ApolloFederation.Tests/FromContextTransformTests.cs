using HotChocolate.Fusion.Logging;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class FromContextTransformTests
{
    private const string Header =
        """
        schema @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@external", "@context", "@fromContext"]) {
          query: Query
        }

        scalar FieldSet
        scalar ContextFieldValue

        directive @key(fields: FieldSet! resolvable: Boolean = true) repeatable on OBJECT | INTERFACE
        directive @external on FIELD_DEFINITION | OBJECT
        directive @context(name: String!) repeatable on INTERFACE | OBJECT | UNION
        directive @fromContext(field: ContextFieldValue) on ARGUMENT_DEFINITION
        directive @link(url: String! import: [String!]) repeatable on SCHEMA

        """;

    [Fact]
    public void Transform_Should_KeepContextDirective_When_TypeSetsContext()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              type Query {
                user: User
              }

              type User @context(name: "userContext") {
                id: ID!
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_Should_RewriteToRequirePath_When_ContextSelectsChainOfFields()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              type Query {
                product: Product
                user: User
              }

              type User @context(name: "userContext") {
                id: ID!
                userCurrency: Currency!
              }

              type Currency {
                isoCode: String!
              }

              type Product {
                id: ID!
                price(currency: String @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_Should_RewriteToChoiceOfPaths_When_ContextSelectsPerTypeCondition()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              type Query {
                product: Product
                a: A
                b: B
                c: C
              }

              type A @context(name: "context1") {
                someField: String
              }

              type B @context(name: "context1") {
                someField: String
              }

              type C @context(name: "context1") {
                someOtherField: String
              }

              type Product {
                id: ID!
                label(value: String @fromContext(field: "$context1 ... on A { someField } ... on B { someField } ... on C { someOtherField }")): String
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_Should_KeepFieldArgument_When_ContextFieldHasArgument()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              enum Unit {
                METRIC
                IMPERIAL
              }

              type Query {
                product: Product
                user: User
              }

              type User @context(name: "ctx") {
                id: ID!
                prop(unit: Unit): String
              }

              type Product {
                id: ID!
                label(value: String @fromContext(field: "$ctx { prop(unit: METRIC) }")): String
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_Should_UseTypeConditionSegment_When_FragmentIsBelowField()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              type Query {
                product: Product
                account: Account
              }

              type Account @context(name: "ctx") {
                id: ID!
                owner: Owner
              }

              interface Owner {
                id: ID!
              }

              type User implements Owner {
                id: ID!
                locale: String
              }

              type Product {
                id: ID!
                label(value: String @fromContext(field: "$ctx { owner { ... on User { locale } } }")): String
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_Should_KeepNameTypeAndDefaultValue_When_ArgumentHasFromContext()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              type Query {
                product: Product
                user: User
              }

              type User @context(name: "userContext") {
                id: ID!
                locale: String
              }

              type Product {
                id: ID!
                label(locale: String! = "en" @fromContext(field: "$userContext { locale }")): String
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public void Transform_Should_KeepExternalField_When_OnlyContextPathUsesIt()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              type Query {
                product: Product
                user: User
              }

              type User @key(fields: "id") @context(name: "userContext") {
                id: ID!
                userCurrency: Currency! @external
              }

              type Currency {
                isoCode: String!
              }

              type Product {
                id: ID!
                price(currency: String @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsSuccess);
        Snapshot.Create()
            .Add(federationSdl, "Apollo Federation SDL", "graphql")
            .Add(result.Value, "Transformed SDL", "graphql")
            .MatchMarkdownSnapshot();
    }

    [Theory]
    [InlineData("$userContext { a b }")]
    [InlineData("userContext { a }")]
    [InlineData("$ { a }")]
    [InlineData("$userContext")]
    [InlineData("$userContext { alias: a }")]
    [InlineData("$userContext { a @skip(if: true) }")]
    [InlineData("$userContext { a } { b }")]
    [InlineData("$userContext ... on A { a b }")]
    [InlineData("$userContext ... on A { ... on B { a } }")]
    [InlineData("$userContext ... { a }")]
    [InlineData("$userContext { a ... on A { b } }")]
    [InlineData("$userContext { a")]
    public void Validate_Should_LogFromContextInvalid_When_FieldValueIsNotAContextSelection(
        string fieldValue)
    {
        // arrange
        var schema = SchemaParser.Parse(
            Header
            + $$"""
               type Query {
                 price(currency: String @fromContext(field: "{{fieldValue}}")): Float
               }
               """);
        schema.Name = "products";
        var log = new CompositionLog();

        // act
        var isValid = FederationSchemaAnalyzer.Validate(schema, log);

        // assert
        Assert.False(isValid);
        var entry = Assert.Single(log);
        Assert.Equal(LogEntryCodes.FederationFromContextInvalid, entry.Code);
        Assert.Equal("FEDERATION_FROM_CONTEXT_INVALID", entry.Code);
        Assert.Equal(
            "The @fromContext directive on argument 'Query.price(currency:)' in schema "
            + "'products' has an invalid 'field' value.",
            entry.Message);
    }

    [Fact]
    public void Transform_Should_Fail_When_FromContextFieldValueIsInvalid()
    {
        // arrange
        const string federationSdl =
            Header
            + """
              type Query {
                price(currency: String @fromContext(field: "$userContext { a b }")): Float
              }
              """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Preprocess_Should_ReplaceFromContextWithRequire_When_SourceSchemaDeclaresNoDirectives()
    {
        // arrange
        var log = new CompositionLog();
        var schema = new SourceSchemaParser(
            new SourceSchemaText(
                "Products",
                """
                extend schema
                  @link(url: "https://specs.apollo.dev/federation/v2.8", import: ["@key", "@external", "@context", "@fromContext"])

                type Query {
                  product: Product
                  user: User
                }

                type User @key(fields: "id") @context(name: "userContext") {
                  id: ID!
                  userCurrency: Currency! @external
                }

                type Currency {
                  isoCode: String!
                }

                type Product {
                  id: ID!
                  price(currency: String @fromContext(field: "$userContext { userCurrency { isoCode } }")): Float
                }
                """),
            log).Parse().Value;

        // act
        var result = new SourceSchemaPreprocessor(schema, [], log).Preprocess();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);
        Assert.Equal(
            "price(currency: String @require(field: \"$userContext.userCurrency.isoCode\")): Float",
            schema.Types.GetType<MutableObjectTypeDefinition>("Product").Fields["price"].ToString());
    }
}
