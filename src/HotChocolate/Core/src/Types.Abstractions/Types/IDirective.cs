namespace HotChocolate.Types;

public interface IDirective : INameProvider
{
    IDirectiveDefinition Definition { get; }

    IReadOnlyArgumentAssignmentCollection Arguments { get; }
}
