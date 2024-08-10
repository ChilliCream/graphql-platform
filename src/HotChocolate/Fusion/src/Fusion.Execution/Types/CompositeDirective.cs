using HotChocolate.Fusion.Types.Collections;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeDirective(
    CompositeDirectiveDefinition type,
    IReadOnlyList<ArgumentAssignment> arguments)
{
    public string Name => Type.Name;

    public CompositeDirectiveDefinition Type { get; } = type;

    public ArgumentAssignmentCollection Arguments { get; } = new(arguments);
}
