using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language.Utilities
{
    public class SchemaSyntaxPrinterTests
    {
        [Fact]
        public void Serialize_ObjectTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "type Foo { bar: String baz: [Int] }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithIndent_OutHasIndentation()
        {
            // arrange
            string schema = "type Foo { bar: String baz: [Int] }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithArgsNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "type Foo { bar(a: Int = 1 b: Int): String }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithArgsWithIndent_OutHasIndentation()
        {
            // arrange
            string schema = "type Foo { bar(a: Int = 1 b: Int): String }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "type Foo @a(x: \"y\") { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDirectivesWithIndent_OutHasIndentation()
        {
            // arrange
            string schema = "type Foo @a(x: \"y\") { bar: String baz: [Int] } " +
                "type Foo @a @b { bar: String @foo " +
                "baz(a: String = \"abc\"): [Int] @foo @bar }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "\"abc\" type Foo @a { \"abc\" bar: String " +
                "\"abc\" baz: [Int] } " +
                "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
                "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ObjectTypeDefWithDescriptionWithIndent_OutHasIndentation()
        {
            // arrange
            string schema = "\"abc\" type Foo @a { \"abc\" bar: String " +
                "\"abc\" baz: [Int] } " +
                "\"abc\" type Foo @a @b { \"abc\" bar: String @foo " +
                "\"abc\" baz(\"abc\" a: String = \"abc\"): [Int] @foo @bar }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_ObjectTypeImplementsXYZ_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "type Foo implements X & Y & Z " +
                "{ bar: String baz: [Int] }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ObjectTypeImplementsXYZWithIndent_OutHasIndentation()
        {
            // arrange
            string schema = "type Foo implements X & Y & Z " +
                "{ bar: String baz: [Int] }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "union A = B | C";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_UnionTypeDefNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "union A = B | C";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "union A @a = B | C union A @a @b = B | C";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_UnionTypeWithDirectiveDefNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "union A @a = B | C union A @a @b = B | C";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "\"abc\" union A = B | C";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_UnionTypeDefWithDescriptionNoIndented_OutHasIndentation()
        {
            // arrange
            string schema = "\"abc\"union A = B | C";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_EnumTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "enum A { B C }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_EnumTypeDefNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "enum A { B C }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_EnumTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "enum A @a @b(c: 1) { B @a @b(c: 1) C @a @b(c: 1) }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_EnumTypeWithDirectiveDefNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "enum A @a @b(c: 1) { B @a @b(c: 1) C @a @b(c: 1) }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_EnumTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "\"abc\" enum A { \"def\" B \"ghi\" C }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_EnumTypeDefWithDescriptionNoIndented_OutHasIndentation()
        {
            // arrange
            string schema = "\"abc\" enum A { \"def\" B \"ghi\" C }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_InputObjectTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "input A { b: String c: [String!]! d: Int = 1 }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_InputObjectTypeDefNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "input A { b: String c: [String!]! d: Int = 1 }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_InputObjectTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "input A @a @b(c: 1) { b: String @a @b(c: 1) " +
                "c: [String!]! @a @b(c: 1) d: Int = 1 @a @b(c: 1) }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_InputObjectTypeWithDirectiveDefNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "input A @a @b(c: 1) { b: String @a @b(c: 1) " +
                "c: [String!]! @a @b(c: 1) d: Int = 1 @a @b(c: 1) }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_InputObjectTypeDefWithDescriptionNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "\"abc\" input A { \"abc\" b: String }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_InputObjectTypeDefWithDescriptionNoIndentt_OutHasIndentation()
        {
            // arrange
            string schema = "\"abc\" input A { \"abc\" b: String }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_ScalarTypeDefNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "scalar A";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ScalarTypeDefWithDirectiveNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "scalar A @a @b(c: 1)";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ScalarTypeDefWithDescNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "\"abc\" scalar A @a @b(c: 1)";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_ScalarTypeDefWithDescIndent_OutHasIndentation()
        {
            // arrange
            string schema = "\"abc\" scalar A @a @b(c: 1)";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString();

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_SchemaDefWithOpNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "schema { query: A }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_SchemaDefWithOpNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "schema { query: A }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public void Serialize_SchemaDefWithOpAndDirecNoIndent_InOutShouldBeTheSame()
        {
            // arrange
            string schema = "schema @a @b(c: 1) { query: A }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            Assert.Equal(schema, result);
        }

        [Fact]
        public void Serialize_SchemaDefWithOpAndDirecNoIndent_OutHasIndentation()
        {
            // arrange
            string schema = "schema @a @b(c: 1) { query: A }";
            DocumentNode document = Utf8GraphQLParser.Parse(schema);

            // act
            string result = document.ToString(false);

            // assert
            result.MatchSnapshot();
        }
    }
}
