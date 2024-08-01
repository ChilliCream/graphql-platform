using HotChocolate.Language;

namespace HotChocolate.Skimmed.Tests;

public class ArgumentTests
{
    [Fact]
    public void Argument_WithStringValueNode_CreatesInstanceWithNameAndValueNode()
    {
        // arrange
        var name = "test";
        var value = "value";

        // act
        var argument = new ArgumentAssignment(name, value);

        // assert
        Assert.Equal(name, argument.Name);
        Assert.IsType<StringValueNode>(argument.Value);
        Assert.Equal(value, ((StringValueNode)argument.Value).Value);
    }

    [Fact]
    public void Argument_WithIntValueNode_CreatesInstanceWithNameAndValueNode()
    {
        // arrange
        var name = "test";
        var value = 42;

        // act
        var argument = new ArgumentAssignment(name, value);

        // assert
        Assert.Equal(name, argument.Name);
        Assert.IsType<IntValueNode>(argument.Value);
        Assert.Equal(value, ((IntValueNode)argument.Value).ToInt32());
    }

    [Fact]
    public void Argument_WithFloatValueNode_CreatesInstanceWithNameAndValueNode()
    {
        // arrange
        var name = "test";
        var value = 3.14;

        // act
        var argument = new ArgumentAssignment(name, value);

        // assert
        Assert.Equal(name, argument.Name);
        Assert.IsType<FloatValueNode>(argument.Value);
        Assert.Equal(value, ((FloatValueNode)argument.Value).ToDouble());
    }

    [Fact]
    public void Argument_WithBooleanValueNode_CreatesInstanceWithNameAndValueNode()
    {
        // arrange
        var name = "test";
        var value = true;

        // act
        var argument = new ArgumentAssignment(name, value);

        // assert
        Assert.Equal(name, argument.Name);
        Assert.IsType<BooleanValueNode>(argument.Value);
        Assert.Equal(value, ((BooleanValueNode)argument.Value).Value);
    }

    [Fact]
    public void Argument_WithIValueNode_CreatesInstanceWithNameAndValueNode()
    {
        // arrange
        var name = "test";
        var value = new StringValueNode("value");

        // act
        var argument = new ArgumentAssignment(name, value);

        // assert
        Assert.Equal(name, argument.Name);
        Assert.Equal(value, argument.Value);
    }
}
