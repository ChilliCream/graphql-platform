using HotChocolate.Types.Filters.Expressions;

namespace HotChocolate.Types.Filters.Conventions
{
    public interface IFilterExpressionOperationDescriptor : IFluent
    {
        /// <summary>
        /// Specifies the handler of the current operation.
        /// </summary>
        /// <param name="handler">A delegate of type <see cref="FilterOperationHandler"/></param>
        IFilterExpressionOperationDescriptor Handler(FilterOperationHandler handler);

        /// <summary>
        /// Add additional configuration to <see cref="IFilterVisitorDescriptor"/>
        /// </summary>
        IFilterExpressionTypeDescriptor And();
    }
}
