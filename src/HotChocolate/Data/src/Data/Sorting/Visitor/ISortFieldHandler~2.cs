namespace HotChocolate.Data.Sorting
{
    public interface ISortFieldHandler<in TContext, T>
        : ISortFieldHandler<TContext>
        where TContext : SortVisitorContext<T>
    {
    }
}
