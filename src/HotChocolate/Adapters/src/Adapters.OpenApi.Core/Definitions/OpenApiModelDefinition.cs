using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiModelDefinition(
    string Name,
    string? Description,
    DocumentNode Document,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentsByName,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDefinition
{
    public FragmentDefinitionNode FragmentDefinition => Document.Definitions.OfType<FragmentDefinitionNode>().First();
}
