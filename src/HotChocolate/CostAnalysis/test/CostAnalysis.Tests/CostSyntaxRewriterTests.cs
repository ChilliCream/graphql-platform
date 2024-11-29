using HotChocolate.CostAnalysis.Types;
using HotChocolate.Types;

namespace HotChocolate.CostAnalysis;

public sealed class CostSyntaxRewriterTests
{
    private readonly CostSyntaxRewriter _costSyntaxRewriter = new();

    [Fact]
    public void Rewrite_DefaultWeights_RemovesCostDirectives()
    {
        // arrange
        var schema = CreateSchema(
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
        var result = _costSyntaxRewriter.Rewrite(
            schema.ToDocument(),
            new CostSyntaxRewriter.Context(schema));

        // assert
        result.MatchSnapshot();
    }

    [Fact]
    public void Rewrite_NonDefaultWeights_DoesNotRemoveCostDirectives()
    {
        // arrange
        var schema = CreateSchema(
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
        var result = _costSyntaxRewriter.Rewrite(
            schema.ToDocument(),
            new CostSyntaxRewriter.Context(schema));

        // assert
        result.MatchSnapshot();
    }

    private static ISchema CreateSchema(string sourceText)
    {
        return SchemaBuilder
            .New()
            .Use(next => next)
            .AddType<Scalar>()
            .AddDirectiveType<CostDirectiveType>()
            .AddDocumentFromString(sourceText)
            .Create();
    }

    private sealed class Scalar() : StringType("Scalar");
}
