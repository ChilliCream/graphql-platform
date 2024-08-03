using HotChocolate.Fusion.Planning.Collections;

namespace HotChocolate.Fusion.Planning;

public sealed class Directive(
    DirectiveDefinition type,
    IReadOnlyList<ArgumentAssignment> arguments)
{
    public string Name => Type.Name;

    public DirectiveDefinition Type { get; } = type;

    public ArgumentAssignmentCollection Arguments { get; } = new(arguments);
}
