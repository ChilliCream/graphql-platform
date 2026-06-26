using HotChocolate.Execution;
using HotChocolate.Language;
using HotChocolate.StarWars;
using HotChocolate.Types.Mutable;
using Microsoft.Extensions.DependencyInjection;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration;

internal static class TestSchemaHelper
{
    public static async Task<MutableSchemaDefinition> CreateStarWarsSchemaAsync(
        params string[] extensions)
    {
        var schema =
            await new ServiceCollection()
                .AddStarWarsRepositories()
                .AddGraphQL()
                .AddStarWars()
                .BuildSchemaAsync(cancellationToken: TestContext.Current.CancellationToken);

        var files = new List<GraphQLFile> { new(schema.ToSyntaxNode()) };

        foreach (var extension in extensions)
        {
            files.Add(new GraphQLFile(Utf8GraphQLParser.Parse(extension)));
        }

        return SchemaHelper.Load(files);
    }
}
