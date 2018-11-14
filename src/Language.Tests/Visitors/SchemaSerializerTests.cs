
using System;
using System.IO;
using System.Text;
using ChilliCream.Testing;
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

            var serializer = new SchemaSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "type Foo { bar: String baz: [Int] }";

            var serializer = new SchemaSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().Snapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo @a { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";

            var serializer = new SchemaSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "type Foo @a { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";

            var serializer = new SchemaSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().Snapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" type Foo @a { \"abc\" bar: String " +
                "\"abc\" baz: [Int] } " +
                "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
                "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";

            var serializer = new SchemaSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDescriptionWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "\"abc\" type Foo @a { \"abc\" bar: String " +
                "\"abc\" baz: [Int] } " +
                "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
                "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";

            var serializer = new SchemaSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().Snapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "union A = B | C";

            var serializer = new SchemaSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_UnionTypeDefNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "union A = B | C";

            var serializer = new SchemaSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().Snapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "union A @a = B | C union A @a @b = B | C";

            var serializer = new SchemaSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_UnionTypeWithDirectiveDefNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "union A @a = B | C union A @a @b = B | C";

            var serializer = new SchemaSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().Snapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" union A = B | C";

            var serializer = new SchemaSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDescriptionNoIndentt_OutHasIndentation()
        {
            // arrange
            string query = "\"abc\"union A = B | C";

            var serializer = new SchemaSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Parser.Default.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().Snapshot();
        }
    }
}
