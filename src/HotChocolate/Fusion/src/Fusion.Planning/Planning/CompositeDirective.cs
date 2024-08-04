using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning;

public sealed class CompositeDirective(
    CompositeDirectiveDefinition type,
    IReadOnlyList<ArgumentAssignment> arguments)
{
    public string Name => Type.Name;

    public CompositeDirectiveDefinition Type { get; } = type;

    public ArgumentAssignmentCollection Arguments { get; } = new(arguments);
}
