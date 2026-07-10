using HotChocolate.Fusion.Logging;
using HotChocolate.Fusion.Options;
using HotChocolate.Fusion.Results;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.ApolloFederation;

public sealed class ApolloFederationCompatibilityTests
{
    [Fact]
    public void Compose_Should_RequireExplicitCompatibility_When_InterfaceObjectKeyIsNonResolvable()
    {
        // act
        var strictResult = Compose(allowNonResolvableInterfaceObjects: false);
        var compatibilityResult = Compose(allowNonResolvableInterfaceObjects: true);

        // assert
        Assert.True(strictResult.IsFailure);
        Assert.True(
            compatibilityResult.IsSuccess,
            compatibilityResult.IsSuccess ? null : compatibilityResult.Errors[0].Message);
    }

    private static CompositionResult<MutableSchemaDefinition> Compose(
        bool allowNonResolvableInterfaceObjects)
    {
        var options = new SchemaComposerOptions();
        options.ApolloFederationCompatibility.AllowNonResolvableInterfaceObjects =
            allowNonResolvableInterfaceObjects;

        return new SchemaComposer(
            [
                new SourceSchemaText("A", SchemaA),
                new SourceSchemaText("B", SchemaB)
            ],
            options,
            new CompositionLog()).Compose();
    }

    private const string SchemaA =
        """
        extend schema
          @link(url: "https://specs.apollo.dev/federation/v2.6", import: ["@key"])

        type Query {
          a: Node
        }

        interface Node @key(fields: "id") {
          id: ID!
        }

        type NodeImpl implements Node @key(fields: "id") {
          id: ID!
        }
        """;

    private const string SchemaB =
        """
        extend schema
          @link(
            url: "https://specs.apollo.dev/federation/v2.6"
            import: ["@key", "@interfaceObject"])

        type Query {
          b: Node
        }

        type Node @key(fields: "id", resolvable: false) @interfaceObject {
          id: ID!
          field: String
        }
        """;
}
