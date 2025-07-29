using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;
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

        var root = operation.RootSelectionSet;
        Assert.Equal(1, root.Selections.Length);

        var product = root.Selections[0];
        Assert.Equal("product", product.Field.Name);
        Assert.True(product.IsIncluded(0));

        var productSelectionSet =
            operation.GetSelectionSet(
                product,
                product.Type.NamedType<IObjectTypeDefinition>());
        Assert.Equal(1, productSelectionSet.Selections.Length);

        var id = productSelectionSet.Selections[0];
        Assert.Equal("id", id.Field.Name);
        Assert.True(id.IsIncluded(0));
    }

    [Fact]
    public void Query_NoName_With_Skip_No_Include_No_Internal()
    {
        // arrange
        const string sourceText =
            """
            query ($if1: Boolean!) {
                product @skip(if: $if1) {
                    id
                }
            }
            """;

        var document = Utf8GraphQLParser.Parse(sourceText);
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
        var schema = CreateSchema();

        var booleanType = schema.Types.GetType<IScalarTypeDefinition>("Boolean");
        var nonNullBooleanType = new NonNullType(booleanType);

        var variableValues = new VariableValueCollection(
            new Dictionary<string, VariableValue>
            {
                { "if1", new VariableValue("if1", nonNullBooleanType, BooleanValueNode.True) }
            });

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", operationDefinition);
        var flags = operation.CreateIncludeFlags(variableValues);

        // assert
        Assert.Equal("1", operation.Id);
        Assert.Equal(OperationType.Query, operation.Definition.Operation);
        Assert.Equal(schema.GetOperationType(OperationType.Query), operation.RootType);
        Assert.Equal(schema, operation.Schema);

        var root = operation.RootSelectionSet;
        Assert.Equal(1, root.Selections.Length);

        var product = root.Selections[0];
        Assert.Equal("product", product.Field.Name);
        Assert.False(product.IsIncluded(flags));

        var productSelectionSet =
            operation.GetSelectionSet(
                product,
                product.Type.NamedType<IObjectTypeDefinition>());
        Assert.Equal(1, productSelectionSet.Selections.Length);

        var id = productSelectionSet.Selections[0];
        Assert.Equal("id", id.Field.Name);
        Assert.False(id.IsIncluded(flags));
    }

    [Fact]
    public void Query_NoName_With_Internal_Id()
    {
        // arrange
        const string sourceText =
            """
            query {
                product {
                    id @fusion__requirement
                    name
                }
            }
            """;

        var document = Utf8GraphQLParser.Parse(sourceText);
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().First();
        var schema = CreateSchema();

        var variableValues = new VariableValueCollection([]);

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", operationDefinition);
        var flags = operation.CreateIncludeFlags(variableValues);

        // assert
        Assert.Equal("1", operation.Id);
        Assert.Equal(OperationType.Query, operation.Definition.Operation);
        Assert.Equal(schema.GetOperationType(OperationType.Query), operation.RootType);
        Assert.Equal(schema, operation.Schema);

        var root = operation.RootSelectionSet;
        Assert.Equal(1, root.Selections.Length);

        var product = root.Selections[0];
        Assert.Equal("product", product.Field.Name);
        Assert.True(product.IsIncluded(flags));

        var productSelectionSet =
            operation.GetSelectionSet(
                product,
                product.Type.NamedType<IObjectTypeDefinition>());
        Assert.Equal(2, productSelectionSet.Selections.Length);

        var id = productSelectionSet.Selections[0];
        Assert.True(id.IsInternal);
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
                name: String
            }

            scalar Boolean
            """;

        return SchemaParser.Parse(sourceText);
    }
}
