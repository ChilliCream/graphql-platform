using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Factories;

internal class SchemaSyntaxVisitorContext : ISyntaxVisitorContext
{
    public SchemaSyntaxVisitorContext(IDescriptorContext directiveContext)
    {
        DirectiveContext = directiveContext;
    }

    public List<TypeReference> Types { get; } = [];

    public IReadOnlyCollection<DirectiveNode>? Directives { get; set; }

    public string? QueryTypeName { get; set; }

    public string? MutationTypeName { get; set; }

    public string? SubscriptionTypeName { get; set; }

    public string? Description { get; set; }

    public IDescriptorContext DirectiveContext { get; }
}
