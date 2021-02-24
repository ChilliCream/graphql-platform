using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;
using static ChilliCream.Testing.FileResource;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class TestDataHelper
    {
        public static ClientModel CreateClientModelAsync(
            string queryResource, 
            string schemaResource)
        {
           ISchema schema = SchemaHelper.Load(
                (string.Empty, Utf8GraphQLParser.Parse(Open(schemaResource))),
                (string.Empty, Utf8GraphQLParser.Parse("extend schema @key(fields: \"id\")")));

            DocumentNode document = Utf8GraphQLParser.Parse(Open(queryResource));

            return DocumentAnalyzer
                .New()
                .SetSchema(schema)
                .AddDocument(document)
                .Analyze();
        }

        public static async Task<ClientModel> CreateClientModelAsync(string query)
        {
            ISchema schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            schema = SchemaHelper.Load(
                    (string.Empty, schema.ToDocument()),
                    (string.Empty, Utf8GraphQLParser.Parse("extend schema @key(fields: \"id\")")));

            DocumentNode document = Utf8GraphQLParser.Parse(query);

            return DocumentAnalyzer
                .New()
                .SetSchema(schema)
                .AddDocument(document)
                .Analyze();
        }
    }
}
