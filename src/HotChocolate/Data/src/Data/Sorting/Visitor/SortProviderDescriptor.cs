namespace HotChocolate.Data.Sorting;

public class SortProviderDescriptor<TContext>
    : ISortProviderDescriptor<TContext>
    where TContext : ISortVisitorContext
{
    protected SortProviderDescriptor()
    {
    }

    protected SortProviderConfiguration Configuration { get; } = new();

    public SortProviderConfiguration CreateConfiguration() => Configuration;

    public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
        where TFieldHandler : ISortFieldHandler<TContext>
    {
        Configuration.Handlers.Add((typeof(TFieldHandler), null));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        TFieldHandler fieldHandler)
        where TFieldHandler : ISortFieldHandler<TContext>
    {
        Configuration.Handlers.Add((typeof(TFieldHandler), fieldHandler));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>()
        where TOperationHandler : ISortOperationHandler<TContext>
    {
        Configuration.OperationHandlers.Add((typeof(TOperationHandler), null));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
        TOperationHandler operationHandler)
        where TOperationHandler : ISortOperationHandler<TContext>
    {
        Configuration.OperationHandlers.Add((typeof(TOperationHandler), operationHandler));
        return this;
    }

    public static SortProviderDescriptor<TContext> New() =>
        new SortProviderDescriptor<TContext>();
}
