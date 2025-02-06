using HotChocolate.Types;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class Directive : ITypeSystemMemberDefinition, IReadOnlyDirective
{
    public Directive(DirectiveDefinition type, params ArgumentAssignment[] arguments)
        : this(type, (IReadOnlyList<ArgumentAssignment>)arguments)
    {
    }

    public Directive(DirectiveDefinition type, IReadOnlyList<ArgumentAssignment> arguments)
    {
        Definition = type;
        Arguments = new(arguments);
    }

    public string Name => Definition.Name;

    public DirectiveDefinition Definition { get; }

    IReadOnlyDirectiveDefinition IReadOnlyDirective.Definition => Definition;

    public ArgumentAssignmentCollection Arguments { get; }

    IReadOnlyArgumentAssignmentCollection IReadOnlyDirective.Arguments => Arguments;

    public override string ToString()
        => RewriteDirective(this).ToString(true);
}
