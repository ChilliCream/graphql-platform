using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning.Completion;

internal ref struct CompositeObjectTypeCompletionContext(
    DirectiveCollection directives,
    CompositeInterfaceTypeCollection interfaces,
    SourceObjectTypeCollection sources)
{
    public DirectiveCollection Directives { get; } = directives;

    public CompositeInterfaceTypeCollection Interfaces { get; } = interfaces;

    public SourceObjectTypeCollection Sources { get; } = sources;
}
