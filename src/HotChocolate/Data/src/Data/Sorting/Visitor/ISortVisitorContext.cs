using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public interface ISortVisitorContext
{
    Stack<IType> Types { get; }

    Stack<IInputField> Fields { get; }

    IList<IError> Errors { get; }
}
