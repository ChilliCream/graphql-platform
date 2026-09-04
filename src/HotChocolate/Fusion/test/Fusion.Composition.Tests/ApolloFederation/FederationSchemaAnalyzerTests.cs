using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Language;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class FederationSchemaAnalyzerTests
{
    [Fact]
    public void Compose_Should_SilentlyDropAuthenticationDirectives_When_Federation25SchemaUsesThem()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "products",
                    """
                    extend schema
                        @link(
                            url: "https://specs.apollo.dev/federation/v2.5"
                            import: ["@authenticated", "@requiresScopes"])

                    scalar federation__Scope

                    directive @authenticated on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM

                    directive @requiresScopes(scopes: [[federation__Scope!]!]!)
                        on FIELD_DEFINITION | OBJECT | INTERFACE | SCALAR | ENUM

                    type Query {
                        product: Product
                            @authenticated
                            @requiresScopes(scopes: [["a:read"]])
                    }

                    type Product @authenticated {
                        id: ID!
                    }
                    """)
            ],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsSuccess);
        Assert.Empty(log);

        var document = result.Value.ToSyntaxNode();
        var infrastructureDefinitions = document.Definitions
            .Where(
                definition => definition switch
                {
                    DirectiveDefinitionNode { Name.Value: "authenticated" or "requiresScopes" } => true,
                    ScalarTypeDefinitionNode { Name.Value: "federation__Scope" } => true,
                    _ => false
                })
            .ToArray();
        infrastructureDefinitions.MatchInlineSnapshot("[]");

        var types = new DocumentNode(
            document.Definitions
                .OfType<ObjectTypeDefinitionNode>()
                .Where(definition => definition.Name.Value is "Query" or "Product")
                .ToArray());
        types.ToString().MatchInlineSnapshot(
            """
            type Query @fusion__type(schema: PRODUCTS) {
              product: Product @fusion__field(schema: PRODUCTS)
            }

            type Product @fusion__type(schema: PRODUCTS) {
              id: ID! @fusion__field(schema: PRODUCTS)
            }
            """);
    }

    [Fact]
    public void Compose_Should_Fail_When_ComposeDirectiveIsImported()
    {
        // arrange
        var log = new CompositionLog();
        var composer = new SchemaComposer(
            [
                new SourceSchemaText(
                    "products",
                    """
                    extend schema
                        @link(
                            url: "https://specs.apollo.dev/federation/v2.5"
                            import: ["@composeDirective"])

                    directive @composeDirective(name: String!) repeatable on SCHEMA

                    type Query {
                        product: String
                    }
                    """)
            ],
            new SchemaComposerOptions(),
            log);

        // act
        var result = composer.Compose();

        // assert
        Assert.True(result.IsFailure);
        log.Select(entry => entry.ToString()).MatchInlineSnapshots(
        [
            """
            {
                "message": "The @composeDirective directive is not supported.",
                "code": "FEDERATION_DIRECTIVE_NOT_SUPPORTED",
                "severity": "Error",
                "schema": "products",
                "extensions": {}
            }
            """
        ]);
    }
}
