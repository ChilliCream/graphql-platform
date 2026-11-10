using HotChocolate.Execution;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes;

public class ExecutionNodeConditionAlternativesTests : FusionTestBase
{
    private static readonly FusionSchemaDefinition s_schema = ComposeSchema(
        """
        type Query {
            foo: String
        }

        scalar Boolean
        """);

    [Fact]
    public void IsSkipped_Should_ReturnFalse_When_ConditionsAndAlternativesAreEmpty()
    {
        // arrange
        var variables = CreateVariables(("a", false));

        // act
        var skipped = ExecutionNode.IsSkipped(variables, [], []);

        // assert
        Assert.False(skipped);
    }

    [Fact]
    public void IsSkipped_Should_ReturnTrue_When_ConditionFailsAndNoAlternativesExist()
    {
        // arrange
        var variables = CreateVariables(("a", false));

        // act
        var skipped = ExecutionNode.IsSkipped(variables, [Condition("a", true)], []);

        // assert
        Assert.True(skipped);
    }

    [Fact]
    public void IsSkipped_Should_ReturnFalse_When_OneAlternativeHasAllConditionsPassing()
    {
        // arrange
        var variables = CreateVariables(("a", false), ("b", true), ("c", true));
        ExecutionNodeCondition[][] alternatives =
        [
            [Condition("a", true)],
            [Condition("b", true), Condition("c", true)]
        ];

        // act
        var skipped = ExecutionNode.IsSkipped(variables, [], alternatives);

        // assert
        Assert.False(skipped);
    }

    [Fact]
    public void IsSkipped_Should_ReturnTrue_When_NoAlternativeHasAllConditionsPassing()
    {
        // arrange
        var variables = CreateVariables(("a", false), ("b", true), ("c", false));
        ExecutionNodeCondition[][] alternatives =
        [
            [Condition("a", true)],
            [Condition("b", true), Condition("c", true)]
        ];

        // act
        var skipped = ExecutionNode.IsSkipped(variables, [], alternatives);

        // assert
        Assert.True(skipped);
    }

    [Fact]
    public void IsSkipped_Should_ReturnTrue_When_ConditionFailsAndAnAlternativePasses()
    {
        // arrange
        var variables = CreateVariables(("a", false), ("b", true));

        // act
        var skipped = ExecutionNode.IsSkipped(
            variables,
            [Condition("a", true)],
            [[Condition("b", true)]]);

        // assert
        Assert.True(skipped);
    }

    [Fact]
    public void IsSkipped_Should_Throw_When_AlternativeReferencesMissingVariable()
    {
        // arrange
        var variables = CreateVariables(("a", false));

        // act
        void Act() => ExecutionNode.IsSkipped(variables, [], [[Condition("missing", true)]]);

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Act);
        Assert.Equal(ThrowHelper.MissingBooleanVariable("missing").Message, exception.Message);
    }

    private static ExecutionNodeCondition Condition(string variableName, bool passingValue)
        => new() { VariableName = variableName, PassingValue = passingValue };

    private static VariableValueCollection CreateVariables(params (string Name, bool Value)[] values)
    {
        var booleanType = new NonNullType(s_schema.Types.GetType<IScalarTypeDefinition>("Boolean"));
        var variables = new Dictionary<string, VariableValue>();

        foreach (var (name, value) in values)
        {
            variables.Add(
                name,
                new VariableValue(name, booleanType, value ? BooleanValueNode.True : BooleanValueNode.False));
        }

        return new VariableValueCollection(variables);
    }
}
