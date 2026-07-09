namespace HotChocolate.Data.Sorting;

public readonly struct SortOperationHandlerConfiguration
{
    private readonly Func<SortProviderContext, ISortOperationHandler>? _factory;
    private readonly ISortOperationHandler? _instance;

    public SortOperationHandlerConfiguration(ISortOperationHandler instance)
    {
        _instance = instance;
    }

    public SortOperationHandlerConfiguration(Func<SortProviderContext, ISortOperationHandler> factory)
    {
        _factory = factory;
    }

    public ISortOperationHandler? Instance => _instance;

    public ISortOperationHandler<TContext> Create<TContext>(SortProviderContext context)
        where TContext : ISortVisitorContext
    {
        if (_instance is not null)
        {
            if (_instance is not ISortOperationHandler<TContext> handler)
            {
                throw new InvalidOperationException(
                    $"Expected handler to be of type ISortOperationHandler<{typeof(TContext).Name}>");
            }

            return handler;
        }

        if (_factory is null)
        {
            throw new InvalidOperationException("Expected to have either a factory or an instance.");
        }

        var instance = _factory(context);

        if (instance is not ISortOperationHandler<TContext> casted)
        {
            throw new InvalidOperationException(
                $"Expected handler to be of type ISortOperationHandler<{typeof(TContext).Name}>");
        }

        return casted;
    }
}
