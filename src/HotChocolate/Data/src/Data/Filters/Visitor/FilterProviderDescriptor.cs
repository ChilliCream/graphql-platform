namespace HotChocolate.Data.Filters;

public class FilterProviderDescriptor<TContext>
    : IFilterProviderDescriptor<TContext>
    where TContext : IFilterVisitorContext
{
    protected FilterProviderDescriptor()
    {
    }

    protected FilterProviderConfiguration Configuration { get; } = new();

    public FilterProviderConfiguration CreateConfiguration() => Configuration;

    public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        Func<FilterProviderContext, TFieldHandler> factory)
        where TFieldHandler : IFilterFieldHandler<TContext>
    {
        Configuration.FieldHandlerConfigurations.Add(new FilterFieldHandlerConfiguration(ctx => factory(ctx)));
        return this;
    }

    public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        TFieldHandler fieldHandler)
        where TFieldHandler : IFilterFieldHandler<TContext>
    {
        Configuration.FieldHandlerConfigurations.Add(new FilterFieldHandlerConfiguration(fieldHandler));
        return this;
    }

    public static FilterProviderDescriptor<TContext> New() => new();
}
