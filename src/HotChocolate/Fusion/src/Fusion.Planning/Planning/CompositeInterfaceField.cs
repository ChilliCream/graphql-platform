using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeInterfaceField(
    string name,
    string? description,
    bool isDeprecated,
    string? deprecationReason,
    DirectiveCollection directives,
    CompositeInputFieldCollection arguments,
    ICompositeType type,
    SourceInterfaceFieldCollection sources)
    : ICompositeField
{
    public string Name { get; } = name;

    public string? Description { get; } = description;

    public bool IsDeprecated { get; } = isDeprecated;

    public string? DeprecationReason { get; } = deprecationReason;

    public DirectiveCollection Directives { get; } = directives;

    public CompositeInputFieldCollection Arguments { get; } = arguments;

    public ICompositeType Type { get; } = type;

    public SourceInterfaceFieldCollection Sources { get; } = sources;
}
