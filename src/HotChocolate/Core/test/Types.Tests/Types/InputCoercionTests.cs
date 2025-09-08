using HotChocolate.Language;
using HotChocolate.Utilities;

namespace HotChocolate.Types;

public class InputCoercionTests
{
    /// <summary>
    /// Converts according to input coercion rules.
    /// </summary>
    [Fact]
    public void ConvertAccordingToInputCoercionRules()
    {
        InputIsCoercedCorrectly<BooleanType, BooleanValueNode, bool>(
            new BooleanValueNode(true), true);
        InputIsCoercedCorrectly<BooleanType, BooleanValueNode, bool>(
            new BooleanValueNode(false), false);
        InputIsCoercedCorrectly<IntType, IntValueNode, int>(
            new IntValueNode(123), 123);
        InputIsCoercedCorrectly<FloatType, IntValueNode, double>(
            new IntValueNode(123), 123d);
        InputIsCoercedCorrectly<FloatType, FloatValueNode, double>(
            new FloatValueNode(123.456d), 123.456d);
        InputIsCoercedCorrectly<StringType, StringValueNode, string>(
            new StringValueNode("abc123"), "abc123");
        InputIsCoercedCorrectly<IdType, StringValueNode, string>(
            new StringValueNode("123456"), "123456");
    }

    /// <summary>
    /// Does not convert when input coercion rules reject a value.
    /// </summary>
    [Fact]
    public void ConvertAccordingToInputCoercionRules2()
    {
        InputCannotBeCoercedCorrectly<BooleanType, IntValueNode>(
            new IntValueNode(123));
        InputCannotBeCoercedCorrectly<IntType, FloatValueNode>(
            new FloatValueNode(123.123d));
        InputCannotBeCoercedCorrectly<IntType, BooleanValueNode>(
            new BooleanValueNode(true));
        InputCannotBeCoercedCorrectly<IntType, StringValueNode>(
            new StringValueNode("123.123"));
        InputCannotBeCoercedCorrectly<FloatType, StringValueNode>(
            new StringValueNode("123"));
        InputCannotBeCoercedCorrectly<StringType, FloatValueNode>(
            new FloatValueNode(123.456d));
        InputCannotBeCoercedCorrectly<StringType, BooleanValueNode>(
            new BooleanValueNode(false));
        InputIsCoercedCorrectly<IdType, StringValueNode, string>(
            new StringValueNode("123456"), "123456");
    }

    [Fact]
    public void ListCanBeCoercedFromListValue()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new BooleanType());
        var list = new ListValueNode(new BooleanValueNode(true), new BooleanValueNode(false));

        // act
        var coercedValue =
            inputParser.ParseLiteral(list, type, Path.Root.Append("root"));

        // assert
        Assert.Collection(Assert.IsType<List<bool?>>(coercedValue), Assert.True, Assert.False);
    }

    /// <summary>
    /// Expected Type:  [[Boolean]]
    /// Provided Value: [[true], [true, false]]
    /// Coerced Value:  [[true], [true, false]]
    /// </summary>
    [Fact]
    public void Matrix_Can_Be_Coerced_From_Matrix()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new ListType(new BooleanType()));
        var value = new ListValueNode(
            new ListValueNode(new BooleanValueNode(true)),
            new ListValueNode(new BooleanValueNode(true), new BooleanValueNode(false)));

        // act
        var coercedValue =
            inputParser.ParseLiteral(value, type, Path.Root.Append("root"));

        // assert
        coercedValue.MatchSnapshot();
    }

    /// <summary>
    /// Expected Type:  [[Boolean]]
    /// Provided Value: true
    /// Coerced Value:  [[true]]
    /// </summary>
    [Fact]
    public void Matrix_Can_Be_Coerced_From_Single_Value()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new ListType(new BooleanType()));
        var value = new BooleanValueNode(true);

        // act
        var coercedValue =
            inputParser.ParseLiteral(value, type, Path.Root.Append("root"));

        // assert
        coercedValue.MatchSnapshot();
    }

    /// <summary>
    /// Expected Type:  [[Boolean]]
    /// Provided Value: null
    /// Coerced Value:  null
    /// </summary>
    [Fact]
    public void Matrix_Can_Be_Coerced_From_Null()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new ListType(new BooleanType()));
        var value = NullValueNode.Default;

        // act
        var coercedValue =
            inputParser.ParseLiteral(value, type, Path.Root.Append("root"));

        // assert
        Assert.Null(coercedValue);
    }

    /// <summary>
    /// Expected Type:  [[Boolean]]
    /// Provided Value: [true]
    /// Coerced Value:  Error: Incorrect item value
    /// </summary>
    [Fact]
    public void Matrix_Cannot_Be_Coerced_From_List()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new ListType(new BooleanType()));
        var value = new ListValueNode(new BooleanValueNode(true));

        // act
        void Action() =>
            inputParser.ParseLiteral(value, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void ListCanBeCoercedFromListElementValue()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new BooleanType());
        var element = new BooleanValueNode(true);

        // act
        var coercedValue =
            inputParser.ParseLiteral(element, type, Path.Root.Append("root"));

        // assert
        Assert.Collection(Assert.IsType<List<bool?>>(coercedValue), Assert.True);
    }

    [Fact]
    public void ListCannotBeCoercedFromMixedList()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new BooleanType());
        var list = new ListValueNode(new BooleanValueNode(true), new StringValueNode("foo"));

        // act
        void Action() =>
            inputParser.ParseLiteral(list, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    [Fact]
    public void ListCannotBeCoercedIfElementTypeDoesNotMatch()
    {
        // arrange
        var inputParser = new InputParser(new DefaultTypeConverter());
        var type = (IInputType)new ListType(new BooleanType());
        var element = new StringValueNode("foo");

        // act
        void Action() =>
            inputParser.ParseLiteral(element, type, Path.Root.Append("root"));

        // assert
        Assert.Throws<SerializationException>(Action);
    }

    private void InputIsCoercedCorrectly<TType, TLiteral, TExpected>(
        TLiteral literal, TExpected expectedValue)
        where TType : ScalarType, new()
        where TLiteral : IValueNode
    {
        // arrange
        var type = new TType();

        // act
        var coercedValue = type.ParseLiteral(literal);

        // assert
        Assert.IsType<TExpected>(coercedValue);
        Assert.Equal(expectedValue, coercedValue);
    }

    private void InputCannotBeCoercedCorrectly<TType, TLiteral>(
        TLiteral literal)
        where TType : ScalarType, new()
        where TLiteral : IValueNode
    {
        // arrange
        var type = new TType();

        // act
        void Action() => type.ParseLiteral(literal);

        // assert
        Assert.Throws<SerializationException>(Action);
    }
}
