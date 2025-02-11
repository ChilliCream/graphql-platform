namespace HotChocolate.Types;

public interface IDirective
{
    string Name { get; }

    IDirectiveDefinition Definition { get; }

    IReadOnlyArgumentAssignmentCollection Arguments { get; }
}
