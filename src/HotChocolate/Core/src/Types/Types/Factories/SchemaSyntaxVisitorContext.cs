using System.Collections.Immutable;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Factories;

internal class SchemaSyntaxVisitorContext(IDescriptorContext descriptorContext)
{
    public List<TypeReference> Types { get; } = [];

    public IReadOnlyCollection<DirectiveNode>? Directives { get; set; }

    public ImmutableDictionary<string, IReadOnlyList<DirectiveNode>> ScalarDirectives { get; set; } =
        ImmutableDictionary<string, IReadOnlyList<DirectiveNode>>.Empty;

    public string? QueryTypeName { get; set; }

    public string? MutationTypeName { get; set; }

    public string? SubscriptionTypeName { get; set; }

    public string? Description { get; set; }

    public IDescriptorContext DescriptorContext { get; } = descriptorContext;
}
