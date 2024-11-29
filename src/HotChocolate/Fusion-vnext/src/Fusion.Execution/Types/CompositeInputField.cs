using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeInputField(
    string name,
    string? description,
    IValueNode? defaultValue,
    bool isDeprecated,
    string? deprecationReason)
    : ICompositeField
{
    public string Name { get; } = name;

    public string? Description { get; } = description;

    public IValueNode? DefaultValue { get; } = defaultValue;

    public bool IsDeprecated { get; } = isDeprecated;

    public string? DeprecationReason { get; } = deprecationReason;

    public DirectiveCollection Directives { get; private set; } = default!;

    public ICompositeType Type { get; private set; } = default!;

    internal void Complete(CompositeInputFieldCompletionContext context)
    {
        Directives = context.Directives;
        Type = context.Type;
    }

    public override string ToString() => Name;
}
