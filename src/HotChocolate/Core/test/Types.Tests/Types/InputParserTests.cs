using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Utilities;
using HotChocolate.Tests;
using Snapshooter.Xunit;
using Xunit;

#nullable enable

namespace HotChocolate.Types
{
    public class InputParserTests
    {
        [Fact]
        public void Deserialize_InputObject_AllIsSet()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<TestInput>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("TestInput");

            var fieldData = new Dictionary<string, object?>
            {
                { "field1", "abc" },
                { "field2", 123 }
            };

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            var runtimeValue = parser.ParseResult(fieldData, type, Path.Root);

            // assert
            Assert.IsType<TestInput>(runtimeValue).MatchSnapshot();
        }

        [Fact]
        public void Parse_InputObject_AllIsSet()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<TestInput>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("TestInput");

            var fieldData = new ObjectValueNode(
                new ObjectFieldNode("field1", "abc"),
                new ObjectFieldNode("field2", 123));

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            var runtimeValue = parser.ParseLiteral(fieldData, type, Path.Root);

            // assert
            Assert.IsType<TestInput>(runtimeValue).MatchSnapshot();
        }

        [Fact]
        public void Deserialize_InputObject_AllIsSet_ConstructorInit()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Test2Input>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("Test2Input");

            var fieldData = new Dictionary<string, object?>
            {
                { "field1", "abc" },
                { "field2", 123 }
            };

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            var runtimeValue = parser.ParseResult(fieldData, type, Path.Root);

            // assert
            Assert.IsType<Test2Input>(runtimeValue).MatchSnapshot();
        }

        [Fact]
        public void Parse_InputObject_AllIsSet_ConstructorInit()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Test2Input>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("Test2Input");

            var fieldData = new ObjectValueNode(
                new ObjectFieldNode("field1", "abc"),
                new ObjectFieldNode("field2", 123));

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            var runtimeValue = parser.ParseLiteral(fieldData, type, Path.Root);

            // assert
            Assert.IsType<Test2Input>(runtimeValue).MatchSnapshot();
        }

        [Fact]
        public void Deserialize_InputObject_AllIsSet_MissingRequired()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Test2Input>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("Test2Input");

            var fieldData = new Dictionary<string, object?>
            {
                { "field2", 123 }
            };

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            void Action() => parser.ParseResult(fieldData, type, Path.Root);

            // assert
            Assert.Throws<SerializationException>(Action).MatchSnapshot();
        }

        [Fact]
        public void Parse_InputObject_AllIsSet_MissingRequired()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Test2Input>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("Test2Input");

            var fieldData = new ObjectValueNode(
                new ObjectFieldNode("field2", 123));

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            void Action() => parser.ParseLiteral(fieldData, type, Path.Root);

            // assert
            Assert.Throws<SerializationException>(Action).MatchSnapshot();
        }

        [Fact]
        public void Deserialize_InputObject_AllIsSet_OneInvalidField()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<TestInput>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("TestInput");

            var fieldData = new Dictionary<string, object?>
            {
                { "field2", 123 },
                { "field3", 123 }
            };

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            void Action() => parser.ParseResult(fieldData, type, Path.Root.Append("root"));

            // assert
            Assert.Throws<SerializationException>(Action).MatchSnapshot();
        }

        [Fact]
        public void Parse_InputObject_AllIsSet_OneInvalidField()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<TestInput>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("TestInput");

            var fieldData = new ObjectValueNode(
                new ObjectFieldNode("field2", 123),
                new ObjectFieldNode("field3", 123));

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            void Action() => parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

            // assert
            Assert.Throws<SerializationException>(Action).MatchSnapshot();
        }

        [Fact]
        public void Deserialize_InputObject_AllIsSet_TwoInvalidFields()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<TestInput>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("TestInput");

            var fieldData = new Dictionary<string, object?>
            {
                { "field2", 123 },
                { "field3", 123 },
                { "field4", 123 }
            };

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            void Action() => parser.ParseResult(fieldData, type, Path.Root.Append("root"));

            // assert
            Assert.Throws<SerializationException>(Action).MatchSnapshot();
        }

        [Fact]
        public void Parse_InputObject_AllIsSet_TwoInvalidFields()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<TestInput>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("TestInput");

            var fieldData = new ObjectValueNode(
                new ObjectFieldNode("field2", 123),
                new ObjectFieldNode("field3", 123),
                new ObjectFieldNode("field4", 123));

            // act
            var parser = new InputParser(new DefaultTypeConverter());
            void Action() => parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

            // assert
            Assert.Throws<SerializationException>(Action).MatchSnapshot();
        }

        [Fact]
        public void Parse_InputObject_WithDefault_Values()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Test3Input>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            InputObjectType type = schema.GetType<InputObjectType>("Test3Input");

            var fieldData = new ObjectValueNode(
                new ObjectFieldNode("field2", 123));

            // act
            var parser = new InputParser();
            var obj = parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

            // assert
            Assert.Equal("DefaultAbc", Assert.IsType<Test3Input>(obj).Field1);
        }

        [Fact]
        public void Parse_InputObject_NonNullViolation()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<Test3Input>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            var type = new NonNullType(schema.GetType<InputObjectType>("Test3Input"));

            // act
            var parser = new InputParser();
            void Action()
                => parser.ParseLiteral(NullValueNode.Default, type, Path.Root.Append("root"));

            // assert
            Assert.Throws<SerializationException>(Action).MatchSnapshot();
        }

        [Fact]
        public void Parse_InputObject_NullableEnumList()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddInputObjectType<FooInput>()
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            var type = new NonNullType(schema.GetType<InputObjectType>("FooInput"));

            var listData = new ListValueNode(
                NullValueNode.Default,
                new EnumValueNode("BAZ"));

            var fieldData = new ObjectValueNode(
                new ObjectFieldNode("bars", listData));

            // act
            var parser = new InputParser();
            var runtimeData = parser.ParseLiteral(fieldData, type, Path.Root.Append("root"));

            // assert
            Assert.Collection(
                Assert.IsType<FooInput>(runtimeData).Bars,
                t => Assert.Null(t),
                t => Assert.Equal(Bar.Baz, t));
        }

        public class TestInput
        {
            public string? Field1 { get; set; }

            public int? Field2 { get; set; }
        }

        public class Test2Input
        {
            public Test2Input(string field1)
            {
                Field1 = field1;
            }

            public string Field1 { get; }

            public int? Field2 { get; set; }
        }

        public class Test3Input
        {
            public Test3Input(string field1)
            {
                Field1 = field1;
            }

            [DefaultValue("DefaultAbc")]
            public string Field1 { get; }

            public int? Field2 { get; set; }
        }

        public class FooInput
        {
            public List<Bar?> Bars { get; set; } = new();
        }

        public enum Bar
        {
            Baz
        }
    }
}
