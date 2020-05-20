namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterVisitorOperationDescriptor<T> : IFluent
    {
        /// <summary>
        /// Specifies the handler of the current operation.
        /// </summary>
        /// <param name="handler">A delegate of type <see cref="FilterOperationHandler"/></param>
        IFilterVisitorOperationDescriptor<T> Handler(FilterOperationHandler<T> handler);

        /// <summary>
        /// Add additional configuration to <see cref="IFilterVisitorTypeDescriptor<T>"/>
        /// </summary>
        IFilterVisitorTypeDescriptor<T> And();
    }
}
