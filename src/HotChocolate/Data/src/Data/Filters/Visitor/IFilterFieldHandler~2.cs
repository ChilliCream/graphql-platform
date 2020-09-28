namespace HotChocolate.Data.Filters
{
    public interface IFilterFieldHandler<in TContext, T>
        : IFilterFieldHandler<TContext>
        where TContext : FilterVisitorContext<T>
    {
    }
}
