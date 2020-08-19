namespace HotChocolate.Data.Filters
{
    public interface IFilterProviderDescriptor<out TContext> : IFluent
        where TContext : IFilterVisitorContext
    {
        IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
            where TFieldHandler : IFilterFieldHandler<TContext>;

        IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
            TFieldHandler fieldHandler)
            where TFieldHandler : IFilterFieldHandler<TContext>;
    }
}
