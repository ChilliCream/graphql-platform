using HotChocolate.Language;

namespace HotChocolate.Types.Mutable;

public class ArgumentTests
{
    [Fact]
    public void Argument_WithStringValueNode_CreatesInstanceWithNameAndValueNode()
    {
        // arrange
        const string name = "test";
        const string value = "value";

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
        const string name = "test";
        const int value = 42;

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
        const string name = "test";
        const double value = 3.14;

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
        const string name = "test";
        const bool value = true;

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
        const string name = "test";
        var value = new StringValueNode("value");

        // act
        var argument = new ArgumentAssignment(name, value);

        // assert
        Assert.Equal(name, argument.Name);
        Assert.Equal(value, argument.Value);
    }
}
