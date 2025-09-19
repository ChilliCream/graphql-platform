using HotChocolate.Types;

namespace HotChocolate.Data.Sorting;

public abstract class SortVisitorContext<T> : ISortVisitorContext<T>
{
    protected SortVisitorContext(ISortInputType initialType)
    {
        ArgumentNullException.ThrowIfNull(initialType);
        Types.Push(initialType);
    }

    public Stack<IType> Types { get; } = [];

    public Stack<IInputValueDefinition> Fields { get; } = [];

    public IList<IError> Errors { get; } = [];

    public Queue<T> Operations { get; } = [];

    public Stack<T> Instance { get; } = [];
}
