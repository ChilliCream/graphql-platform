using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Utilities
{
    public class ObjectValueToInputObjectConverterTests
    {
        [Fact]
        public void Convert_ObjectValue_FooInputObject()
        {
            // arrange
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType<InputObjectType<Foo>>();
            });

            var type = schema.GetType<InputObjectType>("FooInput");

            var baz = new ObjectValueNode(
                new ObjectFieldNode("number", new StringValueNode("1.5")));

            var bar = new ObjectValueNode(
                new ObjectFieldNode("state", new EnumValueNode("ON")),
                new ObjectFieldNode("bazs", new ListValueNode(baz)),
                new ObjectFieldNode("stringArray", new ListValueNode(
                    new IValueNode[]  {
                        new StringValueNode("abc"),
                        new StringValueNode("def")
                    })));

            var foo = new ObjectValueNode(
                new ObjectFieldNode("text", new StringValueNode("abc")),
                new ObjectFieldNode("bar", bar));

            // assert
            var converter = new ObjectValueToInputObjectConverter(
                TypeConversion.Default);
            object converted = converter.Convert(foo, type);

            // assert
            converted.Snapshot();
        }

        [Fact]
        public void Convert_ObjectValue_InputObjectWithoutClrType()
        {
            // arrange
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterType(new InputObjectType(t =>
                    t.Name("FooInput")
                        .Field("a")
                        .Type<ListType<StringType>>()));
            });

            var type = schema.GetType<InputObjectType>("FooInput");

            var foo = new ObjectValueNode(
                new ObjectFieldNode("a", new ListValueNode(
                    new IValueNode[]  {
                        new StringValueNode("abc"),
                        new StringValueNode("def")
                    })));

            // assert
            var converter = new ObjectValueToInputObjectConverter(
                TypeConversion.Default);
            object converted = converter.Convert(foo, type);

            // assert
            converted.Snapshot();
        }

        public class Foo
        {
            public string Text { get; set; }
            public Bar Bar { get; set; }
        }

        public class Bar
        {
            public State State { get; set; }
            public IReadOnlyCollection<Baz> Bazs { get; set; }
            public string[] StringArray { get; set; }
        }

        public class Baz
        {
            public decimal Number { get; set; }
        }

        public enum State
        {
            On,
            Off
        }
    }
}
