using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public interface ISortVisitorContext
{
    Stack<IType> Types { get; }

    Stack<IInputValueDefinition> Fields { get; }

    IList<IError> Errors { get; }
}
