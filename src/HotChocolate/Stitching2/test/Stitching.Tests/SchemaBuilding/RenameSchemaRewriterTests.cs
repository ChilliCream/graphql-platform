using System.Collections.Generic;
using ChilliCream.Testing;
using HotChocolate.Language;
using HotChocolate.Language.Utilities;
using HotChocolate.Language.Visitors;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Stitching.SchemaBuilding
{
    public class RenameSchemaRewriterTests
    {
        [Fact]
        public void Rename_ObjectType()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                type Foo { 
                    field: String 
                }");

        [Fact]
        public void Rename_ObjectType_Annotated()
            => Rewrite(@"
                type Foo @rename(to: ""Bar"") { 
                    field: String 
                }");

        [Fact]
        public void Rename_ObjectTypeExtension()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                extend type Foo { 
                    field: String 
                }");

        [Fact]
        public void Rename_InterfaceType()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                interface Foo { 
                    field: String 
                }");

        [Fact]
        public void Rename_InterfaceTypeExtension()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                extend interface Foo { 
                    field: String 
                }");

        [Fact]
        public void Rename_UnionType()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                union Foo = Abc | Def");

        [Fact]
        public void Rename_UnionTypeExtension()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                extend union Foo = Abc | Def");

        [Fact]
        public void Rename_EnumType()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                enum Foo { 
                    ABC
                    DEF
                }");

        [Fact]
        public void Rename_EnumTypeExtension()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                extend enum Foo { 
                    ABC
                    DEF
                }");

        [Fact]
        public void Rename_ScalarType()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                scalar Foo");

        [Fact]
        public void Rename_ScalarTypeExtension()
            => Rewrite(@"
                extend schema @rename(from: ""Foo"", to: ""Bar"")
                
                extend scalar Foo @baz");

        [Fact]
        public void Build_Context()
            => Rewrite(
                @"type Foo { 
                    field1: String
                }
                
                extend type Foo { 
                    field2: String
                }",
                @"extend schema @rename(from: ""Foo"", to: ""Bar"")");

        [Fact]
        public void Rename_ObjectType_Field()
            => Rewrite(@"
                extend schema @rename(from: ""Foo.field1"", to: ""field2"")
                
                type Foo { 
                    field1: String 
                }");

        [Fact]
        public void Rename_ObjectType_Field_Annotated()
            => Rewrite(@"
                type Foo { 
                    field1: String @rename(to: ""field2"") 
                }");

        [Fact]
        public void Rename_InterfaceType_Field()
           => Rewrite(@"
                extend schema @rename(from: ""Foo.field1"", to: ""field2"")
                
                interface Foo { 
                    field1: String 
                }");

        [Fact]
        public void Rename_InterfaceType_Field_Annotated()
            => Rewrite(@"
                interface Foo { 
                    field1: String @rename(to: ""field2"") 
                }");

        [Fact]
        public void Rename_InputObjectType_Field()
           => Rewrite(@"
                extend schema @rename(from: ""Foo.field1"", to: ""field2"")
                
                input Foo { 
                    field1: String 
                }");

        [Fact]
        public void Rename_InputObjectType_Field_Annotated()
            => Rewrite(@"
                input Foo { 
                    field1: String @rename(to: ""field2"") 
                }");

        private void Rewrite(string schema, params string[] extensions)
        {
            // arrange
            var context = new SchemaRewriterContext { Rewriters = { new RenameSchemaRewriter() } };
            var document = Utf8GraphQLParser.Parse(schema);

            // act
            var visitor = new SchemaInspector();
            visitor.Visit(document, context);

            foreach (string extension in extensions)
            {
                visitor.Visit(Utf8GraphQLParser.Parse(extension), context);
            }

            // assert
            var rewriter = new SchemaRewriter();
            rewriter.Rewrite(document, context).Print().MatchSnapshot();
        }
    }
}
