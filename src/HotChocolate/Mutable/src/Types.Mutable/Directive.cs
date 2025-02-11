using System.Collections.Immutable;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

public sealed class Directive : IDirective
{
    public Directive(MutableDirectiveDefinition type, params ImmutableArray<ArgumentAssignment> arguments)
    {
        Definition = type;
        Arguments = new ArgumentAssignmentCollection(arguments);
    }

    public Directive(MutableDirectiveDefinition type, IEnumerable<ArgumentAssignment> arguments)
    {
        Definition = type;
        Arguments = new ArgumentAssignmentCollection([..arguments]);
    }

    public string Name => Definition.Name;

    public MutableDirectiveDefinition Definition { get; }

    IDirectiveDefinition IDirective.Definition => Definition;

    public ArgumentAssignmentCollection Arguments { get; }

    public override string ToString()
        => Format(this).ToString(true);
}
