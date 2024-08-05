using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeInterfaceField(
    string name,
    string? description,
    bool isDeprecated,
    string? deprecationReason,
    DirectiveCollection directives,
    CompositeInputFieldCollection arguments,
    ICompositeType type,
    SourceInterfaceMemberCollection sources)
    : ICompositeField
{
    public string Name { get; } = name;

    public string? Description { get; } = description;

    public bool IsDeprecated { get; } = isDeprecated;

    public string? DeprecationReason { get; } = deprecationReason;

    public DirectiveCollection Directives { get; } = directives;

    public CompositeInputFieldCollection Arguments { get; } = arguments;

    public ICompositeType Type { get; } = type;

    public SourceInterfaceMemberCollection Sources { get; } = sources;
}
