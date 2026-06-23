using System.Diagnostics.CodeAnalysis;
using HotChocolate.Language;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using static CookieCrumble.FileResource;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class TestDataHelper
{
    public static ClientModel CreateClientModelAsync(
        string queryResource,
        string schemaResource)
    {
        var schema = SchemaHelper.Load(
            [
                new(Utf8GraphQLParser.Parse(Open(schemaResource))),
                new(Utf8GraphQLParser.Parse("extend schema @key(fields: \"id\")"))
            ]);

        var document = Utf8GraphQLParser.Parse(Open(queryResource));

        return DocumentAnalyzer
            .New()
            .SetSchema(schema)
            .AddDocument(document)
            .Analyze();
    }

    public static async Task<ClientModel> CreateClientModelAsync([StringSyntax("graphql")] string query)
    {
        var schema = await TestSchemaHelper.CreateStarWarsSchemaAsync(
            "extend schema @key(fields: \"id\")");

        var document = Utf8GraphQLParser.Parse(query);

        return DocumentAnalyzer
            .New()
            .SetSchema(schema)
            .AddDocument(document)
            .Analyze();
    }
}
