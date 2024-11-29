using HotChocolate.Types;

namespace HotChocolate.Data.Filters;

/// <inheritdoc />
public abstract class FilterVisitorContext<T>
    : IFilterVisitorContext<T>
{
    protected FilterVisitorContext(
        IFilterInputType initialType,
        FilterScope<T>? filterScope = null)
    {
        if (initialType is null)
        {
            throw new ArgumentNullException(nameof(initialType));
        }

        Types.Push(initialType);
        Scopes = new Stack<FilterScope<T>>();
        Scopes.Push(filterScope ?? CreateScope());
    }

    /// <inheritdoc />
    public Stack<FilterScope<T>> Scopes { get; }

    /// <inheritdoc />
    public Stack<IType> Types { get; } = new Stack<IType>();

    /// <inheritdoc />
    public Stack<IInputField> Operations { get; } = new Stack<IInputField>();

    /// <inheritdoc />
    public IList<IError> Errors { get; } = new List<IError>();

    /// <inheritdoc />
    public virtual FilterScope<T> CreateScope()
    {
        return new FilterScope<T>();
    }
}
