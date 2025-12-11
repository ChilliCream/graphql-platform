using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public interface IOpenApiDocument
{
    string? Description { get; }

    Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup { get; }

    HashSet<string> ExternalFragmentReferences { get; }
}
