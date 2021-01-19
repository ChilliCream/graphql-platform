using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Extensions;
using StrawberryShake.CodeGeneration.Utilities;
using Xunit;

namespace StrawberryShake.CodeGeneration.Mappers
{
    public static class TestDataHelper
    {
        public static async Task<ClientModel> CreateClientModelAsync(string query)
        {
            ISchema schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            schema = SchemaHelper.Load(schema.ToDocument());

            DocumentNode document = Utf8GraphQLParser.Parse(query);

            return DocumentAnalyzer
                .New()
                .SetSchema(schema)
                .AddDocument(document)
                .Analyze();
        }
    }
}
