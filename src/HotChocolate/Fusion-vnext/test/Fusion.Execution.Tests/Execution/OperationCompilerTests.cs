using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types.Mutable;
using HotChocolate.Types.Mutable.Serialization;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public class OperationCompilerTests
{
    private readonly ObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>> _fieldMapPool =
        new DefaultObjectPool<OrderedDictionary<string, List<FieldSelectionNode>>>(new FieldMapPooledObjectPolicy());

    [Fact]
    public void Query_NoName_No_Skip_No_Include_No_Internal()
    {
        // arrange
        const string sourceText =
            """
            query {
                product {
                    id
                }
            }
            """;

        var document = Utf8GraphQLParser.Parse(sourceText);
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
        var schema = CreateSchema();

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", operationDefinition);

        // assert
        Assert.Equal("1", operation.Id);
        Assert.Equal(OperationType.Query, operation.Definition.Operation);
        Assert.Equal(schema.GetOperationType(OperationType.Query), operation.RootType);
        Assert.Equal(schema, operation.Schema);

        Assert.Equal(1, operation.RootSelectionSet.Selections.Length);
        Assert.Equal("product", operation.RootSelectionSet.Selections[0].Field.Name);
        Assert.True(operation.RootSelectionSet.Selections[0].IsIncluded(0));
    }

    public static ISchemaDefinition CreateSchema()
    {
        const string sourceText =
            """
            type Query {
                product: Product
            }

            type Product {
                id: ID
            }
            """;

        return SchemaParser.Parse(sourceText);
    }
}
