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

    public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
        where TFieldHandler : IFilterFieldHandler<TContext>
    {
        Configuration.Handlers.Add((typeof(TFieldHandler), null));
        return this;
    }

    public IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        TFieldHandler fieldHandler)
        where TFieldHandler : IFilterFieldHandler<TContext>
    {
        Configuration.Handlers.Add((typeof(TFieldHandler), fieldHandler));
        return this;
    }

    public static FilterProviderDescriptor<TContext> New() =>
        new FilterProviderDescriptor<TContext>();
}
