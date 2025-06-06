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
        ArgumentNullException.ThrowIfNull(initialType);

        Types.Push(initialType);
        Scopes = new Stack<FilterScope<T>>();
        Scopes.Push(filterScope ?? CreateScope());
    }

    /// <inheritdoc />
    public Stack<FilterScope<T>> Scopes { get; }

    /// <inheritdoc />
    public Stack<IType> Types { get; } = new Stack<IType>();

    /// <inheritdoc />
    public Stack<IInputValueDefinition> Operations { get; } = new Stack<IInputValueDefinition>();

    /// <inheritdoc />
    public IList<IError> Errors { get; } = [];

    /// <inheritdoc />
    public virtual FilterScope<T> CreateScope()
    {
        return new FilterScope<T>();
    }
}
