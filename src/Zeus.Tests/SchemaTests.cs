using System;
using System.Collections.Immutable;
using System.Linq;
using GraphQLParser;
using GraphQLParser.AST;
using Xunit;
using Zeus.Types;

namespace Zeus.Tests
{
    public class SchemaTests
    {
        [Fact]
        public void DeserializeSimpleSchema()
        {
            // arrange
            Source source = new Source(@"type Foo { bar: String }");
            Lexer lexer = new Lexer();
            Parser parser = new Parser(lexer);

            // act
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor();
            parser.Parse(source).Accept(visitor);

            // assert
            Assert.Equal(1, visitor.ObjectTypes.Count);

            ObjectDeclaration objectType = visitor.ObjectTypes.First();
            Assert.Equal("Foo", objectType.Name);
            Assert.Equal(1, objectType.Fields.Count);
            Assert.True(objectType.Fields.ContainsKey("bar"));

            FieldDeclaration field = objectType.Fields["bar"];
            Assert.Equal("bar", field.Name);
            Assert.Empty(field.Arguments);

            Assert.NotNull(field.Type);
            Assert.Equal("String", field.Type.Name);
            Assert.Equal(TypeKind.Scalar, field.Type.Kind);
            Assert.True(field.Type.IsNullable);
            Assert.Null(field.Type.ElementType);
        }
    }
}
