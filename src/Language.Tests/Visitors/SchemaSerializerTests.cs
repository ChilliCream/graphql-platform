
using System;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaSerializerTests
    {
        [Fact]
        public void Serialize_ObjectTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo { bar: String baz: [Int] }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            SchemaSerializer serializer = new SchemaSerializer();

            // act
            serializer.Visit(queryDocument);

            // assert
            Assert.Equal(
                query,
                serializer.Value);
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "type Foo { bar: String baz: [Int] }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            SchemaSerializer serializer = new SchemaSerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            Assert.Equal(
                "type Foo {\n  bar: String\n  baz: [Int]\n}"
                    .Replace("\n", Environment.NewLine),
                serializer.Value);
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo { bar: String baz: [Int] }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            SchemaSerializer serializer = new SchemaSerializer();

            // act
            serializer.Visit(queryDocument);

            // assert
            Assert.Equal(
                query,
                serializer.Value);
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "type Foo @a { bar: String baz: [Int] } type Foo @a @b(a: \"abc\") { bar: String baz: [Int] }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            SchemaSerializer serializer = new SchemaSerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            Assert.Equal(
                "type Foo {\n  bar: String\n  baz: [Int]\n}"
                    .Replace("\n", Environment.NewLine),
                serializer.Value);
        }
    }
}
