using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Factories;

internal class SchemaSyntaxVisitorContext(
    IDescriptorContext descriptorContext,
    Dictionary<string, IReadOnlyList<DirectiveNode>> scalarDirectives)
{
    public List<TypeReference> Types { get; } = [];

    public IReadOnlyCollection<DirectiveNode>? Directives { get; set; }

    public Dictionary<string, IReadOnlyList<DirectiveNode>> ScalarDirectives => scalarDirectives;

    public string? QueryTypeName { get; set; }

    public string? MutationTypeName { get; set; }

    public string? SubscriptionTypeName { get; set; }

    public string? Description { get; set; }

    public IDescriptorContext DescriptorContext { get; } = descriptorContext;
}
