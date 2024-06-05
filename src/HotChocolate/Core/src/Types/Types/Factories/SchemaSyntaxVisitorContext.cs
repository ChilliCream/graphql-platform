using System.Collections.Generic;
using HotChocolate.Configuration;
using HotChocolate.Language;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Types.Factories;

internal class SchemaSyntaxVisitorContext(IDescriptorContext descriptorContext)
{
    public List<TypeReference> Types { get; } = [];

    public TypeInterceptor? TypeInterceptor { get; set; }

    public IReadOnlyCollection<DirectiveNode>? Directives { get; set; }

    public string? QueryTypeName { get; set; }

    public string? MutationTypeName { get; set; }

    public string? SubscriptionTypeName { get; set; }

    public string? Description { get; set; }

    public IDescriptorContext DescriptorContext { get; } = descriptorContext;
}
