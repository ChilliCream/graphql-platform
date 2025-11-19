namespace HotChocolate.Data.Sorting;

public readonly struct SortFieldHandlerConfiguration
{
    private readonly Func<SortProviderContext, ISortFieldHandler>? _factory;
    private readonly ISortFieldHandler? _instance;

    public SortFieldHandlerConfiguration(ISortFieldHandler instance)
    {
        _instance = instance;
    }

    public SortFieldHandlerConfiguration(Func<SortProviderContext, ISortFieldHandler> factory)
    {
        _factory = factory;
    }

    public ISortFieldHandler<TContext> Create<TContext>(SortProviderContext context)
        where TContext : ISortVisitorContext
    {
        if (_instance is not null)
        {
            if (_instance is not ISortFieldHandler<TContext> handler)
            {
                throw new InvalidOperationException(
                    $"Expected handler to be of type ISortFieldHandler<{typeof(TContext).Name}>");
            }

            return handler;
        }

        if (_factory is null)
        {
            throw new InvalidOperationException("Expected to have either a factory or an instance.");
        }

        var instance = _factory(context);

        if (instance is not ISortFieldHandler<TContext> casted)
        {
            throw new InvalidOperationException(
                $"Expected handler to be of type ISortFieldHandler<{typeof(TContext).Name}>");
        }

        return casted;
    }
}
