using HotChocolate.Fusion.Planning.Collections;
using HotChocolate.Fusion.Planning.Completion;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Planning;

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
        Directives = CompletionTools.CreateDirectiveCollection(context.Context, context.Directives);
    }
}
