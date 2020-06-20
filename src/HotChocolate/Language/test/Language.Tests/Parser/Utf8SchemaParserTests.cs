using System.Text;
using ChilliCream.Testing;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Language
{
    public class Utf8SchemaParserTests
    {
        [Fact]
        public void ParserSimpleObjectType()
        {
            // arrange
            string sourceText = "type a @foo(a: \"123\") " +
                "{ b: String @foo(a: \"123\") " +
                "c(d: F = ENUMVALUE @foo(a: \"123\")): Int }";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParserInputObjectType()
        {
            // arrange
            string sourceText = "input a @foo(a: \"123\") " +
                "{ b: String @foo(a: \"123\") c: Int = 123 }";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParserScalarType()
        {
            // arrange
            string sourceText = "scalar FOO @foo(a: \"123\")";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParserSimpleInterfaceType()
        {
            // arrange
            string sourceText = "interface a implements e @foo(a: \"123\") " +
                "{ b: String @foo(a: \"123\") " +
                "c(d: F = ENUMVALUE @foo(a: \"123\")): Int }";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParseEnum()
        {
            // arrange
            string sourceText = "enum Foo @foo(a: \"123\") "
                + "{ BAR @foo(a: 123) , BAZ }";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParseUnion()
        {
            // arrange
            string sourceText = "union Foo @foo(a: \"123\") = "
                + "BAR | BAZ ";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void ParseUnion_LeadingPipe()
        {
            // arrange
            string sourceText = "union Foo @foo(a: \"123\") = "
                + "| BAR | BAZ ";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

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
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            SchemaSyntaxSerializer.Serialize(document).MatchSnapshot();
        }

        [Fact]
        public void OneGraph_Schema()
        {
            // arrange
            byte[] sourceText = Encoding.UTF8.GetBytes(FileResource.Open("onegraph.graphql"));

            // act
            DocumentNode document =  Utf8GraphQLParser.Parse(sourceText);

            // assert
            document.ToString().MatchSnapshot();
        }
    }
}
