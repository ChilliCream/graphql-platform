using System.Threading.Tasks;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.CodeGeneration;
using StrawberryShake.CodeGeneration.Analyzers;
using StrawberryShake.CodeGeneration.Analyzers.Models;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake
{
    public static class TestHelper
    {
        public static PropertyDescriptor GetNamedNonNullStringTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NonNullTypeDescriptor(new NamedTypeDescriptor("string", "System")));

        public static PropertyDescriptor GetNamedNonNullIntTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NonNullTypeDescriptor(new NamedTypeDescriptor("int", "System")));

        public static async Task<ClientModel> CreateClientModelAsync(
            params string[] sourceText)
        {
            ISchema schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            schema = SchemaHelper.Load(schema.ToDocument());

            var analyzer = new DocumentAnalyzer();

            analyzer.SetSchema(schema);

            foreach (string source in sourceText)
            {
                analyzer.AddDocument(Utf8GraphQLParser.Parse(source));
            }

            return analyzer.Analyze();
        }
    }
}
