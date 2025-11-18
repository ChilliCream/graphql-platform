namespace HotChocolate.Data.Filters;

public class FilterProviderDescriptor<TContext>
    : IFilterProviderDescriptor<TContext>
    where TContext : IFilterVisitorContext
{
    protected FilterProviderDescriptor()
    {
    }

    protected FilterProviderConfiguration<TContext> Configuration { get; } = new();

    public FilterProviderConfiguration<TContext> CreateConfiguration() => Configuration;

    public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        Func<FilterProviderContext, TFieldHandler> factory)
        where TFieldHandler : IFilterFieldHandler<TContext>
    {
        // TODO: Find a better way
        Configuration.HandlerFactories.Add(ctx => factory(ctx));
        return this;
    }

    public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        TFieldHandler fieldHandler)
        where TFieldHandler : IFilterFieldHandler<TContext>
    {
        // TODO: Find a better way
        Configuration.HandlerFactories.Add(_ => fieldHandler);
        return this;
    }

    public static FilterProviderDescriptor<TContext> New() => new();
}
