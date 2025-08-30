using System.Text.Json;
using HotChocolate.Buffers;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public class ResultDataMapperTests : FusionTestBase
{
    private readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> _fieldMapPool =
        new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(new FieldMapPooledObjectPolicy());

    [Fact]
    public void Resolve_Path_Single_Segment()
    {
        // arrange
        var parser = new FieldSelectionMapParser("id");
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
        var operation = operationCompiler.Compile("1", "1", operationDefinition);

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
        var writer = new PooledArrayWriter();
        var result = ResultDataMapper.Map(objectResult, fieldSelectionMap, schema, ref writer);

        // assert
        Assert.Equal("\"123\"", result?.ToString());
        writer?.Dispose();
    }

    [Fact]
    public void Resolve_Path_Two_Segments()
    {
        // arrange
        var parser = new FieldSelectionMapParser("product.id");
        var fieldSelectionMap = parser.Parse();

        var schema = SchemaBuilder.New()
            .AddDocumentFromString(
                """
                type Query {
                  product: Product
                }

                type Product {
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
                  product {
                    id
                  }
                }
                """)
                .Definitions.OfType<OperationDefinitionNode>().First();
        var operationCompiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = operationCompiler.Compile("1", "1", operationDefinition);

        var jsonDocument = JsonDocument.Parse(
            """
            {
              "product": {
                "id": "123"
              }
            }
            """);

        var serviceCollection = new ServiceCollection();
        HotChocolateFusionServiceCollectionExtensions.AddResultObjectPools(
            serviceCollection,
            new FusionMemoryPoolOptions());
        using var services = serviceCollection.BuildServiceProvider();
        using var scope = services.CreateScope();
        var resultPoolSession = scope.ServiceProvider.GetRequiredService<ResultPoolSession>();

        var rootResult = new ObjectResult();
        rootResult.Initialize(resultPoolSession, operation.RootSelectionSet, 0);

        var productSelection = operation.RootSelectionSet.Selections[0];
        var productType = productSelection.Type.NamedType<ObjectType>();
        var productSelectionSet = operation.GetSelectionSet(productSelection, productType);

        var productResult = new ObjectResult();
        productResult.Initialize(resultPoolSession, productSelectionSet, 0);
        productResult["id"].SetNextValue(jsonDocument.RootElement.GetProperty("product").GetProperty("id"));
        rootResult["product"].SetNextValue(productResult);

        // act
        var writer = new PooledArrayWriter();
        var result = ResultDataMapper.Map(rootResult, fieldSelectionMap, schema, ref writer);

        // assert
        Assert.Equal("\"123\"", result?.ToString());
    }
}
