namespace HotChocolate.Data.Filters
{
    /// <inheritdoc/>
    public interface IFilterFieldHandler<in TContext, T>
        : IFilterFieldHandler<TContext>
        where TContext : FilterVisitorContext<T>
    {
    }
}
