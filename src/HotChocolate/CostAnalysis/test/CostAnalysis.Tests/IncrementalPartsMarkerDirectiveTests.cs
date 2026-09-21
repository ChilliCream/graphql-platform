using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class IncrementalPartsMarkerDirectiveTests
{
    // Mirrors the internal marker directive the HotChocolate normalizer appends to an
    // operation definition that still has incremental delivery parts. The cost plan compiler
    // has no knowledge of this directive, and this test asserts it stays that way: an unknown
    // directive on the operation definition must never change the compiled plan.
    private const string MarkerDirectiveName = "hc__hasIncrementalParts";

    private const string Schema =
        """
        type Query {
            example: Example! @cost(weight: "2.0")
        }

        type Example @cost(weight: "3.0") {
            exampleField: Boolean!
        }
        """;

    private const string Operation =
        """
        query {
            example {
                ... @defer {
                    exampleField
                }
            }
        }
        """;

    [Fact]
    public async Task Compile_Produces_The_Same_Plan_With_And_Without_The_Marker_Directive()
    {
        // arrange
        var requestExecutor = await new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddResolver("Query", "example", _ => new object())
            .AddResolver("Example", "exampleField", _ => false)
            .ModifyCostOptions(o => o.DefaultResolverCost = null)
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var schemaIndex = requestExecutor.Schema.Services.GetRequiredService<CostSchemaIndex>();
        var document = Utf8GraphQLParser.Parse(Operation);
        var operation = (OperationDefinitionNode)document.Definitions[0];

        var markedOperation = operation.WithDirectives(
            [.. operation.Directives, new DirectiveNode(MarkerDirectiveName)]);
        var markedDocument = document.WithDefinitions([markedOperation]);

        // act
        var plan = CostPlanCompiler.Compile(schemaIndex, document, operation, CostAnalyses.Cost);
        var markedPlan = CostPlanCompiler.Compile(
            schemaIndex, markedDocument, markedOperation, CostAnalyses.Cost);

        var estimate = plan.EvaluateAssumedBound();
        var markedEstimate = markedPlan.EvaluateAssumedBound();

        // assert
        Assert.Equal(estimate.TypeCost, markedEstimate.TypeCost);
        Assert.Equal(estimate.FieldCost, markedEstimate.FieldCost);
    }
}
