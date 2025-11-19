namespace HotChocolate.Data.Filters;

public readonly struct FilterFieldHandlerConfiguration
{
    private readonly Func<FilterProviderContext, IFilterFieldHandler>? _factory;
    private readonly IFilterFieldHandler? _instance;

    public FilterFieldHandlerConfiguration(IFilterFieldHandler instance)
    {
        _instance = instance;
    }

    public FilterFieldHandlerConfiguration(Func<FilterProviderContext, IFilterFieldHandler> factory)
    {
        _factory = factory;
    }

    public IFilterFieldHandler<TContext> Create<TContext>(FilterProviderContext context)
        where TContext : IFilterVisitorContext
    {
        if (_instance is not null)
        {
            if (_instance is not IFilterFieldHandler<TContext> handler)
            {
                throw new InvalidOperationException(
                    $"Expected handler to be of type IFilterFieldHandler<{typeof(TContext).Name}>");
            }

            return handler;
        }

        if (_factory is null)
        {
            throw new InvalidOperationException("Expected to have either a factory or an instance.");
        }

        var instance = _factory(context);

        if (instance is not IFilterFieldHandler<TContext> casted)
        {
            throw new InvalidOperationException(
                $"Expected handler to be of type IFilterFieldHandler<{typeof(TContext).Name}>");
        }

        return casted;
    }
}
