using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ObjectValueToDictionaryConverterTests
    {
        [Fact]
        public void Convert_ObjectGraphWithScalars_DictionaryWithClrRepres()
        {
            // arrange
            var obj = new ObjectValueNode(
                new ObjectFieldNode("a", new StringValueNode("abc")),
                new ObjectFieldNode("b", new IntValueNode("123")),
                new ObjectFieldNode("c", new FloatValueNode("1.5")),
                new ObjectFieldNode("d", new BooleanValueNode(true)),
                new ObjectFieldNode("e", new EnumValueNode("DEF")),
                new ObjectFieldNode("f", NullValueNode.Default));

            // act
            var converter = new ObjectValueToDictionaryConverter();
            Dictionary<string, object> dict = converter.Convert(obj);

            // assert
            dict.Snapshot();
        }

        [Fact]
        public void Convert_ObjectGraphOfObjects_DictionaryWithClrRepres()
        {
            // arrange
            var child = new ObjectValueNode(
                new ObjectFieldNode("a", new StringValueNode("abc")),
                new ObjectFieldNode("b", new IntValueNode("123")),
                new ObjectFieldNode("c", new FloatValueNode("1.5")),
                new ObjectFieldNode("d", new BooleanValueNode(true)),
                new ObjectFieldNode("e", new EnumValueNode("DEF")),
                new ObjectFieldNode("f", NullValueNode.Default));

            var obj = new ObjectValueNode(
                new ObjectFieldNode("a", child));

            // act
            var converter = new ObjectValueToDictionaryConverter();
            Dictionary<string, object> dict = converter.Convert(obj);

            // assert
            dict.Snapshot();
        }

        [Fact]
        public void Convert_ObjectGraphWithList_DictionaryWithClrRepres()
        {
            // arrange
            var child = new ObjectValueNode(
                new ObjectFieldNode("a", new StringValueNode("abc")),
                new ObjectFieldNode("b", new IntValueNode("123")),
                new ObjectFieldNode("c", new FloatValueNode("1.5")),
                new ObjectFieldNode("d", new BooleanValueNode(true)),
                new ObjectFieldNode("e", new EnumValueNode("DEF")),
                new ObjectFieldNode("f", NullValueNode.Default));

            var obj = new ObjectValueNode(
                new ObjectFieldNode("a",
                    new ListValueNode(new IValueNode[] { child, child })),
                new ObjectFieldNode("b",
                    new ListValueNode(new IValueNode[] {
                        new StringValueNode("a"),
                        new StringValueNode("b") })));

            // act
            var converter = new ObjectValueToDictionaryConverter();
            Dictionary<string, object> dict = converter.Convert(obj);

            // assert
            dict.Snapshot();
        }
    }
}
