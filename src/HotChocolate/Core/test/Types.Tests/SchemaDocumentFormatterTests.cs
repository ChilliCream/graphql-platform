using HotChocolate.Execution;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate;

public static class SchemaDocumentFormatterTests
{
    [Fact]
    public static async Task Add_No_Schema_Formatter()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              hello: String
            }
            """);
    }


    [Fact]
    public static async Task Add_Single_Schema_Formatter()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ConfigureSchemaServices(sp => sp.AddSingleton<ISchemaDocumentFormatter, Formatter1>())
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              hello: String
            }

            scalar Scalar1
            """);
    }

    [Fact]
    public static async Task Add_Two_Schema_Formatters()
    {
        var schema =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .ConfigureSchemaServices(
                    sp =>
                        sp.AddSingleton<ISchemaDocumentFormatter, Formatter1>()
                            .AddSingleton<ISchemaDocumentFormatter, Formatter2>())
                .BuildSchemaAsync();

        schema.MatchInlineSnapshot(
            """
            schema {
              query: Query
            }

            type Query {
              hello: String
            }

            scalar Scalar1

            scalar Scalar2
            """);
    }

    public class Query
    {
        public string Hello() => "Hello";
    }

    public class Formatter1 : ISchemaDocumentFormatter
    {
        public DocumentNode Format(DocumentNode schemaDocument)
        {
            var definitions = schemaDocument.Definitions.ToList();

            definitions.Add(
                new ScalarTypeDefinitionNode(
                    null,
                    new NameNode("Scalar1"),
                    null,
                    Array.Empty<DirectiveNode>()));

            return new DocumentNode(null, definitions);
        }
    }

    public class Formatter2 : ISchemaDocumentFormatter
    {
        public DocumentNode Format(DocumentNode schemaDocument)
        {
            var definitions = schemaDocument.Definitions.ToList();

            definitions.Add(
                new ScalarTypeDefinitionNode(
                    null,
                    new NameNode("Scalar2"),
                    null,
                    Array.Empty<DirectiveNode>()));

            return new DocumentNode(null, definitions);
        }
    }
}
