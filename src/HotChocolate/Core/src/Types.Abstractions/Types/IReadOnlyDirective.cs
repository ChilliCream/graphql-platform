namespace HotChocolate.Types;

public interface IReadOnlyDirective
{
    IReadOnlyDirectiveDefinition Definition { get; }

    IReadOnlyArgumentAssignmentCollection Arguments { get; }
}
