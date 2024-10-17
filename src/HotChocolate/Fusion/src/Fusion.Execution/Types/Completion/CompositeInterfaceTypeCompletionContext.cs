using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types.Completion;

internal ref struct CompositeInterfaceTypeCompletionContext(
    DirectiveCollection directives,
    CompositeInterfaceTypeCollection interfaces)
{
    public DirectiveCollection Directives { get; } = directives;

    public CompositeInterfaceTypeCollection Interfaces { get; } = interfaces;
}
