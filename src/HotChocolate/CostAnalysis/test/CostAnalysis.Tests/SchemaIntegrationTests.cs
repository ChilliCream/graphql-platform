using HotChocolate.CostAnalysis.Types;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.CostAnalysis;

public sealed class SchemaIntegrationTests
{
    private readonly CostSyntaxRewriter _costSyntaxRewriter = new();

    [Fact]
    public async Task Rewrite_DefaultWeights_RemovesCostDirectives()
    {
        // arrange
        var schema = await CreateSchemaAsync(
            """
            type Query {
                compositeField(
                    inputArg: Input @cost(weight: "1")      # ARGUMENT_DEFINITION (input type)
                    listArg: [Scalar] @cost(weight: "1")    # ARGUMENT_DEFINITION (list type)
                    scalarArg: Scalar @cost(weight: "0")    # ARGUMENT_DEFINITION (scalar type)
                ): CompositeType @cost(weight: "1")         # FIELD_DEFINITION (composite type)
            }

            type CompositeType @cost(weight: "1") {         # OBJECT
                listField: [ElementType] @cost(weight: "1") # FIELD_DEFINITION (list type)
            }

            type ElementType {
                scalarField: Scalar @cost(weight: "0")      # FIELD_DEFINITION (scalar type)
            }

            input Input {
                inputField: Input @cost(weight: "1")        # INPUT_FIELD_DEFINITION (input type)
                listField: [Scalar] @cost(weight: "1")      # INPUT_FIELD_DEFINITION (list type)
                scalarField: Scalar @cost(weight: "0")      # INPUT_FIELD_DEFINITION (scalar type)
            }

            enum Enum @cost(weight: "0") { ENUM_VALUE }     # ENUM

            scalar Scalar @cost(weight: "0")                # SCALAR
            """);

        // act
        var result = schema.ToDocument();

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public async Task Rewrite_NonDefaultWeights_DoesNotRemoveCostDirectives()
    {
        // arrange
        var schema = await CreateSchemaAsync(
            """
            type Query {
                compositeField(
                    inputArg: Input @cost(weight: "0")      # ARGUMENT_DEFINITION (input type)
                    listArg: [Scalar] @cost(weight: "0")    # ARGUMENT_DEFINITION (list type)
                    scalarArg: Scalar @cost(weight: "1")    # ARGUMENT_DEFINITION (scalar type)
                ): CompositeType @cost(weight: "0")         # FIELD_DEFINITION (composite type)
            }

            type CompositeType @cost(weight: "0") {         # OBJECT
                listField: [ElementType] @cost(weight: "0") # FIELD_DEFINITION (list type)
            }

            type ElementType {
                scalarField: Scalar @cost(weight: "1")      # FIELD_DEFINITION (scalar type)
            }

            input Input {
                inputField: Input @cost(weight: "0")        # INPUT_FIELD_DEFINITION (input type)
                listField: [Scalar] @cost(weight: "0")      # INPUT_FIELD_DEFINITION (list type)
                scalarField: Scalar @cost(weight: "1")      # INPUT_FIELD_DEFINITION (scalar type)
            }

            enum Enum @cost(weight: "1") { ENUM_VALUE }     # ENUM

            scalar Scalar @cost(weight: "1")                # SCALAR
            """);

        // act
        var result = schema.ToDocument();

        // assert
        result.MatchSnapshot();
    }

    private static async Task<ISchema> CreateSchemaAsync(string sourceText)
        => await new ServiceCollection()
            .AddGraphQLServer()
            .AddType<Scalar>()
            .AddDocumentFromString(sourceText)
            .UseField(next => next)
            .BuildSchemaAsync();

    private sealed class Scalar() : StringType("Scalar");
}
