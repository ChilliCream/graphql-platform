using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xunit;

namespace HotChocolate.Language
{
    public class SchemaParserTests
    {
        [Fact]
        public void ParserSimpleObjectType()
        {
            // arrange
            string sourceText = "type a { b: String c: Int }";

            // act
            Parser parser = new Parser();
            DocumentNode document = parser.Parse(sourceText);

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

            // act
            Parser parser = new Parser();
            DocumentNode document = parser.Parse(sourceText);

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
    }
}
