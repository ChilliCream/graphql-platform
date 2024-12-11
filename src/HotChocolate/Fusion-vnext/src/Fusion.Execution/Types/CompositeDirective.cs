using HotChocolate.Fusion.Types.Collections;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Types;

public sealed class CompositeDirective(
    CompositeDirectiveType type,
    IReadOnlyList<ArgumentAssignment> arguments)
{
    public string Name => Type.Name;

    public CompositeDirectiveType Type { get; } = type;

    public ArgumentAssignmentCollection Arguments { get; } = new(arguments);

    public DirectiveNode ToSyntaxNode()
        => new(new NameNode(Name), Arguments.ToSyntaxNodes());
}
