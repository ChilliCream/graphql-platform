using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using HotChocolate.PersistedOperations;
using HotChocolate.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

public sealed class CostAnalyzerEmptySelectionSetTests
{
    [Theory]
    [InlineData("{ }", 0, 0)]
    [InlineData("mutation { }", 0, 0)]
    [InlineData("{ hero { } }", 10, 2)]
    [InlineData("{ hero { ... on Droid { } } }", 10, 2)]
    public async Task Analyze_Should_CalculateCost_When_SelectionSetIsEmpty(
        string operation,
        double expectedFieldCost,
        double expectedTypeCost)
    {
        // arrange
        var document = Utf8GraphQLParser.Parse(operation);
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var context = new DocumentValidatorContext();
        context.Initialize(requestExecutor.Schema, default, document, 1, 5, 1_000, null);

        // act
        var result = Analyze(document, context);

        // assert
        Assert.Equal(expectedFieldCost, result.FieldCost);
        Assert.Equal(expectedTypeCost, result.TypeCost);
    }

    [Fact]
    public async Task Execute_Should_ReturnEmptyData_When_PersistedDocumentHasEmptySelectionSet()
    {
        // arrange
        const string documentId = "empty-selection-set";
        var services = new ServiceCollection()
            .AddMemoryCache()
            .AddGraphQL()
            .ModifyRequestOptions(o => o.PersistedOperations.SkipPersistedDocumentValidation = true)
            .AddDocumentFromString(Schema)
            .AddInMemoryOperationDocumentStorage()
            .AddCostAnalyzer()
            .AddResolver("Query", "hero", static _ => new object())
            .AddResolver("Mutation", "updateHero", static _ => new object())
            .AddResolver("Droid", "name", static _ => "R2-D2")
            .UsePersistedOperationPipeline()
            .Services
            .BuildServiceProvider();
        var requestExecutor = await services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);
        var storage = requestExecutor.Schema.Services.GetRequiredService<IOperationDocumentStorage>();
        await storage.SaveAsync(
            new OperationDocumentId(documentId),
            new OperationDocument(Utf8GraphQLParser.Parse("{ }")),
            TestContext.Current.CancellationToken);

        // act
        var result = await requestExecutor.ExecuteAsync(
            OperationRequest.FromId(documentId),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            """
            {
              "data": {}
            }
            """,
            result.ToJson());
    }

    private static CostMetrics Analyze(DocumentNode document, DocumentValidatorContext context)
    {
        var analyzerType = typeof(CostMetrics).Assembly.GetType("HotChocolate.CostAnalysis.CostAnalyzer")!;
        var analyzer = Activator.CreateInstance(
            analyzerType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [new RequestCostOptions(1_000, 1_000, true, false, null)],
            null)!;
        var operation = document.Definitions.OfType<OperationDefinitionNode>().Single();

        return (CostMetrics)analyzerType
            .GetMethod("Analyze", BindingFlags.Instance | BindingFlags.Public)!
            .Invoke(analyzer, [operation, context])!;
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddDocumentFromString(Schema)
            .AddCostAnalyzer()
            .AddResolver("Query", "hero", static _ => new object())
            .AddResolver("Mutation", "updateHero", static _ => new object())
            .AddResolver("Droid", "name", static _ => "R2-D2");

    private const string Schema =
        """
        type Query {
            hero: Droid
        }

        type Mutation {
            updateHero: Droid
        }

        type Droid {
            name: String
        }
        """;
}
