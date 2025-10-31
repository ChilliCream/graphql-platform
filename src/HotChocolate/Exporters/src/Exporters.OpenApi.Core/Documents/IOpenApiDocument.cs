using HotChocolate.Language;

namespace HotChocolate.Exporters.OpenApi;

public interface IOpenApiDocument
{
    string Id { get; }

    string Name { get; }

    string? Description { get; }

    Dictionary<string, FragmentDefinitionNode> LocalFragmentLookup { get; }

    HashSet<string> ExternalFragmentReferences { get; }
}
