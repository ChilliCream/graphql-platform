using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiModelDefinition : IOpenApiDefinition
{
    public OpenApiModelDefinition(
        string name,
        string? description,
        DocumentNode document,
        FragmentDefinitionNode fragmentDefinition,
        Dictionary<string, FragmentDefinitionNode> localFragmentsByName,
        HashSet<string> externalFragmentReferences)
    {
        Name = name;
        Description = description;
        Document = document;
        FragmentDefinition = fragmentDefinition;
        LocalFragmentsByName = localFragmentsByName;
        ExternalFragmentReferences = externalFragmentReferences;
    }

    public string Name { get; }

    public string? Description { get; }

    public DocumentNode Document { get; }

    public FragmentDefinitionNode FragmentDefinition { get; }

    public Dictionary<string, FragmentDefinitionNode> LocalFragmentsByName { get; }

    public HashSet<string> ExternalFragmentReferences { get; }
}
