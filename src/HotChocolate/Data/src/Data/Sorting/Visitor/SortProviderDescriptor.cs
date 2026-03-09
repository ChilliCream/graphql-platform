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

    public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        Func<SortProviderContext, TFieldHandler> factory)
        where TFieldHandler : ISortFieldHandler<TContext>
    {
        Configuration.FieldHandlerConfigurations.Add(new SortFieldHandlerConfiguration(ctx => factory(ctx)));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        TFieldHandler fieldHandler)
        where TFieldHandler : ISortFieldHandler<TContext>
    {
        Configuration.FieldHandlerConfigurations.Add(new SortFieldHandlerConfiguration(fieldHandler));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
        Func<SortProviderContext, TOperationHandler> factory)
        where TOperationHandler : ISortOperationHandler<TContext>
    {
        Configuration.OperationHandlerConfigurations.Add(new SortOperationHandlerConfiguration(ctx => factory(ctx)));
        return this;
    }

    public ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
        TOperationHandler operationHandler)
        where TOperationHandler : ISortOperationHandler<TContext>
    {
        Configuration.OperationHandlerConfigurations.Add(new SortOperationHandlerConfiguration(operationHandler));
        return this;
    }

    public static SortProviderDescriptor<TContext> New() => new();
}
