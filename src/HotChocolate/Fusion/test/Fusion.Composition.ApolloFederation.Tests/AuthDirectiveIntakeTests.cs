using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class AuthDirectiveIntakeTests
{
    [Fact]
    public void Transform_Should_RewriteToFusionDirectives_When_DirectivesAreImportedByName()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: ["@key", "@authenticated", "@requiresScopes", "@policy"]
              ) {
              query: Query
            }

            type Query {
              product(id: ID!): Product @authenticated
              secret: String @requiresScopes(scopes: [["read", "write"], ["admin"]])
              audited: String @policy(policies: [["auditor"]])
              single: String @requiresScopes(scopes: "read")
              flat: String @policy(policies: ["a", "b"])
            }

            type Product @key(fields: "id") @authenticated {
              id: ID!
              name: String @requiresScopes(scopes: [["product:read"]])
            }

            interface Node @policy(policies: [["admin"]]) {
              id: ID! @authenticated
            }

            enum Role @authenticated {
              ADMIN
            }

            scalar Secret @requiresScopes(scopes: [["secret"]])

            scalar federation__Scope
            scalar federation__Policy
            scalar FieldSet

            directive @key(fields: FieldSet!) repeatable on OBJECT | INTERFACE
            directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @requiresScopes(scopes: [[federation__Scope!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @policy(policies: [[federation__Policy!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
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
    public void Transform_Should_RewriteToFusionDirectives_When_ImportsAreRenamed()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: [
                  "@key"
                  { name: "@authenticated", as: "@auth" }
                  { name: "@requiresScopes", as: "@scopes" }
                  { name: "@policy", as: "@apolloPolicy" }
                  { name: "Scope", as: "AuthScope" }
                  { name: "Policy", as: "AuthPolicy" }
                ]
              ) {
              query: Query
            }

            type Query {
              a: String @auth
              b: String @scopes(scopes: [["read"]])
              c: String @apolloPolicy(policies: [["admin"]])
            }

            interface Node @apolloPolicy(policies: [["admin"]]) {
              id: ID! @auth
            }

            scalar AuthScope
            scalar AuthPolicy
            scalar FieldSet

            directive @key(fields: FieldSet!) repeatable on OBJECT | INTERFACE
            directive @auth on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @scopes(scopes: [[AuthScope!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @apolloPolicy(policies: [[AuthPolicy!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
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
    public void Transform_Should_RewriteToFusionDirectives_When_NamesAreNamespaced()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", as: "fed") {
              query: Query
            }

            type Query {
              a: String @fed__authenticated
              b: String @fed__requiresScopes(scopes: [["read"]])
              c: String @fed__policy(policies: [["admin"]])
            }

            scalar fed__Scope
            scalar fed__Policy

            directive @fed__authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @fed__requiresScopes(scopes: [[fed__Scope!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @fed__policy(policies: [[fed__Policy!]!]!)
              on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM
            directive @link(url: String! import: [String!] as: String) repeatable on SCHEMA
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
    public void Transform_Should_ResolveByImportedName_When_RenamesSwapTheDirectives()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: [
                  { name: "@requiresScopes", as: "@policy" }
                  { name: "@policy", as: "@requiresScopes" }
                ]
              ) {
              query: Query
            }

            type Query {
              a: String @policy(scopes: [["read"]])
              b: String @requiresScopes(policies: [["admin"]])
            }

            directive @link(url: String! import: [String!]) repeatable on SCHEMA
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
    public void Transform_Should_KeepUserType_When_ItIsNamedLikeTheApolloScalar()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@requiresScopes"]) {
              query: Query
            }

            type Query {
              policy: Policy @requiresScopes(scopes: [["read"]])
            }

            type Policy {
              id: ID!
            }

            directive @link(url: String! import: [String!]) repeatable on SCHEMA
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
    public void Transform_Should_Fail_When_ComposeDirectiveIsDeclared()
    {
        // arrange
        const string federationSdl =
            """
            schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@composeDirective"]) {
              query: Query
            }

            type Query {
              a: String
            }

            directive @composeDirective(name: String!) repeatable on SCHEMA
            directive @link(url: String! import: [String!]) repeatable on SCHEMA
            """;

        // act
        var result = FederationSchemaTransformer.Transform(federationSdl);

        // assert
        Assert.True(result.IsFailure);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void Compose_Should_MergeToFusionAuthorization_When_SubgraphUsesApolloDirectives()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(
                url: "https://specs.apollo.dev/federation/v2.6"
                import: [
                  "@key"
                  "@shareable"
                  "@authenticated"
                  { name: "@requiresScopes", as: "@scopes" }
                  "@policy"
                ]
              )

            type Query {
              product(id: ID!): Product
              me: String @authenticated
            }

            type Product @key(fields: "id") @authenticated {
              id: ID!
              price: Float @shareable @scopes(scopes: [["read"], ["admin"]])
              cost: Float @policy(policies: [["finance"]])
            }

            interface Node @policy(policies: [["admin"]]) {
              id: ID!
            }
            """;

        const string b =
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Float @shareable @requiresScopes(scopes: [["pricing"]])
            }

            interface Node @policy(policies: [["admin"]]) {
              id: ID!
            }
            """;

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a), new SourceSchemaText("B", b)],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(
            result.IsSuccess,
            result.IsSuccess ? null : string.Join("\n", log.Select(e => e.Message)));
        result.Value.MatchSnapshot(extension: ".graphql");
    }

    [Fact]
    public void Compose_Should_Fail_When_ScopeLiteralIsNotAString()
    {
        // arrange
        const string a =
            """
            extend schema
              @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@requiresScopes"])

            type Query {
              me: String @requiresScopes(scopes: [[1]])
            }
            """;

        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [new SourceSchemaText("A", a)],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        Assert.Contains(log, e => e.Code == LogEntryCodes.AuthorizationDirectiveArgumentInvalid);
    }
}
