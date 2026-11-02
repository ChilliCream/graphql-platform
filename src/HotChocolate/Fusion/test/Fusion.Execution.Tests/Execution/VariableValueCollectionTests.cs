using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution;

public class VariableValueCollectionTests : FusionTestBase
{
    [Fact]
    public void Add_Should_Throw_When_NameAlreadyExists()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field: String
            }

            scalar Boolean
            """);
        var booleanType = schema.Types.GetType<IScalarTypeDefinition>("Boolean");
        var collection = new VariableValueCollection(
            new Dictionary<string, VariableValue>
            {
                ["a"] = new("a", booleanType, BooleanValueNode.True)
            });

        // act
        var exception = Assert.Throws<InvalidOperationException>(
            () => collection.Add("a", new VariableValue("a", booleanType, BooleanValueNode.False)));

        // assert
        Assert.Equal("The variable a already exists in the variable value collection.", exception.Message);
    }

    [Fact]
    public void Add_Should_ExposeValue_When_NameIsNew()
    {
        // arrange
        var schema = ComposeSchema(
            """
            type Query {
              field: String
            }

            scalar Boolean
            """);
        var booleanType = schema.Types.GetType<IScalarTypeDefinition>("Boolean");
        var collection = new VariableValueCollection([]);

        // act
        collection.Add("a", new VariableValue("a", booleanType, BooleanValueNode.True));

        // assert
        Assert.Equal(BooleanValueNode.True, collection.GetValue<BooleanValueNode>("a"));
    }

    [Fact]
    public void Empty_Should_ContainNoVariables_When_Accessed()
    {
        // arrange
        IVariableValueCollection empty = VariableValueCollection.Empty;

        // act
        var found = empty.TryGetValue<BooleanValueNode>("a", out var value);

        // assert
        Assert.True(empty.IsEmpty);
        Assert.False(found);
        Assert.Null(value);
        Assert.Empty(empty);
        Assert.Throws<GraphQLException>(() => empty.GetValue<BooleanValueNode>("a"));
    }
}
