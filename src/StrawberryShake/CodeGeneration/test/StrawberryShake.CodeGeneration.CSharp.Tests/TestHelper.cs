using System.Linq;
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
                new NonNullTypeDescriptor(new NamedTypeDescriptor("string", "System", false)));

        public static PropertyDescriptor GetNamedNullableStringTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NamedTypeDescriptor("string", "System", false));

        public static PropertyDescriptor GetNamedNonNullIntTypeReference(
            string referenceName) =>
            new(
                referenceName,
                new NonNullTypeDescriptor(new NamedTypeDescriptor("int", "System", false)));

        public static async Task<ClientModel> CreateClientModelAsync(
            params string[] sourceText)
        {
            ISchema schema =
                await new ServiceCollection()
                    .AddStarWarsRepositories()
                    .AddGraphQL()
                    .AddStarWars()
                    .BuildSchemaAsync();

            var documents = sourceText
                .Select(st => Utf8GraphQLParser.Parse(st))
                .ToList();

            var typeSystemDocs = documents.GetTypeSystemDocuments().ToList();
            typeSystemDocs.Add(schema.ToDocument());

            var executableDocs = documents.GetExecutableDocuments().ToList();

            var analyzer = new DocumentAnalyzer();

            analyzer.SetSchema(SchemaHelper.Load(typeSystemDocs));

            foreach (DocumentNode executable in executableDocs)
            {
                analyzer.AddDocument(executable);
            }

            return analyzer.Analyze();
        }
    }
}
