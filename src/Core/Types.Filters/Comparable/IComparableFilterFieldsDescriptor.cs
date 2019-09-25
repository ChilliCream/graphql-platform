using HotChocolate.Types.Descriptors.Definitions;

namespace HotChocolate.Types.Filters
{
    public interface IComparableFilterFieldDescriptor
        : IFluent
    {
        /// <summary>
        /// Defines the filter binding behavior.
        ///
        /// The default binding behavior is set to
        /// <see cref="BindingBehavior.Implicit"/>.
        /// </summary>
        /// <param name="behavior">
        /// The binding behavior.
        ///
        /// Implicit:
        /// The boolean filter field descriptor will add
        /// all available boolean filter operations.
        ///
        /// Explicit:
        /// All filter operations have to be specified explicitly.
        /// </param>
        IComparableFilterFieldDescriptor BindFilters(
            BindingBehavior bindingBehavior);

        /// <summary>
        /// Defines that all filter operations have to be specified explicitly.
        /// </summary>
        IComparableFilterFieldDescriptor BindFiltersExplicitly();

        /// <summary>
        /// The comparable filter field descriptor will add
        /// all available comparable filter operations.
        /// </summary>
        IComparableFilterFieldDescriptor BindFiltersImplicitly();

        /// <summary>
        /// Allow equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowEquals();

        /// <summary>
        /// Allow not equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotEquals();

        /// <summary>
        /// Allow in filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowIn();

        /// <summary>
        /// Allow not in filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotIn();

        /// <summary>
        /// Allow greater than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowGreaterThan();

        /// <summary>
        /// Allow not greater than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotGreaterThan();

        /// <summary>
        /// Allow greater than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowGreaterThanOrEquals();

        /// <summary>
        /// Allow not greater than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotGreaterThanOrEquals();

        /// <summary>
        /// Allow lower than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowLowerThan();

        /// <summary>
        /// Allow not lower than filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotLowerThan();

        /// <summary>
        /// Allow lower than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowLowerThanOrEquals();

        /// <summary>
        /// Allow not lower than or equals filter operations.
        /// </summary>
        IComparableFilterOperationDescriptor AllowNotLowerThanOrEquals();

        /// <summary>
        /// Ignore the specified property.
        /// </summary>
        /// <param name="property">The property that hall be ignored.</param>
        IComparableFilterFieldDescriptor Ignore();
    }
}
