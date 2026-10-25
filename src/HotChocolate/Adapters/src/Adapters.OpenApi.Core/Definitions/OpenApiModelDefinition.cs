using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiModelDefinition(
    string Name,
    string? Description,
    DocumentNode Document,
    FragmentDefinitionNode FragmentDefinition,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentsByName,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDefinition;
