using HotChocolate.Types;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Skimmed;

public sealed class Directive : ITypeSystemMemberDefinition, IDirective
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

    IDirectiveDefinition IDirective.Definition => Definition;

    public ArgumentAssignmentCollection Arguments { get; }

    IReadOnlyArgumentAssignmentCollection IDirective.Arguments => Arguments;

    public override string ToString()
        => RewriteDirective(this).ToString(true);
}
