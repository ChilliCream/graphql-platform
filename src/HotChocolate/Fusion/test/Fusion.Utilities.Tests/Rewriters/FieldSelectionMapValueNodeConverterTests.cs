using System.Collections.Immutable;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Rewriters;
using HC = HotChocolate.Language;

namespace HotChocolate.Fusion.Rewriters;

public sealed class FieldSelectionMapValueNodeConverterTests
{
    [Fact]
    public void Convert_StringValue_ProducesHotChocolateStringValue()
    {
        // arrange
        var node = new StringValueNode("abc");

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var stringValue = Assert.IsType<HC.StringValueNode>(result);
        Assert.Equal("abc", stringValue.Value);
    }

    [Fact]
    public void Convert_IntValue_ProducesHotChocolateIntValue()
    {
        // arrange
        var node = new IntValueNode("42");

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var intValue = Assert.IsType<HC.IntValueNode>(result);
        Assert.Equal("42", intValue.Value);
    }

    [Fact]
    public void Convert_FloatValue_ProducesHotChocolateFloatValue()
    {
        // arrange
        var node = new FloatValueNode("1.5");

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var floatValue = Assert.IsType<HC.FloatValueNode>(result);
        Assert.Equal("1.5", floatValue.Value);
    }

    [Fact]
    public void Convert_BlockStringValue_PreservesBlockFlag()
    {
        // arrange
        var node = new StringValueNode("multi\nline", true);

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var stringValue = Assert.IsType<HC.StringValueNode>(result);
        Assert.True(stringValue.Block);
        Assert.Equal("multi\nline", stringValue.Value);
    }

    [Fact]
    public void Convert_BooleanValue_ProducesHotChocolateBooleanValue()
    {
        // arrange
        var node = new BooleanValueNode(true);

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var booleanValue = Assert.IsType<HC.BooleanValueNode>(result);
        Assert.True(booleanValue.Value);
    }

    [Fact]
    public void Convert_EnumValue_ProducesHotChocolateEnumValue()
    {
        // arrange
        var node = new EnumValueNode("IMPERIAL");

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var enumValue = Assert.IsType<HC.EnumValueNode>(result);
        Assert.Equal("IMPERIAL", enumValue.Value);
    }

    [Fact]
    public void Convert_NullValue_ProducesHotChocolateNullValue()
    {
        // arrange
        var node = new NullValueNode();

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        Assert.IsType<HC.NullValueNode>(result);
    }

    [Fact]
    public void Convert_ListValue_ProducesHotChocolateListValue()
    {
        // arrange
        var node = new ListValueNode([new IntValueNode("1"), new IntValueNode("2")]);

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var listValue = Assert.IsType<HC.ListValueNode>(result);
        Assert.Equal(2, listValue.Items.Count);
    }

    [Fact]
    public void Convert_ObjectValue_ProducesHotChocolateObjectValue()
    {
        // arrange
        var node = new ObjectValueNode(
            [new ObjectFieldNode(new NameNode("unit"), new EnumValueNode("IMPERIAL"))]);

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        var objectValue = Assert.IsType<HC.ObjectValueNode>(result);
        var field = Assert.Single(objectValue.Fields);
        Assert.Equal("unit", field.Name.Value);
    }

    [Fact]
    public void Convert_Argument_ProducesHotChocolateArgument()
    {
        // arrange
        var node = new ArgumentNode(new NameNode("unit"), new EnumValueNode("IMPERIAL"));

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(node);

        // assert
        Assert.Equal("unit", result.Name.Value);
        Assert.IsType<HC.EnumValueNode>(result.Value);
    }

    [Fact]
    public void Convert_ArgumentArray_ProducesHotChocolateArgumentList()
    {
        // arrange
        var arguments = ImmutableArray.Create(
            new ArgumentNode(new NameNode("a"), new IntValueNode("1")),
            new ArgumentNode(new NameNode("b"), new EnumValueNode("X")));

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(arguments);

        // assert
        Assert.Equal(2, result.Count);
        Assert.Equal("a", result[0].Name.Value);
    }

    [Fact]
    public void Convert_EmptyArgumentArray_ProducesEmptyList()
    {
        // arrange
        var arguments = ImmutableArray<ArgumentNode>.Empty;

        // act
        var result = FieldSelectionMapValueNodeConverter.Convert(arguments);

        // assert
        Assert.Empty(result);
    }
}
