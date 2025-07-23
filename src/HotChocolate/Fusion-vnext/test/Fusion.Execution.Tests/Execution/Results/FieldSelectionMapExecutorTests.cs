using System.Text.Json;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public class FieldSelectionMapExecutorTests : FusionTestBase
{
    private readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> _fieldMapPool =
        new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(new FieldMapPooledObjectPolicy());

    [Fact]
    public void ResolvePath_ObjectResult_PathNode()
    {
        // arrange
        var parser = new FieldSelectionMapParser("id | id");
        var fieldSelectionMap = parser.Parse();

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                """
                type Query {
                    id: String!
                }
                """)
            .Use(n => n)
            .Create();

        var targetType = schema.Types.GetType<ITypeDefinition>("String");

        var operationDefinition =
            Utf8GraphQLParser.Parse(
                    """
                    {
                        id
                    }
                    """)
                .Definitions.OfType<OperationDefinitionNode>().First();
        var operationCompiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = operationCompiler.Compile("1", operationDefinition);

        var jsonDocument = JsonDocument.Parse(
            """
            {
                "id": "123"
            }
            """);

        var serviceCollection = new ServiceCollection();
        HotChocolateFusionServiceCollectionExtensions.AddResultObjectPools(
            serviceCollection,
            new FusionMemoryPoolOptions());
        using var services = serviceCollection.BuildServiceProvider();
        using var scope = services.CreateScope();
        var resultPoolSession = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

        var objectResult = new ObjectResult();
        objectResult.Initialize(resultPoolSession, operation.RootSelectionSet, 0);
        objectResult["id"].SetNextValue(jsonDocument.RootElement.GetProperty("id"));

        // act
        var executor = new FieldSelectionMapExecutor();
        var context = new FieldSelectionMapExecutorContext(schema);

        context.Types.Push(targetType);
        context.InputTypes.Push(targetType);
        context.Results.Push([objectResult]);

        var result = executor.Visit(fieldSelectionMap, context);

        // assert
    }
}
