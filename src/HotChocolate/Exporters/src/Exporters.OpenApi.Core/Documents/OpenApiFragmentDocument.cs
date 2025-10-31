using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Exporters.OpenApi;

public sealed record OpenApiFragmentDocument(
    string Id,
    string Name,
    string? Description,
    ITypeDefinition TypeCondition,
    FragmentDefinitionNode FragmentDefinition,
    Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup,
    HashSet<string> ExternalFragmentReferences) : IOpenApiDocument;
