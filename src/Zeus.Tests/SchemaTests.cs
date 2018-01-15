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
            Assert.Equal("String", field.Type.ToString());
            Assert.Equal(TypeKind.Scalar, field.Type.Kind);
            Assert.True(field.Type.IsNullable);
            Assert.Null(field.Type.ElementType);
        }

        [Fact]
        public void DeserializeSimpleSchemaWithNonNullableField()
        {
            // arrange
            Source source = new Source(@"type Foo { bar: String! }");
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
            Assert.Equal("String!", field.Type.ToString());
            Assert.Equal(TypeKind.Scalar, field.Type.Kind);
            Assert.False(field.Type.IsNullable);
            Assert.Null(field.Type.ElementType);
        }

        [Fact]
        public void DeserializeSimpleSchemaWithListField()
        {
            // arrange
            Source source = new Source(@"type Foo { bar: [String] }");
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
            Assert.Equal("List", field.Type.Name);
            Assert.Equal("[String]", field.Type.ToString());
            Assert.Equal(TypeKind.List, field.Type.Kind);
            Assert.True(field.Type.IsNullable);

            Assert.NotNull(field.Type.ElementType);
            Assert.Equal("String", field.Type.ElementType.Name);
            Assert.Equal("String", field.Type.ElementType.ToString());
            Assert.Equal(TypeKind.Scalar, field.Type.ElementType.Kind);
            Assert.True(field.Type.ElementType.IsNullable);
            Assert.Null(field.Type.ElementType.ElementType);
        }

        [Fact]
        public void DeserializeSchemaWithTwoTypes()
        {
            // arrange
            Source source = new Source("type Foo { bar: Bar }\r\ntype Bar { bar: String }");
            Lexer lexer = new Lexer();
            Parser parser = new Parser(lexer);

            // act
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor();
            parser.Parse(source).Accept(visitor);

            // assert
            Assert.Equal(2, visitor.ObjectTypes.Count);

            ObjectDeclaration objectType = visitor.ObjectTypes.First();
            Assert.Equal("Foo", objectType.Name);
            Assert.Equal(1, objectType.Fields.Count);
            Assert.True(objectType.Fields.ContainsKey("bar"));

            FieldDeclaration field = objectType.Fields["bar"];
            Assert.Equal("bar", field.Name);
            Assert.Empty(field.Arguments);

            Assert.NotNull(field.Type);
            Assert.Equal("Bar", field.Type.Name);
            Assert.Equal(TypeKind.Object, field.Type.Kind);
            Assert.True(field.Type.IsNullable);
            Assert.Null(field.Type.ElementType);
        }

        [Fact]
        public void DeserializeSchemaWithInputs()
        {
            // arrange
            Source source = new Source("type Foo { bar: String }\r\ninput Bar { bar: String }");
            Lexer lexer = new Lexer();
            Parser parser = new Parser(lexer);

            // act
            SchemaSyntaxVisitor visitor = new SchemaSyntaxVisitor();
            parser.Parse(source).Accept(visitor);

            // assert
            Assert.Equal(1, visitor.InputTypes.Count);

            InputDeclaration inputType = visitor.InputTypes.First();
            Assert.Equal("Bar", inputType.Name);
            Assert.Equal(1, inputType.Fields.Count);
            Assert.True(inputType.Fields.ContainsKey("bar"));

            InputFieldDeclaration field = inputType.Fields["bar"];
            Assert.Equal("bar", field.Name);

            Assert.NotNull(field.Type);
            Assert.Equal("String", field.Type.Name);
            Assert.Equal(TypeKind.Scalar, field.Type.Kind);
            Assert.True(field.Type.IsNullable);
            Assert.Null(field.Type.ElementType);
        }
    }
}
