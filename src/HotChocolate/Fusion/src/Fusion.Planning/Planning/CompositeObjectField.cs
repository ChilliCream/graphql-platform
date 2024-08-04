using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeObjectField(
    string name,
    string? description,
    bool isDeprecated,
    string? deprecationReason,
    CompositeInputFieldCollection arguments)
    : ICompositeField
{
    public string Name { get; } = name;

    public string? Description { get; } = description;

    public bool IsDeprecated { get; } = isDeprecated;

    public string? DeprecationReason { get; } = deprecationReason;

    public DirectiveCollection Directives { get; } = default!;

    public CompositeInputFieldCollection Arguments { get; } = arguments;

    public ICompositeType Type { get; private set; } = default!;

    public SourceObjectFieldCollection Sources { get; } = default!;


}
