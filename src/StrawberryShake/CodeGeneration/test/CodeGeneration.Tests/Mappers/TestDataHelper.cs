using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using static ChilliCream.Testing.FileResource;

namespace StrawberryShake.CodeGeneration.Mappers;

public static class TestDataHelper
{
    public static ClientModel CreateClientModelAsync(
        string queryResource,
        string schemaResource)
    {
        var schema = SchemaHelper.Load(
            new GraphQLFile[]
            {
                new(Utf8GraphQLParser.Parse(Open(schemaResource))),
                new(Utf8GraphQLParser.Parse("extend schema @key(fields: \"id\")")),
            });

        var document = Utf8GraphQLParser.Parse(Open(queryResource));

        return DocumentAnalyzer
            .New()
            .SetSchema(schema)
            .AddDocument(document)
            .AnalyzeAsync()
            .Result;
    }

    public static async Task<ClientModel> CreateClientModelAsync(string query)
    {
        var schema =
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWars()
                .BuildSchemaAsync();

        schema = SchemaHelper.Load(
            new GraphQLFile[]
            {
                new(schema.ToDocument()),
                new(Utf8GraphQLParser.Parse("extend schema @key(fields: \"id\")")),
            });

        var document = Utf8GraphQLParser.Parse(query);

        return DocumentAnalyzer
            .New()
            .SetSchema(schema)
            .AddDocument(document)
            .AnalyzeAsync()
            .Result;
    }
}
