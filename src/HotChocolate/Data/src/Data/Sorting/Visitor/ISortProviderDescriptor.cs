namespace HotChocolate.Data.Sorting;

public interface ISortProviderDescriptor<out TContext> : IFluent
    where TContext : ISortVisitorContext
{
    ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        Func<SortProviderContext, TFieldHandler> factory)
        where TFieldHandler : ISortFieldHandler<TContext>;

    ISortProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
        TFieldHandler fieldHandler)
        where TFieldHandler : ISortFieldHandler<TContext>;

    ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
        Func<SortProviderContext, TOperationHandler> factory)
        where TOperationHandler : ISortOperationHandler<TContext>;

    ISortProviderDescriptor<TContext> AddOperationHandler<TOperationHandler>(
        TOperationHandler operationHandler)
        where TOperationHandler : ISortOperationHandler<TContext>;
}
