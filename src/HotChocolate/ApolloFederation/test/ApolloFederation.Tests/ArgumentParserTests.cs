using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;
using HotChocolate.ApolloFederation.Resolvers;

namespace HotChocolate.ApolloFederation.Tests.Resolvers
{
    public class ArgumentParserTests
    {
        private static ObjectType CreateTestObjectType()
        {
            return new ObjectType(d =>
            {
                d.Name("Test");
                d.Field("foo").Type<StringType>();
                d.Field("bar").Type<IntType>();
                d.Field("nested").Type(new ObjectType(nd =>
                {
                    nd.Name("Nested");
                    nd.Field("baz").Type<StringType>();
                }));
                d.Field("items").Type(new ListType(new ObjectType(ld =>
                {
                    ld.Name("Item");
                    ld.Field("name").Type<StringType>();
                })));
            });
        }

        [Fact]
        public void GetValue_SimpleField_ReturnsValue()
        {
            var type = CreateTestObjectType();
            var valueNode = new ObjectValueNode(
                new ObjectFieldNode("foo", new StringValueNode("abc")),
                new ObjectFieldNode("bar", new IntValueNode(123))
            );
            var result = ArgumentParser.GetValue<string>(valueNode, type, new[] { "foo" });
            Assert.Equal("abc", result);
        }

        [Fact]
        public void GetValue_NestedField_ReturnsValue()
        {
            var type = CreateTestObjectType();
            var valueNode = new ObjectValueNode(
                new ObjectFieldNode("nested", new ObjectValueNode(
                    new ObjectFieldNode("baz", new StringValueNode("deep"))
                ))
            );
            var result = ArgumentParser.GetValue<string>(valueNode, type, new[] { "nested", "baz" });
            Assert.Equal("deep", result);
        }

        [Fact]
        public void GetValue_ListElementField_ReturnsValue()
        {
            var type = CreateTestObjectType();
            var valueNode = new ObjectValueNode(
                new ObjectFieldNode("items", new ListValueNode(
                    new ObjectValueNode(new ObjectFieldNode("name", new StringValueNode("first"))),
                    new ObjectValueNode(new ObjectFieldNode("name", new StringValueNode("second")))
                ))
            );
            var result = ArgumentParser.GetValue<string>(valueNode, type, new[] { "items", "1", "name" });
            Assert.Equal("second", result);
        }

        [Fact]
        public void GetValue_ScalarInt_ReturnsValue()
        {
            var type = CreateTestObjectType();
            var valueNode = new ObjectValueNode(
                new ObjectFieldNode("bar", new IntValueNode(42))
            );
            var result = ArgumentParser.GetValue<int>(valueNode, type, new[] { "bar" });
            Assert.Equal(42, result);
        }

        [Fact]
        public void GetValue_EnumValue_ReturnsValue()
        {
            var enumType = new EnumType(d =>
            {
                d.Name("Color");
                d.Value("RED");
                d.Value("GREEN");
            });
            var valueNode = new EnumValueNode("GREEN");
            var result = ArgumentParser.GetValue<string>(valueNode, enumType, new string[0]);
            Assert.Equal("GREEN", result.ToString());
        }
    }
}
