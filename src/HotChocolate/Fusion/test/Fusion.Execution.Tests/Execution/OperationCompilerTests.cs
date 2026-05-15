using System.Text;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Rewriters;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;
using Microsoft.Extensions.ObjectPool;

namespace HotChocolate.Fusion.Execution;

public class OperationCompilerTests : FusionTestBase
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
        var operation = compiler.Compile("1", "1", operationDefinition);

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
        var operation = compiler.Compile("1", "1", operationDefinition);
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
        var operation = compiler.Compile("1", "1", operationDefinition);
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

    [Fact]
    public void Complementary_Fragment_Spreads_Should_Use_Full_Fragment_When_Minimal_Is_False()
    {
        // arrange
        var includedResponseNames = GetComplementaryFragmentIncludedResponseNames(minimal: false);

        // assert
        includedResponseNames.MatchInlineSnapshot(
            """
            __typename
            id
            hasDrm
            title
            publishedAt
            """);
    }

    [Fact]
    public void Complementary_Fragment_Spreads_Should_Use_Minimal_Fragment_When_Minimal_Is_True()
    {
        // arrange
        var includedResponseNames = GetComplementaryFragmentIncludedResponseNames(minimal: true);

        // assert
        includedResponseNames.MatchInlineSnapshot(
            """
            __typename
            id
            hasDrm
            """);
    }

    [Fact]
    public void Unconditional_Field_Should_Remain_Included_When_Conditional_Duplicate_Is_Excluded()
    {
        // arrange
        const string sourceText =
            """
            query ($if1: Boolean!) {
                product {
                    name
                    name @include(if: $if1)
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
                { "if1", new VariableValue("if1", nonNullBooleanType, BooleanValueNode.False) }
            });

        // act
        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", operationDefinition);
        var flags = operation.CreateIncludeFlags(variableValues);

        // assert
        var product = GetSelection(operation.RootSelectionSet, "product");
        var productSelectionSet = operation.GetSelectionSet(product);
        var name = GetSelection(productSelectionSet, "name");

        Assert.True(name.IsIncluded(flags));
    }

    public static FusionSchemaDefinition CreateSchema()
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

        return ComposeSchema(sourceText);
    }

    private static FusionSchemaDefinition CreateStreamSchema()
    {
        const string sourceText =
            """
            type Query {
                series: [Series!]!
            }

            type Series {
                id: ID!
                streams: [Stream!]!
            }

            type Stream {
                id: ID!
                title: String
                hasDrm: Boolean
                publishedAt: String
            }

            scalar Boolean
            """;

        return ComposeSchema(sourceText);
    }

    private string GetComplementaryFragmentIncludedResponseNames(bool minimal)
    {
        const string sourceText =
            """
            query TestQuery($minimal: Boolean = false) {
              series {
                streams {
                  __typename
                  ...streamFields
                }
              }
            }

            fragment streamFields on Stream {
              __typename
              ...MinimalStream @include(if: $minimal)
              ...FullStream @skip(if: $minimal)
            }

            fragment MinimalStream on Stream {
              id
              hasDrm
            }

            fragment FullStream on Stream {
              id
              title
              hasDrm
              publishedAt
            }
            """;

        var document = Utf8GraphQLParser.Parse(sourceText);
        var schema = CreateStreamSchema();
        var rewritten = new DocumentRewriter(schema).RewriteDocument(document);
        var operationDefinition = rewritten.Definitions.OfType<OperationDefinitionNode>().First();
        var booleanType = schema.Types.GetType<IScalarTypeDefinition>("Boolean");
        var variableValues = new VariableValueCollection(
            new Dictionary<string, VariableValue>
            {
                {
                    "minimal",
                    new VariableValue(
                        "minimal",
                        booleanType,
                        minimal ? BooleanValueNode.True : BooleanValueNode.False)
                }
            });

        var compiler = new OperationCompiler(schema, _fieldMapPool);
        var operation = compiler.Compile("1", "1", operationDefinition);
        var flags = operation.CreateIncludeFlags(variableValues);

        var series = GetSelection(operation.RootSelectionSet, "series");
        var seriesSelectionSet = operation.GetSelectionSet(series);
        var streams = GetSelection(seriesSelectionSet, "streams");
        var streamsSelectionSet = operation.GetSelectionSet(streams);

        return GetIncludedResponseNames(streamsSelectionSet, flags);
    }

    private static Selection GetSelection(SelectionSet selectionSet, string responseName)
    {
        if (selectionSet.TryGetSelection(responseName, out var selection))
        {
            return selection;
        }

        throw new InvalidOperationException(
            $"The selection set does not contain a `{responseName}` selection.");
    }

    private static string GetIncludedResponseNames(SelectionSet selectionSet, ulong flags)
    {
        var result = new StringBuilder();

        foreach (var selection in selectionSet.Selections)
        {
            if (selection.IsIncluded(flags))
            {
                if (result.Length > 0)
                {
                    result.AppendLine();
                }

                result.Append(selection.ResponseName);
            }
        }

        return result.ToString();
    }
}
