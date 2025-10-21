using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Exporters.OpenApi;

internal sealed class OpenApiFragmentDocument(
    string id,
    FragmentDefinitionNode fragmentDefinition,
    ITypeDefinition typeCondition)
    : IOpenApiDocument
{
    public string Id { get; } = id;

    public ITypeDefinition TypeCondition { get; } = typeCondition;

    public FragmentDefinitionNode FragmentDefinition { get; } = fragmentDefinition;
}
