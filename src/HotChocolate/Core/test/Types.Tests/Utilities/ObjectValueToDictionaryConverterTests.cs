using HotChocolate.Language;

namespace HotChocolate.Utilities;

public class ObjectValueToDictionaryConverterTests
{
    [Fact]
    public void Convert_ObjectGraphWithScalars_DictionaryWithClrRepres()
    {
        // arrange
        var obj = new ObjectValueNode(
            new ObjectFieldNode("a", new StringValueNode("abc")),
            new ObjectFieldNode("b", new IntValueNode(123)),
            new ObjectFieldNode("c", new FloatValueNode(1.5d)),
            new ObjectFieldNode("d", new BooleanValueNode(true)),
            new ObjectFieldNode("e", new EnumValueNode("DEF")),
            new ObjectFieldNode("f", NullValueNode.Default));

        // act
        var converter = new ObjectValueToDictionaryConverter();
        var dict = converter.Convert(obj);

        // assert
        dict.MatchSnapshot();
    }

    [Fact]
    public void Convert_ObjectGraphOfObjects_DictionaryWithClrRepres()
    {
        // arrange
        var child = new ObjectValueNode(
            new ObjectFieldNode("a", new StringValueNode("abc")),
            new ObjectFieldNode("b", new IntValueNode(123)),
            new ObjectFieldNode("c", new FloatValueNode(1.5d)),
            new ObjectFieldNode("d", new BooleanValueNode(true)),
            new ObjectFieldNode("e", new EnumValueNode("DEF")),
            new ObjectFieldNode("f", NullValueNode.Default));

        var obj = new ObjectValueNode(
            new ObjectFieldNode("a", child));

        // act
        var converter = new ObjectValueToDictionaryConverter();
        var dict = converter.Convert(obj);

        // assert
        dict.MatchSnapshot();
    }

    [Fact]
    public void Convert_ObjectGraphWithList_DictionaryWithClrRepres()
    {
        // arrange
        var child = new ObjectValueNode(
            new ObjectFieldNode("a", new StringValueNode("abc")),
            new ObjectFieldNode("b", new IntValueNode(123)),
            new ObjectFieldNode("c", new FloatValueNode(1.5d)),
            new ObjectFieldNode("d", new BooleanValueNode(true)),
            new ObjectFieldNode("e", new EnumValueNode("DEF")),
            new ObjectFieldNode("f", NullValueNode.Default));

        var obj = new ObjectValueNode(
            new ObjectFieldNode("a",
                new ListValueNode([child, child,])),
            new ObjectFieldNode("b",
                new ListValueNode(
                [
                    new StringValueNode("a"),
                    new StringValueNode("b"),
                ])));

        // act
        var converter = new ObjectValueToDictionaryConverter();
        var dict = converter.Convert(obj);

        // assert
        dict.MatchSnapshot();
    }
}
