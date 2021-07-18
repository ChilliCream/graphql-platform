using System.Collections.Generic;
using HotChocolate.Language;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.CodeGeneration.Neo4J
{
    public class Neo4JCodeGeneratorTests
    {
        [Fact]
        public void Works()
        {
            // Arrange
            DocumentNode? doc = Utf8GraphQLParser.Parse(@"
                type Movie @typeName(name: ""Foo"", pluralName: ""Bars"") {
                  title: String
                  year: Int
                  imdbRating: Float
                  baz: Baz
                  quox: String
                }

                type Actor {
                  name: String
                }

                type Baz {
                  foo: String
                }

                # settings would be annotated to the schema and translate directly into schema options
                schema
                  @paging(kind: NONE)
                  @filtering
                  @sorting
                {
                  query: Query
                }");

            var docs = new List<DocumentNode>() { doc };

            var context = new Neo4JCodeGeneratorContext(
                "MyNeo4J",
                "Neo4JDatabase",
                "CompanyName.Neo4J",
                docs);

            // Act
            CodeGenerationResult? result = new Neo4JCodeGenerator().Generate(context);

            // Assert
            Snapshot.Match(result);
        }
    }
}
