namespace HotChocolate.Data.Sorting;

public class SortProviderDescriptor<TContext>
    : ISortProviderDescriptor<TContext>
    where TContext : ISortVisitorContext
{
    protected SortProviderDescriptor()
    {
    }

    protected SortProviderConfiguration<TContext> Configuration { get; } = new();

    public SortProviderConfiguration<TContext> CreateConfiguration() => Configuration;

    public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        Func<SortProviderContext, TFieldHandler> factory)
        where TFieldHandler : ISortFieldHandler<TContext>
    {
        // TODO: Find a better way
        Configuration.HandlerFactories.Add(ctx => factory(ctx));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        TFieldHandler fieldHandler)
        where TFieldHandler : ISortFieldHandler<TContext>
    {
        // TODO: Find a better way
        Configuration.HandlerFactories.Add(_ => fieldHandler);
        return this;
    }

    public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
        Func<SortProviderContext, TOperationHandler> factory)
        where TOperationHandler : ISortOperationHandler<TContext>
    {
        // TODO: Find a better way
        Configuration.OperationHandlerFactories.Add(ctx => factory(ctx));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
        TOperationHandler operationHandler)
        where TOperationHandler : ISortOperationHandler<TContext>
    {
        // TODO: Find a better way
        Configuration.OperationHandlerFactories.Add(_ => operationHandler);
        return this;
    }

    public static SortProviderDescriptor<TContext> New() => new();
}
