using HotChocolate.Language;

namespace HotChocolate.Adapters.OpenApi;

public sealed record OpenApiModelDefinition : IOpenApiDefinition
{
    internal OpenApiModelDefinition(
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

    public static OpenApiModelDefinition From(
        OpenApiModelSettingsDto settings,
        string name,
        DocumentNode document)
    {
        if (!document.Definitions.All(d => d is FragmentDefinitionNode))
        {
            throw new ArgumentException("The document can only contain fragment definitions.",
                nameof(document));
        }

        var fragmentDefinition = document.Definitions.OfType<FragmentDefinitionNode>().FirstOrDefault() ??
            throw new ArgumentException("The document must contain at least one fragment definition.",
                nameof(document));

        if (fragmentDefinition.Name.Value != name)
        {
            throw new ArgumentException("The provided name does not match the name of the first fragment definition.",
                nameof(name));
        }

        var fragmentReferences = FragmentReferenceFinder.Find(document, fragmentDefinition);

        return new OpenApiModelDefinition(
            name,
            settings.Description,
            document,
            fragmentDefinition,
            fragmentReferences.Local,
            fragmentReferences.External);
    }

    public OpenApiModelSettingsDto ToDto()
        => new OpenApiModelSettingsDto(Description);
}
