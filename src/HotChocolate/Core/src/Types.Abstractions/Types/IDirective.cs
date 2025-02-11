namespace HotChocolate.Types;

public interface IDirective : INameProvider
{
    IDirectiveDefinition Definition { get; }

    ArgumentAssignmentCollection Arguments { get; }
}
