namespace HotChocolate.Data.Sorting;

public class SortProviderConfiguration<TContext> where TContext : ISortVisitorContext
{
    public IList<Func<SortProviderContext, ISortFieldHandler<TContext>>> HandlerFactories { get; } = [];

    public IList<Func<SortProviderContext, ISortOperationHandler<TContext>>> OperationHandlerFactories { get; } = [];
}
