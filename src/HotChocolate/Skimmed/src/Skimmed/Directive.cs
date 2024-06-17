using static HotChocolate.Skimmed.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class Directive : ITypeSystemMemberDefinition
{
    public Directive(DirectiveDefinition type, params ArgumentAssignment[] arguments)
        : this(type, (IReadOnlyList<ArgumentAssignment>)arguments)
    {
    }

    public Directive(DirectiveDefinition type, IReadOnlyList<ArgumentAssignment> arguments)
    {
        Type = type;
        Arguments = new(arguments);
    }

    public string Name => Type.Name;

    public DirectiveDefinition Type { get; }

    public ArgumentAssignmentCollection Arguments { get; }

    public override string ToString()
        => RewriteDirective(this).ToString(true);
}
