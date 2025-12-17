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

    public static OpenApiModelDefinition From(
        OpenApiModelSettingsDto settings,
        string name,
        DocumentNode document)
    {
        var fragmentReferences = FragmentReferenceFinder.Find(document);

        return new OpenApiModelDefinition(
            name,
            settings.Description,
            document,
            fragmentReferences.Local,
            fragmentReferences.External);
    }

    public OpenApiModelSettingsDto ToDto()
        => new OpenApiModelSettingsDto(Description);
}
