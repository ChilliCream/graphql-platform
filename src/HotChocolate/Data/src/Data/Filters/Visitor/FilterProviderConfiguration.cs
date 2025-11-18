namespace HotChocolate.Data.Filters;

public class FilterProviderConfiguration<TContext> where TContext : IFilterVisitorContext
{
    public IList<Func<FilterProviderContext, IFilterFieldHandler<TContext>>> HandlerFactories { get; } = [];
}
