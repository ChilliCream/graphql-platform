using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaParserTests
    {
         [Fact]
        public void ParserSimpleObjectType()
        {
            // arrange
            string sourceText = "type a @foo(a: \"123\") " +
                "{ b: String @foo(a: \"123\") " +
                "c(d: F = ENUMVALUE @foo(a: \"123\")): Int }";
            var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParserInputObjectType()
        {
            // arrange
            string sourceText = "input a @foo(a: \"123\") " +
                "{ b: String @foo(a: \"123\") c: Int = 123 }";
            var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParserScalarType()
        {
            // arrange
            string sourceText = "scalar FOO @foo(a: \"123\")";
            var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParserSimpleInterfaceType()
        {
            // arrange
            string sourceText = "interface a @foo(a: \"123\") " +
                "{ b: String @foo(a: \"123\") " +
                "c(d: F = ENUMVALUE @foo(a: \"123\")): Int }";
             var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParseEnum()
        {
            // arrange
            string sourceText = "enum Foo @foo(a: \"123\") "
                + "{ BAR @foo(a: 123) , BAZ }";
            var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParseUnion()
        {
            // arrange
            string sourceText = "union Foo @foo(a: \"123\") = "
                + "BAR | BAZ ";
            var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParseUnion_LeadingPipe()
        {
            // arrange
            string sourceText = "union Foo @foo(a: \"123\") = "
                + "| BAR | BAZ ";
            var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParseSchemaDefinition()
        {
            // arrange
            string sourceText = "\"\"\"\nDescription\n\"\"\"" +
                "schema @foo(a: \"123\") " +
                "{ query: Foo mutation: Bar subscription: Baz }";
            var parser = new Parser();

            // act
            DocumentNode document = parser.Parse(sourceText);

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }
    }
}
