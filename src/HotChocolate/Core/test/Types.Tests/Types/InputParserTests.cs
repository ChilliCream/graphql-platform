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
    }
}
