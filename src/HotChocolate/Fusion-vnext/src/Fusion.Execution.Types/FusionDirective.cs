using HotChocolate.Language;
using HotChocolate.Serialization;
using HotChocolate.Types;
using ArgumentAssignmentCollection = HotChocolate.Fusion.Types.Collections.ArgumentAssignmentCollection;

namespace HotChocolate.Fusion.Types;

public sealed class FusionDirective : IDirective
{
    public FusionDirective(MutableDirectiveDefinition type, params ImmutableArray<ArgumentAssignment> arguments)
    {
        Definition = type;
        Arguments = new ArgumentAssignmentCollection(arguments);
    }

    public FusionDirective(MutableDirectiveDefinition type, IEnumerable<ArgumentAssignment> arguments)
    {
        Definition = type;
        Arguments = new ArgumentAssignmentCollection([..arguments]);
    }

    public string Name => Definition.Name;

    public FusionDirectiveDefinition Definition { get; }

    IDirectiveDefinition IDirective.Definition => Definition;

    public ArgumentAssignmentCollection Arguments { get; }

    public override string ToString()
        => SchemaDebugFormatter.Format(this).ToString(true);
}
