namespace HotChocolate.Data.Filters
{
    public interface IFilterProviderDescriptor<out TContext> : IFluent
        where TContext : IFilterVisitorContext
    {
        /// <summary>
        /// Adds a <see cref="IFilterFieldHandler{TContext,T}"/> to the provider
        /// This field handler is either injected by the dependency injection or created by an
        /// activator
        /// </summary>
        /// <typeparam name="TFieldHandler">The type of the field handler</typeparam>
        /// <returns>The descriptor that this methods was called on</returns>
        IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>()
            where TFieldHandler : IFilterFieldHandler<TContext>;

        /// <summary>
        /// Adds an instance of a  <see cref="IFilterFieldHandler{TContext,T}"/> to the provider
        /// This instance is directly used by the visitor for executing filters
        /// </summary>
        /// <param name="fieldHandler"></param>
        /// <typeparam name="TFieldHandler">The type of the field handler</typeparam>
        /// <returns>The descriptor that this methods was called on</returns>
        IFilterProviderDescriptor<TContext> AddFieldHandler<TFieldHandler>(
            TFieldHandler fieldHandler)
            where TFieldHandler : IFilterFieldHandler<TContext>;
    }
}
