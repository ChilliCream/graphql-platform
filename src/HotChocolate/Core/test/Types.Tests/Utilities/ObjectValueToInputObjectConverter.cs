using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
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
                c.RegisterQueryType<DummyQuery>();
                c.RegisterType<InputObjectType<Foo>>();
                c.RegisterType<DecimalType>();
            });

            InputObjectType type = schema.GetType<InputObjectType>("FooInput");

            var baz = new ObjectValueNode(
                new ObjectFieldNode("number", new FloatValueNode(1.5d)));

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
            var converted = converter.Convert(foo, type);

            // assert
            converted.MatchSnapshot();
        }

        [Fact]
        public void Convert_ObjectValue_InputObjectWithoutClrType()
        {
            // arrange
            ISchema schema = Schema.Create(c =>
            {
                c.RegisterQueryType<DummyQuery>();
                c.RegisterType(new InputObjectType(t =>
                    t.Name("FooInput")
                        .Field("a")
                        .Type<ListType<StringType>>()));
            });

            InputObjectType type = schema.GetType<InputObjectType>("FooInput");

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
            converted.MatchSnapshot();
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

        public class DummyQuery
        {
            public string Foo { get; set; }
        }
    }
}
