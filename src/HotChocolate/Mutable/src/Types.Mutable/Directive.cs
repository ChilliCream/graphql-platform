using System.Collections.Immutable;
using System.Runtime.InteropServices;
using static HotChocolate.Serialization.SchemaDebugFormatter;

namespace HotChocolate.Types.Mutable;

public sealed class Directive : ITypeSystemMember, IDirective
{
    public Directive(MutableDirectiveDefinition type, params ArgumentAssignment[] arguments)
    {
        Definition = type;
        Arguments = new ArgumentAssignmentCollection(ImmutableCollectionsMarshal.AsImmutableArray(arguments));
    }

    public Directive(MutableDirectiveDefinition type, ImmutableArray<ArgumentAssignment> arguments)
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

    IReadOnlyArgumentAssignmentCollection IDirective.Arguments => Arguments;

    public override string ToString()
        => Format(this).ToString(true);
}
