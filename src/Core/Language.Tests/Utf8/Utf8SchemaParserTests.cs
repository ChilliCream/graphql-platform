using System.Text;
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
            string sourceText = "type a { b: String c: Int }";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<ObjectTypeDefinitionNode>(t);
                    var objectTypeDefinition = (ObjectTypeDefinitionNode)t;
                    Assert.Equal(NodeKind.ObjectTypeDefinition, objectTypeDefinition.Kind);
                    Assert.Equal("a", objectTypeDefinition.Name.Value);
                    Assert.Collection(objectTypeDefinition.Fields,
                        f =>
                        {
                            Assert.Equal("b", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("String", ((NamedTypeNode)f.Type).Name.Value);
                        },
                        f =>
                        {
                            Assert.Equal("c", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("Int", ((NamedTypeNode)f.Type).Name.Value);
                        });
                });
        }

        [Fact]
        public void ParserSimpleInterfaceType()
        {
            // arrange
            string sourceText = "interface a { b: String c: Int }";
            var parser = new Utf8GraphQLParser(
                Encoding.UTF8.GetBytes(sourceText));

            // act
            DocumentNode document = parser.Parse();

            // assert
            Assert.Collection(document.Definitions,
                t =>
                {
                    Assert.IsType<InterfaceTypeDefinitionNode>(t);
                    var objectTypeDefinition = (InterfaceTypeDefinitionNode)t;
                    Assert.Equal(NodeKind.InterfaceTypeDefinition, objectTypeDefinition.Kind);
                    Assert.Equal("a", objectTypeDefinition.Name.Value);
                    Assert.Collection(objectTypeDefinition.Fields,
                        f =>
                        {
                            Assert.Equal("b", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("String", ((NamedTypeNode)f.Type).Name.Value);
                        },
                        f =>
                        {
                            Assert.Equal("c", f.Name.Value);
                            Assert.IsType<NamedTypeNode>(f.Type);
                            Assert.Equal("Int", ((NamedTypeNode)f.Type).Name.Value);
                        });
                });
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
    }
}
