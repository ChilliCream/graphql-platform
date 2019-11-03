using System.IO;
using System.Text;
using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaSyntaxSerializerTests
    {
        [Fact]
        public void Serialize_ObjectTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo { bar: String baz: [Int] }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithArgsNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo { bar(a: Int = 1 b: Int): String }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithArgsWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "type Foo { bar(a: Int = 1 b: Int): String }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo @a(x: \"y\") { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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
            string query = "type Foo @a(x: \"y\") { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" type Foo @a { \"abc\" bar: String " +
                "\"abc\" baz: [Int] } " +
                "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
                "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeImplementsXYZ_InOutShouldBeTheSame()
        {
            // arrange
            string query = "type Foo implements X & Y & Z " +
                "{ bar: String baz: [Int] }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ObjectTypeImplementsXYZWithIndent_OutHasIndentation()
        {
            // arrange
            string query = "type Foo implements X & Y & Z " +
                "{ bar: String baz: [Int] }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "union A = B | C";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "union A @a = B | C union A @a @b = B | C";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" union A = B | C";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

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

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_EnumTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "enum A { B C }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_EnumTypeDefNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "enum A { B C }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_EnumTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "enum A @a @b(c: 1) { B @a @b(c: 1) C @a @b(c: 1) }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_EnumTypeWithDirectiveDefNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "enum A @a @b(c: 1) { B @a @b(c: 1) C @a @b(c: 1) }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_EnumTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" enum A { \"def\" B \"ghi\" C }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_EnumTypeDefWithDescriptionNoIndentt_OutHasIndentation()
        {
            // arrange
            string query = "\"abc\" enum A { \"def\" B \"ghi\" C }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_InputObjectTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "input A { b: String c: [String!]! d: Int = 1 }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_InputObjectTypeDefNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "input A { b: String c: [String!]! d: Int = 1 }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_InputObjectTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "input A @a @b(c: 1) { b: String @a @b(c: 1) " +
                "c: [String!]! @a @b(c: 1) d: Int = 1 @a @b(c: 1) }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_InputObjectTypeWithDirectiveDefNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "input A @a @b(c: 1) { b: String @a @b(c: 1) " +
                "c: [String!]! @a @b(c: 1) d: Int = 1 @a @b(c: 1) }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_InputObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" input A { \"abc\" b: String }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_InputObjectTypeDefWithDescriptionNoIndentt_OutHasIndentation()
        {
            // arrange
            string query = "\"abc\" input A { \"abc\" b: String }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_ScalarTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "scalar A";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ScalarTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "scalar A @a @b(c: 1)";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ScalarTypeDefWithDescNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "\"abc\" scalar A @a @b(c: 1)";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_ScalarTypeDefWithDescIndent_OutHasIndentation()
        {
            // arrange
            string query = "\"abc\" scalar A @a @b(c: 1)";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_SchemaDefWithOpNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "schema { query: A }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_SchemaDefWithOpNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "schema { query: A }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }

        [Fact]
        public void Serialize_SchemaDefWithOpAndDirecNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string query = "schema @a @b(c: 1) { query: A }";

            var serializer = new SchemaSyntaxSerializer();
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            Assert.Equal(
                query,
                content.ToString());
        }

        [Fact]
        public void Serialize_SchemaDefWithOpAndDirecNoIndent_OutHasIndentation()
        {
            // arrange
            string query = "schema @a @b(c: 1) { query: A }";

            var serializer = new SchemaSyntaxSerializer(true);
            var content = new StringBuilder();
            var writer = new StringWriter(content);

            DocumentNode queryDocument = Utf8GraphQLParser.Parse(query);

            // act
            serializer.Visit(queryDocument, new DocumentWriter(writer));

            // assert
            content.ToString().MatchSnapshot();
        }
    }
}
