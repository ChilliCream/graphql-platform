using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiFragmentDocument(
    string Name,
    string? Description,
    FragmentDefinitionNode FragmentDefinition,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDocument;
