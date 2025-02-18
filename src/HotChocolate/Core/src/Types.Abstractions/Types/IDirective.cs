namespace HotChocolate.Types;

public interface IDirective : INameProvider, ISyntaxNodeProvider
{
    IDirectiveDefinition Definition { get; }

    ArgumentAssignmentCollection Arguments { get; }
}
