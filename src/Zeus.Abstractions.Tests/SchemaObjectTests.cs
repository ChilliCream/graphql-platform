using System;
using System.Linq;
using Xunit;

namespace Zeus.Abstractions.Tests
{
    public class SchemaObjectTests
    {
        [Fact]
        public void EnumTypeDefinitionTest()
        {
            // act
            EnumTypeDefinition typeDefinition = new EnumTypeDefinition("Foo", new[] { "A", "B" });

            // assert
            Assert.Equal("Foo", typeDefinition.Name);
            Assert.Equal("A", typeDefinition.Values.First());
            Assert.Equal("B", typeDefinition.Values.Last());
            Assert.Equal(2, typeDefinition.Values.Count);

            string expectedStringRepresentation = $"enum Foo{Environment.NewLine}{{{Environment.NewLine}  A{Environment.NewLine}  B{Environment.NewLine}}}";
            Assert.Equal(expectedStringRepresentation, typeDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithoutArgument()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"), null);

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Empty(fieldDefinition.Arguments);

            string expectedStringRepresentation = $"foo: String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithOneArgument()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"),
                new[] { new InputValueDefinition("a", new NamedType("Int"), null) });

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Equal(1, fieldDefinition.Arguments.Count);

            string expectedStringRepresentation = $"foo(a: Int): String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithTwoArguments()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"),
                new[] {
                    new InputValueDefinition("a", new NamedType("Int"), null),
                    new InputValueDefinition("b", new NamedType("Boolean"), null) });

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Equal(2, fieldDefinition.Arguments.Count);

            string expectedStringRepresentation = $"foo(a: Int, b: Boolean): String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

        [Fact]
        public void FieldDefinitionWithTwoArgumentsAndDefaultValue()
        {
            // act
            FieldDefinition fieldDefinition = new FieldDefinition(
                "foo", new NamedType("String"),
                new[] {
                    new InputValueDefinition("a", new NamedType("Int"), "1"),
                    new InputValueDefinition("b", new NamedType("Boolean"), null) });

            // assert
            Assert.Equal("foo", fieldDefinition.Name);
            Assert.Equal(new NamedType("String"), fieldDefinition.Type);
            Assert.Equal(2, fieldDefinition.Arguments.Count);

            string expectedStringRepresentation = $"foo(a: Int = 1, b: Boolean): String";
            Assert.Equal(expectedStringRepresentation, fieldDefinition.ToString());
        }

    }
}
