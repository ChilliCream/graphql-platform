namespace HotChocolate.Data.Sorting
{
    public interface ISortOperationHandler<in TContext, T>
        : ISortOperationHandler<TContext>
        where TContext : SortVisitorContext<T>
    {
    }
}
