namespace HotChocolate.Data.Sorting
{
    /// <inheritdoc/>
    public interface ISortOperationHandler<in TContext, T>
        : ISortOperationHandler<TContext>
        where TContext : SortVisitorContext<T>
    {
    }
}
