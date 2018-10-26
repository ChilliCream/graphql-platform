
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
            Assert.Equal(Snapshot.Current(), Snapshot.New(serializer.Value));
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo @a { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";
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
            string query = "type Foo @a { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            SchemaSerializer serializer = new SchemaSerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(serializer.Value));
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" type Foo @a { \"abc\" bar: String " +
                "\"abc\" baz: [Int] } " +
                "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
                "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
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
        public void Serialize_ObjectTypeDefWithDescriptionWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "\"abc\" type Foo @a { \"abc\" bar: String " +
                "\"abc\" baz: [Int] } " +
                "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
                "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
            DocumentNode queryDocument = Parser.Default.Parse(query);
            SchemaSerializer serializer = new SchemaSerializer(true);

            // act
            serializer.Visit(queryDocument);

            // assert
            Assert.Equal(Snapshot.Current(), Snapshot.New(serializer.Value));
        }
    }
}
