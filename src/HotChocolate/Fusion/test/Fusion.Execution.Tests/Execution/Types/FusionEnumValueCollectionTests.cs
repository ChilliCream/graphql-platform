using System.Text;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Execution.Types;

public class FusionEnumValueCollectionTests
{
    [Fact]
    public void ContainsName_Should_Return_True_When_Small_Enum_Contains_Accessible_Value()
    {
        // arrange
        var collection = CreateCollection(
            Value("RED"),
            Value("GREEN"),
            Value("BLUE"));

        // act
        var result = collection.ContainsName("GREEN"u8);

        // assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsName_Should_Return_False_When_Small_Enum_Does_Not_Contain_Value()
    {
        // arrange
        var collection = CreateCollection(
            Value("RED"),
            Value("GREEN"),
            Value("BLUE"));

        // act
        var result = collection.ContainsName("YELLOW"u8);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsName_Should_Return_True_When_Large_Enum_Contains_Shortest_Value()
    {
        // arrange
        var collection = CreateCollection(CreateVaryingLengthValues(20));

        // act
        var result = collection.ContainsName(Encoding.UTF8.GetBytes(new string('E', 1)));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsName_Should_Return_True_When_Large_Enum_Contains_Longest_Value()
    {
        // arrange
        var collection = CreateCollection(CreateVaryingLengthValues(20));

        // act
        var result = collection.ContainsName(Encoding.UTF8.GetBytes(new string('E', 20)));

        // assert
        Assert.True(result);
    }

    [Fact]
    public void ContainsName_Should_Return_False_When_Large_Enum_Has_EqualLength_Near_Neighbor()
    {
        // arrange
        var collection = CreateCollection(CreateVaryingLengthValues(20));

        // act
        // same length as "EEEEE" (5) but differs in the final byte.
        var result = collection.ContainsName("EEEEX"u8);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsName_Should_Return_False_When_Value_Is_Inaccessible()
    {
        // arrange
        var collection = CreateCollection(
            Value("VISIBLE"),
            Value("HIDDEN", isInaccessible: true));

        // act
        var result = collection.ContainsName("HIDDEN"u8);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsName_Should_Return_False_When_Span_Is_Empty()
    {
        // arrange
        var collection = CreateCollection(
            Value("RED"),
            Value("GREEN"),
            Value("BLUE"));

        // act
        var result = collection.ContainsName(ReadOnlySpan<byte>.Empty);

        // assert
        Assert.False(result);
    }

    [Fact]
    public void ContainsName_Should_Return_False_When_Payload_Contains_Json_Escape_Sequence()
    {
        // arrange
        var collection = CreateCollection(Value("FOO"));

        // act
        // a raw JSON payload for "FOO" written as "FOO" never matches a
        // [A-Za-z0-9_] enum name, so masking is the correct outcome.
        var result = collection.ContainsName("F\\u004fO"u8);

        // assert
        Assert.False(result);
    }

    private static FusionEnumValueCollection CreateCollection(params FusionEnumValue[] values)
        => new(values);

    private static FusionEnumValue[] CreateVaryingLengthValues(int count)
    {
        var values = new FusionEnumValue[count];

        for (var i = 0; i < count; i++)
        {
            values[i] = Value(new string('E', i + 1));
        }

        return values;
    }

    private static FusionEnumValue Value(string name, bool isInaccessible = false)
        => new(name, description: null, isDeprecated: false, deprecationReason: null, isInaccessible);
}
